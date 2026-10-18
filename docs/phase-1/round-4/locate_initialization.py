from pathlib import Path
import struct,json,hashlib,re
G=Path(r'D:\Steam\steamapps\common\Grim Dawn');O=Path(r'D:\MyGitRepo\Grim-Dynasty\docs\phase-1\round-4');O.mkdir(exist_ok=True)
class PE:
 def __init__(self,rel):
  self.rel=rel;self.b=(G/rel).read_bytes();b=self.b;self.u16=lambda p:struct.unpack_from('<H',b,p)[0];self.u32=lambda p:struct.unpack_from('<I',b,p)[0];c=self.u32(60)+4;o=c+20;self.base=struct.unpack_from('<Q',b,o+24)[0];self.dd=o+112;self.sections=[]
  for i in range(self.u16(c+2)):
   p=o+self.u16(c+16)+40*i;self.sections.append((b[p:p+8].split(b'\0')[0].decode(),self.u32(p+12),self.u32(p+16),self.u32(p+20)))
  er=self.u32(self.dd+24);es=self.u32(self.dd+28);self.ranges=[struct.unpack_from('<III',b,self.off(er)+i*12)[:2] for i in range(es//12)] if er else []
 def off(self,r):return next(p+r-s for n,s,size,p in self.sections if s<=r<s+size)
 def string(self,r):
  p=self.off(r);return self.b[p:self.b.index(b'\0',p)].decode('ascii',errors='replace')
 def owner(self,r):return [{'start':hex(self.base+s),'rva':hex(s),'bytes':e-s} for s,e in self.ranges if s<=r<e]
 def exports(self):
  er=self.u32(self.dd)
  if not er:return []
  p=self.off(er);nt=self.off(self.u32(p+32));ot=self.off(self.u32(p+36));ft=self.off(self.u32(p+28))
  return [{'symbol':self.string(self.u32(nt+i*4)),'rva':self.u32(ft+self.u16(ot+i*2)*4)} for i in range(self.u32(p+24))]
 def imports(self):
  ir=self.u32(self.dd+8);result={}
  if not ir:return result
  p=self.off(ir)
  for i in range(1024):
   oft,_,_,name,ft=struct.unpack_from('<IIIII',self.b,p+i*20)
   if not any([oft,name,ft]):break
   tab=self.off(oft or ft)
   for k in range(100000):
    v=struct.unpack_from('<Q',self.b,tab+k*8)[0]
    if not v:break
    result[ft+k*8]={'module':self.string(name),'symbol':'ordinal:'+str(v&65535) if v>>63 else self.string(v+2)}
  return result
main=PE('x64/Grim Dawn.exe');iat=main.imports()
pattern=re.compile(r'^\?\?0Engine@|^\?\?1Engine@|^\?Initialize@Engine@|^\?InitializeMod@Engine@|^\?AddSource@FileSystem@|^InitializeObjects$|^ShutdownObjects$|^\?PostLuaInitialize@GameEngine@|^\?PreLuaShutdown@GameEngine@|^\?Initialize@LuaManager@')
selected={r:x for r,x in iat.items() if pattern.search(x['symbol'])}
sites=[]
for n,s,size,p in main.sections:
 if n!='.text':continue
 for i in range(p,p+size-6):
  if main.b[i:i+2] not in [b'\xff\x15',b'\xff\x25']:continue
  r=s+i-p;dest=r+6+struct.unpack_from('<i',main.b,i+2)[0]
  if dest in selected:sites.append({'address':hex(main.base+r),'rva':hex(r),'shape':'RIP-relative CALL' if main.b[i+1]==0x15 else 'RIP-relative JMP','targetIAT':hex(dest),'import':selected[dest],'ownerCandidates':main.owner(r),'caveat':'Byte-pattern candidate until verified by disassembly; import occurrence is not execution order.'})
exports=[]
for rel in ['x64/Engine.dll','x64/Game.dll']:
 pe=PE(rel);found=[]
 for x in pe.exports():
  if re.search(r'^\?\?0Engine@|^\?\?0GameEngine@|^\?Initialize@Engine@|^\?InitializeMod@Engine@|^\?AddSource@FileSystem@|^\?AddSourceArchive@FileSystem@|^InitializeObjects$|^ShutdownObjects$|^\?PostLuaInitialize@GameEngine@|^\?PreLuaShutdown@GameEngine@|^\?Initialize@FileSystem@',x['symbol']):
   found.append({'symbol':x['symbol'],'rva':hex(x['rva']),'address':hex(pe.base+x['rva']),'bounds':pe.owner(x['rva'])})
 exports.append({'module':rel,'sha256':hashlib.sha256(pe.b).hexdigest(),'symbols':found})
alloc=[]
for r,x in iat.items():
 if re.search(r'(^malloc$|^free$|^calloc$|^realloc$|^_aligned_|^\?\?2@|^\?\?3@|Allocate@|Deallocate@|Alloc@|Free@)',x['symbol']):alloc.append(x)
j={'mainSHA256':hashlib.sha256(main.b).hexdigest(),'selectedImports':list(selected.values()),'candidateCallSites':sites,'initializationExports':exports,'allocationImports':alloc,'scanScope':'One finite pass over main .text for FF15/FF25 candidates only; no target execution or recursive analysis.'}
with (O/'initialization-locations.json').open('x',encoding='utf8') as f:json.dump(j,f,indent=2)
print(json.dumps({'sites':sites,'exports':exports,'allocationImports':alloc}))
