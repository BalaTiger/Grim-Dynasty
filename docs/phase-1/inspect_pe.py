"""Read-only PE import inventory; never loads a DLL or executes a target."""
import hashlib
import json
import pathlib
import struct

ROOT = pathlib.Path(r'D:\Steam\steamapps\common\Grim Dawn')
TARGETS = ['Grim Dawn.exe', 'Engine.dll', 'Game.dll', 'AssetManager.exe',
           'Editor.exe', 'DBREditor.exe', 'QuestEditor.exe', 'ConversationEditor.exe',
           'ArchiveTool.exe', 'MapCompiler.exe', 'x64/Grim Dawn.exe',
           'x64/Engine.dll', 'x64/Game.dll']

def inspect(path):
    data = path.read_bytes()
    def u16(p): return struct.unpack_from('<H', data, p)[0]
    def u32(p): return struct.unpack_from('<I', data, p)[0]
    pe = u32(0x3c)
    if data[:2] != b'MZ' or data[pe:pe+4] != b'PE\0\0':
        raise ValueError('Not PE')
    coff = pe + 4
    machine, nsec = u16(coff), u16(coff+2)
    opt = coff + 20
    magic = u16(opt)
    dd = opt + (112 if magic == 0x20b else 96)
    sections = []
    for i in range(nsec):
        p = opt + u16(coff+16) + i*40
        sections.append((u32(p+12), max(u32(p+8),u32(p+16)), u32(p+20)))
    def offset(rva):
        if rva < u32(opt+60): return rva
        for start, size, raw in sections:
            if start <= rva < start+size: return raw + rva-start
        raise ValueError(f'Unmapped RVA {rva:x}')
    def cstr(rva):
        p=offset(rva)
        return data[p:data.index(b'\0',p)].decode('ascii',errors='replace')
    rva,size=u32(dd+8),u32(dd+12)
    imports=[]
    if rva:
        p=offset(rva)
        for i in range(min(size//20+1,1024)):
            row=struct.unpack_from('<IIIII',data,p+i*20)
            if not any(row): break
            imports.append(cstr(row[3]))
    delay_rva,delay_size=u32(dd+13*8),u32(dd+13*8+4)
    return {'path':str(path.relative_to(ROOT)), 'bytes':len(data),
            'sha256':hashlib.sha256(data).hexdigest(),
            'machine':hex(machine),'architecture':'x64' if machine==0x8664 else 'x86' if machine==0x14c else 'other',
            'imports':imports,'delay_import_directory_present':bool(delay_rva),
            'limitations':'Static normal import names only; delay imports/dynamic loads/transitive dependencies not resolved; no launch or DRM inference.'}

if __name__ == '__main__':
    print(json.dumps({'game_root':str(ROOT),'operation':'read-only PE headers and ordinary import descriptors',
                      'targets':[inspect(ROOT / x) for x in TARGETS]},indent=2))
