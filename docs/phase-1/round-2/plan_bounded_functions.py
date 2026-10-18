from pathlib import Path
import json,struct
base=Path(r'D:\MyGitRepo\Grim-Dynasty');src=base/'docs/phase-1/boundary-audit/module-boundaries.json';out=base/'docs/phase-1/round-2';out.mkdir(exist_ok=True)
data=json.loads(src.read_text(encoding='utf8'));plans=[]
targets={'x64/Engine.dll':['?SetSavePath@Engine@','?GetBaseDataPathFromOptions@Engine@','?GetDatabaseArchive@Engine@'], 'x64/Game.dll':['?GetRootSavePath@GameEngine@','?GetUserSaveDataFolder@GameEngine@','?GetBaseFolder@GameEngine@']}
for rel,pats in targets.items():
 b=(Path(r'D:\Steam\steamapps\common\Grim Dawn')/rel).read_bytes();u16=lambda p:struct.unpack_from('<H',b,p)[0];u32=lambda p:struct.unpack_from('<I',b,p)[0];coff=u32(0x3c)+4;opt=coff+20;image=struct.unpack_from('<Q',b,opt+24)[0];dd=opt+112;secs=[]
 for i in range(u16(coff+2)):
  p=opt+u16(coff+16)+i*40;secs.append((u32(p+12),max(u32(p+8),u32(p+16)),u32(p+20)))
 def off(r):
  for s,n,p in secs:
   if s<=r<s+n:return p+r-s
  raise ValueError('Unmapped')
 er=u32(dd+24);es=u32(dd+28);ranges=[]
 if er:
  p=off(er)
  for i in range(es//12):
   start,end,uw=struct.unpack_from('<III',b,p+12*i)
   if start:ranges.append((start,end))
 module=next(x for x in data if x['relativePath']==rel);examples={x['name']:x for c in module['categories'].values() for x in c['exportExamples']};funcs=[]
 for pat in pats:
  x=next(x for n,x in examples.items() if n.startswith(pat));rva=int(x['rva'],16);matches=[(s,e) for s,e in ranges if s==rva]
  size=matches[0][1]-rva if len(matches)==1 else 16
  if size>2048:raise ValueError('Function exceeds selected budget')
  funcs.append({'symbol':x['name'],'start':hex(image+rva),'end':hex(image+rva+size-1),'bytes':size,'boundary':'PE exception RuntimeFunction' if matches else '16-byte probe; leaf boundary unresolved'})
 plans.append({'module':rel,'functions':funcs})
with (out/'bounded-function-plan.json').open('x',encoding='utf8') as f:json.dump(plans,f,indent=2)
for p in plans:print(p['module'],[(x['start'],x['bytes'],x['boundary']) for x in p['functions']])
