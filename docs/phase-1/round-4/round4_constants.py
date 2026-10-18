from pathlib import Path
import json,struct
exec(Path('locate_initialization.py').read_text(encoding='utf8').split('main=PE')[0])
out=Path(r'D:\MyGitRepo\Grim-Dynasty\docs\phase-1\round-4');rows=[]
spec={'x64/Engine.dll':{'literals':[0x30b77c,0x30bfd4,0x309c04,0x309bb4,0x30be38,0x30bee0,0x30bef8,0x30beb0,0x30bec8,0x30c270,0x30c288,0x30c228],'thunks':[0x2ddb38]},'x64/Game.dll':{'literals':[0x69b590],'thunks':[0x5da6c4,0x5da5dc]}}
for rel,sel in spec.items():
 pe=PE(rel);iat=pe.imports();lits=[];thunks=[]
 for r in sel['literals']:
  p=pe.off(r);raw=pe.b[p:p+160];lits.append({'rva':hex(r),'preferredVA':hex(pe.base+r),'fileOffset':hex(p),'asciiUntilNUL':raw.split(b'\0',1)[0].decode('ascii',errors='replace')})
 for r in sel['thunks']:
  raw=pe.b[pe.off(r):pe.off(r)+12];x={'rva':hex(r),'bytes':raw.hex(' ')}
  if raw[:2]==b'\xff\x25':
   to=r+6+struct.unpack_from('<i',raw,2)[0];x.update({'recognized':'RIP-relative import JMP','iatRVA':hex(to),'import':iat.get(to)})
  else:x['recognized']='not a simple import JMP; not followed'
  thunks.append(x)
 rows.append({'module':rel,'literals':lits,'thunks':thunks})
with (out/'constant-and-crt-thunk-evidence.json').open('x',encoding='utf8') as f:json.dump(rows,f,indent=2)
print(json.dumps(rows))
