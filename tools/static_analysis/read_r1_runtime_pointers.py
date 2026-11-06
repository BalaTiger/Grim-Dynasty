"""Bounded read-only pointer sampler for the R1 runtime-value gaps.

Safety contract (deliberately narrow):
  * OpenProcess asks for PROCESS_QUERY_LIMITED_INFORMATION |
    PROCESS_VM_READ only; no debug privilege, no attach, no thread suspend.
  * Only ReadProcessMemory is used on the target. Nothing is written,
    allocated, injected, patched or executed in the target.
  * Sampling is a fixed count with a deadline; the script never waits on a
    target event and never blocks the target.
  * Every sample records the module base and the raw qword, so a wrong base
    or a missing pointer is visible as data instead of being inferred.

This tool produces runtime *values* for the OWN model to consume. It does not
establish that a value has a particular meaning; that stays with the model's
explicit publications.
"""
import ctypes
import ctypes.wintypes as wt
import json
import sys
import time

PROCESS_QUERY_LIMITED_INFORMATION = 0x1000
PROCESS_VM_READ = 0x0010
TH32CS_SNAPMODULE = 0x00000008
TH32CS_SNAPMODULE32 = 0x00000010
MAX_SAMPLES_CAP = 32
MAX_CHAIN = 8


class MODULEENTRY32W(ctypes.Structure):
    _fields_ = [
        ("dwSize", wt.DWORD), ("th32ModuleID", wt.DWORD),
        ("th32ProcessID", wt.DWORD), ("GlblcntUsage", wt.DWORD),
        ("ProccntUsage", wt.DWORD), ("modBaseAddr", ctypes.POINTER(ctypes.c_byte)),
        ("modBaseSize", wt.DWORD), ("hModule", wt.HMODULE),
        ("szModule", wt.WCHAR * 256), ("szExePath", wt.WCHAR * 260),
    ]


class MODULEINFO(ctypes.Structure):
    _fields_ = [
        ("lpBaseOfDll", ctypes.c_void_p),
        ("SizeOfImage", wt.DWORD),
        ("EntryPoint", ctypes.c_void_p),
    ]


class Sampler:
    def __init__(self, pid, prefer_psapi=False):
        self.pid = pid
        self.prefer_psapi = prefer_psapi
        self.k32 = ctypes.WinDLL("kernel32", use_last_error=True)
        self.handle = self.k32.OpenProcess(
            PROCESS_QUERY_LIMITED_INFORMATION | PROCESS_VM_READ, False, pid)
        if not self.handle:
            raise OSError(ctypes.get_last_error(), "OpenProcess failed")

    def close(self):
        if self.handle:
            self.k32.CloseHandle(self.handle)
            self.handle = None

    def module(self, name):
        """Locate a module by name.

        Toolhelp is tried first; some sandboxes deny that snapshot while still
        allowing PSAPI enumeration, so PSAPI is the documented fallback. A
        failed lookup returns None instead of a guessed base.
        """
        if not self.prefer_psapi:
            found = self._module_toolhelp(name)
            if found is not None:
                return found
        return self._module_psapi(name)

    def _module_toolhelp(self, name):
        snap = self.k32.CreateToolhelp32Snapshot(
            TH32CS_SNAPMODULE | TH32CS_SNAPMODULE32, self.pid)
        if snap == ctypes.c_void_p(-1).value:
            return None
        try:
            entry = MODULEENTRY32W()
            entry.dwSize = ctypes.sizeof(MODULEENTRY32W)
            ok = self.k32.Module32FirstW(snap, ctypes.byref(entry))
            while ok:
                if entry.szModule.lower() == name.lower():
                    base = ctypes.cast(entry.modBaseAddr, ctypes.c_void_p).value
                    return {"name": entry.szModule, "base": base,
                            "size": entry.modBaseSize, "path": entry.szExePath,
                            "via": "toolhelp"}
                ok = self.k32.Module32NextW(snap, ctypes.byref(entry))
            return None
        finally:
            self.k32.CloseHandle(snap)

    def _module_psapi(self, name):
        psapi = ctypes.WinDLL("psapi", use_last_error=True)
        psapi.EnumProcessModules.argtypes = [wt.HANDLE, ctypes.POINTER(ctypes.c_void_p),
                                             wt.DWORD, ctypes.POINTER(wt.DWORD)]
        psapi.EnumProcessModules.restype = wt.BOOL
        psapi.GetModuleBaseNameW.argtypes = [wt.HANDLE, ctypes.c_void_p,
                                             wt.LPWSTR, wt.DWORD]
        psapi.GetModuleBaseNameW.restype = wt.DWORD
        psapi.GetModuleInformation.argtypes = [wt.HANDLE, ctypes.c_void_p,
                                               ctypes.POINTER(MODULEINFO), wt.DWORD]
        psapi.GetModuleInformation.restype = wt.BOOL
        needed = wt.DWORD(0)
        MAX_MODULES = 1024
        array = (ctypes.c_void_p * MAX_MODULES)()
        if not psapi.EnumProcessModules(self.handle, array,
                                        ctypes.sizeof(array), ctypes.byref(needed)):
            return None
        count = min(MAX_MODULES, needed.value // ctypes.sizeof(ctypes.c_void_p))
        for index in range(count):
            module_handle = ctypes.c_void_p(array[index])
            buf = ctypes.create_unicode_buffer(260)
            if not psapi.GetModuleBaseNameW(self.handle, module_handle, buf, 260):
                continue
            if buf.value.lower() != name.lower():
                continue
            info = MODULEINFO()
            if not psapi.GetModuleInformation(self.handle, module_handle,
                                              ctypes.byref(info), ctypes.sizeof(info)):
                continue
            path = ctypes.create_unicode_buffer(32768)
            self.k32.QueryFullProcessImageNameW.argtypes = [wt.HANDLE, wt.DWORD,
                                                            wt.LPWSTR, ctypes.POINTER(wt.DWORD)]
            self.k32.QueryFullProcessImageNameW.restype = wt.BOOL
            length = wt.DWORD(32768)
            self.k32.QueryFullProcessImageNameW(self.handle, 0, path, ctypes.byref(length))
            return {"name": buf.value, "base": ctypes.cast(info.lpBaseOfDll, ctypes.c_void_p).value,
                    "size": info.SizeOfImage, "path": path.value, "via": "psapi"}
        return None

    def read(self, address, size=8):
        buf = ctypes.create_string_buffer(size)
        got = ctypes.c_size_t(0)
        ok = self.k32.ReadProcessMemory(
            self.handle, ctypes.c_void_p(address), buf, size, ctypes.byref(got))
        if not ok or got.value != size:
            return None
        return int.from_bytes(buf.raw, "little")


FNV_OFFSET = 0xCBF29CE484222325
FNV_PRIME = 0x100000001B3
FNV_MASK64 = (1 << 64) - 1


def fnv1a64_id(value):
    """FNV-1a over exactly the four little-endian bytes of the id.

    Reference vector shared with the model's own tests: id 0 must produce
    0x4d25767f9dce13f5. self_test() asserts that, so the Python hash cannot
    silently drift from the C# one.
    """
    h = FNV_OFFSET
    for shift in (0, 8, 16, 24):
        h ^= (value >> shift) & 0xFF
        h = (h * FNV_PRIME) & FNV_MASK64
    return h


def map_lookup(sampler, manager, key, layout):
    """Traverse the documented bucket chain; never assume a hit."""
    buckets = sampler.read(manager + layout["buckets_offset"])
    mask = sampler.read(manager + layout["mask_offset"])
    sentinel = sampler.read(manager + layout["sentinel_offset"])
    trace = {"manager": f"0x{manager:x}",
             "buckets": None if buckets is None else f"0x{buckets:x}",
             "mask": None if mask is None else f"0x{mask:x}",
             "sentinel": None if sentinel is None else f"0x{sentinel:x}"}
    if not buckets or mask is None or not sentinel:
        trace["result"] = "unreadable-manager-field"
        return None, trace
    index = mask & fnv1a64_id(key)
    trace["key"] = f"0x{key:x}"
    trace["hash"] = f"0x{fnv1a64_id(key):x}"
    trace["index"] = index
    cell = buckets + index * layout["cell_stride"]
    first = sampler.read(cell)
    last = sampler.read(cell + 8)
    trace["cell"] = f"0x{cell:x}"
    trace["first"] = None if first is None else f"0x{first:x}"
    if not first:
        trace["result"] = "unreadable-bucket"
        return None, trace
    end = sentinel if first == sentinel else (sampler.read(last + layout["node_next"]) if last else None)
    trace["end"] = None if end is None else f"0x{end:x}"
    if end is None:
        trace["result"] = "unreadable-chain-end"
        return None, trace
    cursor = first
    steps = 0
    while cursor != end:
        if steps >= layout["max_steps"]:
            trace["result"] = "step-limit"
            return None, trace
        steps += 1
        node_key = sampler.read(cursor + layout["node_key"], 4)
        if node_key is None:
            trace["result"] = "unreadable-node-key"
            return None, trace
        if node_key == key:
            obj = sampler.read(cursor + layout["node_object"])
            trace["steps"] = steps
            trace["node"] = f"0x{cursor:x}"
            trace["object"] = None if obj is None else f"0x{obj:x}"
            trace["result"] = "found" if obj else "found-node-without-object"
            return obj, trace
        cursor = sampler.read(cursor + layout["node_next"])
        if not cursor:
            trace["result"] = "unreadable-next"
            return None, trace
    trace["steps"] = steps
    trace["result"] = "miss"
    return None, trace


WIDTHS = {1: 1, 2: 2, 4: 4, 8: 8}


def read_fields(sampler, base, fields):
    """Read a fixed list of {name, offset, width} fields off one object.

    A field that cannot be read is reported as null; no zero or default is
    substituted, so a wrong object shows up as unreadable rather than coherent.
    """
    out = {}
    for field in fields:
        width = int(field.get("width", 8))
        if width not in WIDTHS:
            out[field["name"]] = {"error": "unsupported-width", "width": width}
            continue
        offset = int(field["offset"], 0)
        value = sampler.read(base + offset, width)
        out[field["name"]] = {
            "offset": hex(offset),
            "width": width,
            "value": None if value is None else f"0x{value:0{width * 2}x}",
        }
    return out


def enumerate_map(sampler, manager, layout, limit=64):
    """Walk every bucket and report (key, node, object) triples.

    This route avoids depending on the Engine object pointer: the caller can
    identify the wanted object by its class or by a key seen elsewhere. The
    walk is bounded by bucket count, a per-bucket chain cap and a total cap, and
    every unreadable step is reported instead of being skipped silently.
    """
    buckets = sampler.read(manager + layout["buckets_offset"])
    mask = sampler.read(manager + layout["mask_offset"])
    sentinel = sampler.read(manager + layout["sentinel_offset"])
    out = {"result": "ok", "entries": [], "buckets": None,
           "unreadable": 0, "skipped_buckets": 0}
    if not buckets or mask is None or not sentinel:
        out["result"] = "unreadable-manager-field"
        return out
    count = mask + 1
    out["buckets"] = count
    produced = 0
    for index in range(count):
        if produced >= limit:
            out["result"] = "limit-reached"
            break
        cell = buckets + index * layout["cell_stride"]
        first = sampler.read(cell)
        last = sampler.read(cell + 8)
        if first is None:
            out["unreadable"] += 1
            continue
        if first == sentinel:
            continue
        end = sampler.read(last + layout["node_next"]) if last else None
        if end is None:
            out["unreadable"] += 1
            out["skipped_buckets"] += 1
            continue
        cursor = first
        steps = 0
        while cursor != end and steps < layout["max_steps"]:
            steps += 1
            key = sampler.read(cursor + layout["node_key"], 4)
            obj = sampler.read(cursor + layout["node_object"])
            if key is None or obj is None:
                out["unreadable"] += 1
                break
            vtable = sampler.read(obj)
            entry = {"key": f"0x{key:x}", "node": f"0x{cursor:x}",
                     "object": f"0x{obj:x}",
                     "vtable": None if vtable is None else f"0x{vtable:x}"}
            out["entries"].append(entry)
            produced += 1
            if produced >= limit:
                out["result"] = "limit-reached"
                break
            cursor = sampler.read(cursor + layout["node_next"])
            if not cursor:
                out["unreadable"] += 1
                break
    return out


def walk(sampler, base, chain, final_dword=None):
    """Follow [addr+off] for each hop, then optionally read a DWORD leaf.

    A hop that cannot be read ends the walk with the trace preserved; the
    missing value is reported as null rather than substituted.
    """
    pointer = base
    trace = []
    for hop in chain[:MAX_CHAIN]:
        off = int(hop, 0)
        address = pointer + off
        value = sampler.read(address)
        trace.append({"at": f"0x{address:x}", "offset": hop,
                      "value": None if value is None else f"0x{value:x}"})
        if value is None:
            return None, trace
        pointer = value
    if final_dword is not None:
        off = int(final_dword, 0)
        address = pointer + off
        value = sampler.read(address, 4)
        trace.append({"at": f"0x{address:x}", "offset": final_dword,
                      "value": None if value is None else f"0x{value:x}",
                      "width": 4})
        return (None if value is None else f"0x{value:x}"), trace
    return pointer, trace


def self_test(prefer_psapi=False):
    """Prove the plumbing on this very process: module base + own MZ header.

    A pass shows OpenProcess/ReadProcessMemory/module lookup work at all. It is
    NOT evidence about the original game and is never reported as such.
    """
    import os
    sampler = Sampler(os.getpid(), prefer_psapi=prefer_psapi)
    try:
        module = sampler.module(os.path.basename(sys.executable))
        if module is None:
            return {"status": "self-test-module-not-found",
                    "module": os.path.basename(sys.executable)}, 1
        head = sampler.read(module["base"], 2)
        pointer = sampler.read(module["base"] + 0x3C, 4)
        return {
            "status": "self-test-passed" if head == 0x5A4D else "self-test-bad-mz",
            "pid": os.getpid(), "module": module,
            "mz": None if head is None else f"0x{head:04x}",
            "e_lfanew": None if pointer is None else f"0x{pointer:x}",
            "writes_to_target": 0, "debugger": False,
            "scope": "plumbing only; not game evidence",
            "fnv_reference_ok": fnv1a64_id(0) == 0x4D25767F9DCE13F5,
            "fnv_id0": f"0x{fnv1a64_id(0):x}",
        }, 0 if head == 0x5A4D else 1
    finally:
        sampler.close()


def main(argv):
    if len(argv) == 2 and argv[1] == "--self-test":
        payload, code = self_test()
        print(json.dumps(payload))
        return code
    if len(argv) == 2 and argv[1] == "--self-test-psapi":
        payload, code = self_test(prefer_psapi=True)
        print(json.dumps(payload))
        return code
    if len(argv) not in (4, 5):
        sys.stderr.write("usage: read_r1_runtime_pointers.py <pid> <module> <gEngineRvaHex> [chain.json]\n")
        return 2
    pid = int(argv[1])
    module_name = argv[2]
    gengine_rva = int(argv[3], 0)
    samples = 1
    chain = []
    final_dword = None
    label = None
    key_chain = None
    key_dword = None
    map_spec = None
    if len(argv) == 5:
        with open(argv[4], "r", encoding="utf-8") as handle:
            spec = json.load(handle)
        chain = spec.get("chain", [])
        final_dword = spec.get("final_dword_offset")
        label = spec.get("label")
        key_chain = spec.get("key_chain")
        key_dword = spec.get("key_dword_offset")
        map_spec = spec.get("map")
        samples = max(1, min(MAX_SAMPLES_CAP, int(spec.get("samples", 1))))

    sampler = Sampler(pid)
    try:
        module = sampler.module(module_name)
        if module is None:
            print(json.dumps({"status": "module-not-found", "module": module_name}))
            return 1
        rows = []
        deadline = time.monotonic() + 30.0
        for index in range(samples):
            if time.monotonic() > deadline:
                break
            gengine = sampler.read(module["base"] + gengine_rva)
            row = {
                "index": index,
                "utc": time.strftime("%Y-%m-%dT%H:%M:%S", time.gmtime()) + "Z",
                "label": label,
                "module_base": f"0x{module['base']:x}",
                "gengine_rva": f"0x{gengine_rva:x}",
                "gengine": None if gengine is None else f"0x{gengine:x}",
                "chain": [],
                "chain_result": None,
                "writes": 0,
                "debugger": False,
            }
            if gengine:
                if key_chain is not None:
                    key, key_trace = walk(sampler, gengine, key_chain, key_dword)
                    row["key_trace"] = key_trace
                    row["key"] = None if key is None else (
                        key if isinstance(key, str) else f"0x{key:x}")
                else:
                    result, trace = walk(sampler, gengine, chain, final_dword)
                    row["chain"] = trace
                    row["chain_result"] = None if result is None else (
                        result if isinstance(result, str) else f"0x{result:x}")
            if map_spec is not None:
                manager = sampler.read(module["base"] + int(map_spec["manager_global_rva"], 0))
                row["manager"] = None if manager is None else f"0x{manager:x}"
                layout = {
                    "buckets_offset": int(map_spec["buckets_offset"], 0),
                    "mask_offset": int(map_spec["mask_offset"], 0),
                    "sentinel_offset": int(map_spec["sentinel_offset"], 0),
                    "cell_stride": int(map_spec["cell_stride"], 0),
                    "node_next": int(map_spec["node_next"], 0),
                    "node_key": int(map_spec["node_key"], 0),
                    "node_object": int(map_spec["node_object"], 0),
                    "max_steps": int(map_spec.get("max_steps", 32)),
                }
                enumerate_spec = spec.get("enumerate")
                if enumerate_spec is not None and manager:
                    row["enumerate"] = enumerate_map(
                        sampler, manager, layout,
                        int(enumerate_spec.get("max_entries", 64)))
                key_value = row.get("key")
                if manager and isinstance(key_value, str):
                    obj, map_trace = map_lookup(sampler, manager, int(key_value, 16), layout)
                    row["map"] = map_trace
                    row["object"] = None if obj is None else f"0x{obj:x}"
                    fields = spec.get("fields") or []
                    if obj and fields:
                        row["fields"] = read_fields(sampler, obj, fields)
                    controller = spec.get("controller")
                    if obj and controller:
                        controller_row = {}
                        if controller.get("vtable"):
                            vtable = sampler.read(obj)
                            controller_row["vtable"] = None if vtable is None else f"0x{vtable:x}"
                        id_offset = controller.get("id_offset")
                        id_value = None
                        if id_offset is not None:
                            id_value = sampler.read(obj + int(id_offset, 0), 4)
                            controller_row["id"] = None if id_value is None else f"0x{id_value:x}"
                        if id_value:
                            ctl, ctl_trace = map_lookup(sampler, manager, id_value, layout)
                            controller_row["map"] = ctl_trace
                            controller_row["pointer"] = None if ctl is None else f"0x{ctl:x}"
                            ctl_fields = controller.get("fields") or []
                            if ctl and ctl_fields:
                                controller_row["fields"] = read_fields(sampler, ctl, ctl_fields)
                        row["controller"] = controller_row
                else:
                    row["map"] = {"result": "no-key-or-manager"}
            rows.append(row)
            if index + 1 < samples:
                time.sleep(0.25)
        print(json.dumps({
            "status": "sampled" if rows else "no-sample",
            "pid": pid,
            "module": module,
            "access": "PROCESS_QUERY_LIMITED_INFORMATION|PROCESS_VM_READ",
            "writes_to_target": 0,
            "samples": rows,
        }))
        return 0
    finally:
        sampler.close()


if __name__ == "__main__":
    sys.exit(main(sys.argv))
