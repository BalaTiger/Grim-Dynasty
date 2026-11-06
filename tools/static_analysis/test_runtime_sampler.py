"""Logic tests for the runtime sampler's read-only traversal.

A FakeSampler serves a dict of address -> integer, so bucket traversal, key
matching, misses and the step limit are exercised without touching any process.
The FNV reference value is the same one the C# model's tests use.
"""
import importlib.util
import pathlib
import sys

HERE = pathlib.Path(__file__).resolve().parent
spec = importlib.util.spec_from_file_location(
    "sampler", HERE / "read_r1_runtime_pointers.py")
sampler_module = importlib.util.module_from_spec(spec)
spec.loader.exec_module(sampler_module)


class FakeSampler:
    def __init__(self, cells):
        self.cells = cells
        self.reads = 0

    def read(self, address, size=8):
        self.reads += 1
        value = self.cells.get(address)
        if value is None:
            return None
        if size == 4:
            return value & 0xFFFFFFFF
        return value


LAYOUT = {
    "buckets_offset": 0x48, "mask_offset": 0x60, "sentinel_offset": 0x38,
    "cell_stride": 0x10, "node_next": 0x00, "node_key": 0x10,
    "node_object": 0x18, "max_steps": 32,
}


def build(ids_with_objects, mask=7, bucket_base=0x9000, sentinel=0x7000):
    """Lay out a minimal manager whose chain is ordered by insertion."""
    cells = {}
    manager = 0x1000
    cells[manager + 0x48] = bucket_base
    cells[manager + 0x60] = mask
    cells[manager + 0x38] = sentinel
    # sentinel node points to itself so an empty cell is well formed
    cells[sentinel + 0x00] = sentinel
    buckets = {}
    node_at = 0x2000
    ends = {}
    for key, obj in ids_with_objects:
        node = node_at
        node_at += 0x100
        cells[node + 0x10] = key
        cells[node + 0x18] = obj
        index = mask & sampler_module.fnv1a64_id(key)
        buckets.setdefault(index, []).append(node)
        ends[index] = node
    for index, nodes in buckets.items():
        cell = bucket_base + index * 0x10
        cells[cell] = nodes[0]
        cells[cell + 8] = nodes[-1]
        for position, node in enumerate(nodes):
            nxt = nodes[position + 1] if position + 1 < len(nodes) else None
            cells[node] = nxt if nxt else sentinel
    return cells, manager


def main():
    failures = []

    def check(name, condition, detail=""):
        if not condition:
            failures.append(f"{name}: {detail}")
        print(json_line(name, bool(condition), detail))

    # 1. FNV reference vector from the model's own tests.
    check("fnv-id0", sampler_module.fnv1a64_id(0) == 0x4D25767F9DCE13F5,
          hex(sampler_module.fnv1a64_id(0)))

    # 2. Single object: found, and the object pointer is returned.
    cells, manager = build([(7, 0xAA00)])
    fake = FakeSampler(cells)
    obj, trace = sampler_module.map_lookup(fake, manager, 7, LAYOUT)
    check("single-found", obj == 0xAA00, f"obj={obj} trace={trace.get('result')}")
    check("single-result", trace["result"] == "found", trace["result"])

    # 3. Miss on an id whose bucket exists but has no matching key.
    obj, trace = sampler_module.map_lookup(fake, manager, 3, LAYOUT)
    check("miss", obj is None and trace["result"] in ("miss", "unreadable-bucket"),
          trace["result"])

    # 4. Collision chain: two ids in one bucket, second one is found.
    colliding = [k for k in range(16) if (7 & sampler_module.fnv1a64_id(k)) == (7 & sampler_module.fnv1a64_id(7))]
    if len(colliding) >= 2:
        first, second = colliding[0], colliding[1]
        cells, manager = build([(first, 0xBB00), (second, 0xCC00)])
        fake = FakeSampler(cells)
        obj, trace = sampler_module.map_lookup(fake, manager, second, LAYOUT)
        check("collision-second", obj == 0xCC00, f"obj={obj} steps={trace.get('steps')}")
        check("collision-steps", trace.get("steps") == 2, str(trace.get("steps")))
    else:
        check("collision-setup", False, "no colliding pair found")

    # 5. Unreadable manager field is reported, never guessed.
    cells, manager = build([(7, 0xAA00)])
    del cells[manager + 0x60]
    obj, trace = sampler_module.map_lookup(FakeSampler(cells), manager, 7, LAYOUT)
    check("unreadable-mask", obj is None and trace["result"] == "unreadable-manager-field",
          trace["result"])

    # 6. Unreadable bucket first pointer.
    cells, manager = build([(7, 0xAA00)])
    index = 7 & sampler_module.fnv1a64_id(7)
    del cells[0x9000 + index * 0x10]
    obj, trace = sampler_module.map_lookup(FakeSampler(cells), manager, 7, LAYOUT)
    check("unreadable-bucket", obj is None and trace["result"] == "unreadable-bucket",
          trace["result"])

    # 7. Step limit stops a cyclic chain instead of looping forever.
    # Build a real cycle A<->B and a chain end that the walk can never reach,
    # so only the step guard can terminate it.
    cells, manager = build([(7, 0xAA00)])
    index = 7 & sampler_module.fnv1a64_id(7)
    cell = 0x9000 + index * 0x10
    a = cells[cell]
    b = 0x6000
    c = 0x6100
    d = 0x6200
    cells[b + 0x10] = 0x424242
    cells[b + 0x18] = 0xDD00
    cells[c] = d            # last node's next defines the end as d
    cells[d] = c
    cells[a] = b            # A -> B
    cells[b] = a            # B -> A  (cycle, never reaches d)
    cells[a + 0x10] = 0x99  # neither node carries the queried key
    cells[b + 0x10] = 0x98
    cells[cell] = a
    cells[cell + 8] = c
    layout = dict(LAYOUT, max_steps=4)
    obj, trace = sampler_module.map_lookup(FakeSampler(cells), manager, 7, layout)
    check("step-limit", trace["result"] == "step-limit", trace["result"])

    # 8. Field reads respect width and never substitute a missing value.
    fields = [
        {"name": "gate", "offset": "0x26af", "width": 1},
        {"name": "id", "offset": "0x16c0", "width": 4},
        {"name": "qword", "offset": "0x2a0", "width": 8},
        {"name": "missing", "offset": "0x9000", "width": 8},
        {"name": "badwidth", "offset": "0x0", "width": 3},
    ]
    obj_cells = {
        0x5000 + 0x26AF: 0x01,
        0x5000 + 0x16C0: 0xDEADBEEF,
        0x5000 + 0x2A0: 0x1122334455667788,
    }
    got = sampler_module.read_fields(FakeSampler(obj_cells), 0x5000, fields)
    check("field-byte", got["gate"]["value"] == "0x01", str(got["gate"]))
    check("field-dword", got["id"]["value"] == "0xdeadbeef", str(got["id"]))
    check("field-qword", got["qword"]["value"] == "0x1122334455667788", str(got["qword"]))
    check("field-missing-null", got["missing"]["value"] is None, str(got["missing"]))
    check("field-bad-width", got["badwidth"].get("error") == "unsupported-width",
          str(got["badwidth"]))

    # 9. Character -> controller id -> same map -> controller object.
    cells, manager = build([(7, 0xAA00), (0x4242, 0xBB00)])
    cells[0xAA00 + 0x16C0] = 0x4242          # character's controller id
    fake = FakeSampler(cells)
    character, _ = sampler_module.map_lookup(fake, manager, 7, LAYOUT)
    check("hop-character", character == 0xAA00, str(character))
    cid = fake.read(character + 0x16C0, 4)
    check("hop-controller-id", cid == 0x4242, str(cid))
    controller, ctl_trace = sampler_module.map_lookup(fake, manager, cid, LAYOUT)
    check("hop-controller", controller == 0xBB00, f"{controller} {ctl_trace.get('result')}")

    # 10. A controller id that is absent from the map is a miss, not the character.
    cells[0xAA00 + 0x16C0] = 0x5555
    fake = FakeSampler(cells)
    character, _ = sampler_module.map_lookup(fake, manager, 7, LAYOUT)
    cid = fake.read(character + 0x16C0, 4)
    controller, ctl_trace = sampler_module.map_lookup(fake, manager, cid, LAYOUT)
    check("hop-missing-controller", controller is None and ctl_trace["result"] in ("miss", "unreadable-bucket"),
          ctl_trace["result"])

    # 11. Enumeration lists every bucket entry with key, node and object.
    cells, manager = build([(1, 0x1100), (2, 0x2200), (3, 0x3300)])
    fake = FakeSampler(cells)
    listing = sampler_module.enumerate_map(fake, manager, LAYOUT, limit=16)
    keys = sorted(int(e["key"], 16) for e in listing["entries"])
    check("enumerate-count", len(listing["entries"]) == 3, str(len(listing["entries"])))
    check("enumerate-keys", keys == [1, 2, 3], str(keys))
    objects = sorted(int(e["object"], 16) for e in listing["entries"])
    check("enumerate-objects", objects == [0x1100, 0x2200, 0x3300], str(objects))

    # 12. Enumeration honours the entry cap instead of running away.
    listing = sampler_module.enumerate_map(fake, manager, LAYOUT, limit=2)
    check("enumerate-limit", len(listing["entries"]) == 2 and listing["result"] == "limit-reached",
          f"{len(listing['entries'])} {listing['result']}")

    # 13. A missing manager field is reported, and an empty map is empty not wrong.
    cells, manager = build([])
    listing = sampler_module.enumerate_map(FakeSampler(cells), manager, LAYOUT, limit=8)
    check("enumerate-empty", listing["entries"] == [] and listing["result"] == "ok",
          listing["result"])
    cells, manager = build([(1, 0x1100)])
    del cells[manager + 0x48]
    listing = sampler_module.enumerate_map(FakeSampler(cells), manager, LAYOUT, limit=8)
    check("enumerate-unreadable", listing["result"] == "unreadable-manager-field",
          listing["result"])

    print("-" * 40)
    print(f"{len(failures)} failure(s)")
    for failure in failures:
        print("  " + failure)
    return 1 if failures else 0


def json_line(name, ok, detail):
    return f"{'PASS' if ok else 'FAIL'}  {name}" + (f"  ({detail})" if detail else "")


if __name__ == "__main__":
    sys.exit(main())
