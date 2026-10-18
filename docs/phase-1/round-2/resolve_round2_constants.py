from pathlib import Path
import json,struct
root=Path(r'D:\Steam\steamapps\common\Grim Dawn');out=Path(r'D:\MyGitRepo\Grim-Dynasty\docs\phase-1\round-2')
plans={'x64/Engine.dll':{'literals':[0x307f69,0x309af4],'thunks':[]},'x64/Game.dll':{'literals':[0x693d00,0x6190ec,0x69a2b8],'thunks':[0x593560]}}
result=[]
for rel,sel in plans.items():
 b=(root/rel).read_bytes();u16=lambda p:struct.unpack_from('<H',b,p)[0];u32=lambda p:struct.unpack_from('<I',b,p)[0];coff=u32(0x3c)+4;opt=coff+20;base=struct.unpack_from('<Q',b,opt+24)[0];dd=opt+112;sections=[]
 for i in range(u16(coff+2)):
  p=opt+u16(coff+16)+i*40;sections.append((u32(p+12),max(u32(p+8),u32(p+16)),u32(p+20)))
 def off(r):
  for s,n,p in sections:
   if s<=r<s+n:return p+r-s
  raise ValueError('Unmapped')
 def cstr(r):
  p=off(r);return b[p:b.index(b'\0',p)].decode('ascii',errors='replace')
 iat={};ir=u32(dd+8)
 if ir:
  p=off(ir)
  for i in range(1024):
   oft,_,_,name,ft=struct.unpack_from('<IIIII',b,p+20*i)
   if not any([oft,name,ft]):break
   table=off(oft or ft)
   for k in range(100000):
    v=struct.unpack_from('<Q',b,table+k*8)[0]
    if not v:break
    iat[ft+k*8]={'module':cstr(name),'symbol':'ordinal:'+str(v&65535) if v>>63 else cstr(v+2)}
 literals=[];thunks=[]
 for r in sel['literals']:
  p=off(r);raw=b[p:p+80];s=raw.split(b'\0',1)[0].decode('ascii',errors='replace')
  literals.append({'rva':hex(r),'preferredVA':hex(base+r),'fileOffset':hex(p),'asciiUntilNUL':s,'first16Hex':raw[:16].hex(' ')})
 for r in sel['thunks']:
  p=off(r);raw=b[p:p+32];item={'rva':hex(r),'preferredVA':hex(base+r),'first32Hex':raw.hex(' ')}
  if raw[:2]==b'\xff\x25':
   dest=r+6+struct.unpack_from('<i',raw,2)[0];item.update({'kind':'x64 RIP-relative indirect JMP thunk','iatRVA':hex(dest),'import':iat.get(dest)})
  else:item['kind']='not recognized as simple import thunk; no behavioral inference'
  thunks.append(item)
 result.append({'module':rel,'literals':literals,'thunks':thunks,'limitations':'Bytes from original binary constants only; no constructed path is resolved or opened. Literal interpretation alone does not prove full format or complete path lifecycle.'})
with (out/'resolved-constants-and-thunk.json').open('x',encoding='utf8') as f:json.dump(result,f,indent=2)
print(json.dumps(result))
