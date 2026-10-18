"""Inspect normal PE import metadata only; no executable or DLL loading."""
from pathlib import Path
import struct,json
root=Path(r'D:\Steam\steamapps\common\Grim Dawn');result=[]
for rel in ['Grim Dawn.exe','x64/Grim Dawn.exe','Game.dll','x64/Game.dll','Engine.dll','x64/Engine.dll']:
 b=(root/rel).read_bytes();u16=lambda p:struct.unpack_from('<H',b,p)[0];u32=lambda p:struct.unpack_from('<I',b,p)[0];coff=u32(0x3c)+4;opt=coff+20;wide=u16(opt)==0x20b;dd=opt+(112 if wide else 96);sections=[]
 for i in range(u16(coff+2)):
  p=opt+u16(coff+16)+i*40;sections.append((u32(p+12),max(u32(p+8),u32(p+16)),u32(p+20)))
 def offset(rva):
  for start,size,raw in sections:
   if start<=rva<start+size:return raw+rva-start
  raise ValueError(hex(rva))
 def string(rva):
  p=offset(rva);return b[p:b.index(b'\0',p)].decode('ascii')
 imports=[];ir=u32(dd+8)
 if ir:
  p=offset(ir)
  while any(b[p:p+20]):
   thunk=u32(p) or u32(p+16);dll=string(u32(p+12))
   if 'steam' in dll.casefold():
    q=offset(thunk);step=8 if wide else 4;names=[]
    while True:
     n=struct.unpack_from('<Q' if wide else '<I',b,q)[0];q+=step
     if not n:break
     names.append('ordinal:'+str(n&65535) if n>>(step*8-1) else string(n+2))
    imports.append({'dll':dll,'names':names})
   p+=20
 result.append({'relative_path':rel,'steam_normal_imports':imports})
Path('steam-import-readiness.json').write_text(json.dumps({'operation':'read-only normal PE import metadata','files':result,'limitation':'Imports establish dependency, not observed runtime failure; original game not launched'},indent=2),encoding='utf8')
print(json.dumps(result,indent=2))
