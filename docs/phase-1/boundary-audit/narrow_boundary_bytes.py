from pathlib import Path
import struct,json,hashlib
root=Path(r'D:\Steam\steamapps\common\Grim Dawn');out=Path(r'D:\MyGitRepo\Grim-Dynasty\docs\phase-1\boundary-audit')
j=json.loads((out/'module-boundaries.json').read_text(encoding='utf8'))
rows=[]
targets={'x64/Engine.dll':['?GetSavePath@Engine@','?SetSavePath@Engine@','?GetDatabaseArchive@Engine@'], 'x64/Game.dll':['?GetRootSavePath@GameEngine@','?GetBaseFolder@GameEngine@','?GetUserSaveDataFolder@GameEngine@']}
for rel,patterns in targets.items():
 b=(root/rel).read_bytes();u16=lambda p:struct.unpack_from('<H',b,p)[0];u32=lambda p:struct.unpack_from('<I',b,p)[0]
 pe=u32(0x3c);coff=pe+4;opt=coff+20;base=struct.unpack_from('<Q',b,opt+24)[0];secs=[]
 for i in range(u16(coff+2)):
  p=opt+u16(coff+16)+i*40;secs.append((u32(p+12),max(u32(p+8),u32(p+16)),u32(p+20)))
 def off(r):
  for s,n,o in secs:
   if s<=r<s+n:return o+r-s
  raise ValueError('Unmapped')
 module=next(x for x in j if x['relativePath']==rel)
 examples={x['name']:x for c in module['categories'].values() for x in c['exportExamples']}
 for pat in patterns:
  candidates=[x for n,x in examples.items() if n.startswith(pat)]
  if len(candidates)!=1:raise ValueError('Non-unique known symbol '+pat)
  sym=candidates[0];rva=int(sym['rva'],16);p=off(rva);sample=b[p:p+64];shape='not interpreted'
  if sample[:3] in [b'\x48\x8d\x81',b'\x48\x8b\x81'] and sample[7]==0xc3:
   delta=struct.unpack_from('<i',sample,3)[0];op='address of field' if sample[1]==0x8d else 'load 8-byte field'
   shape=f'x64 straight-line getter: {op} at this+{delta:#x}; ret. No other instruction before ret.'
  rows.append({'module':rel,'moduleSHA256':hashlib.sha256(b).hexdigest(),'export':sym['name'],'rva':sym['rva'],'preferredVA':hex(base+rva),'fileOffset':hex(p),'sampleBytes':len(sample),'hex':sample.hex(' '),'boundedDecode':shape,'limitations':'Only the explicit getter pattern is decoded; the 64-byte sample may include adjacent function bytes. Does not verify caller initialization, ABI availability, path completeness, successful serialization or standalone launch.'})
with (out/'six-function-byte-probes.json').open('x',encoding='utf8') as f:json.dump(rows,f,indent=2)
print(json.dumps([{k:v for k,v in x.items() if k not in ['hex','moduleSHA256','limitations','export']} for x in rows]))
