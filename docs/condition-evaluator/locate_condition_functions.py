from plan_resource_contracts import pe_info
from pathlib import Path
import struct,json
b,image,off,exports,ranges=pe_info('x64/Game.dll')
pe=struct.unpack_from('<I',b,0x3c)[0];coff=pe+4;opt=coff+20;sections=[]
for i in range(struct.unpack_from('<H',b,coff+2)[0]):
 p=opt+struct.unpack_from('<H',b,coff+16)[0]+i*40
 size,rva,rawsize,raw=struct.unpack_from('<IIII',b,p+8);sections.append((rva,max(size,rawsize),raw))
def rva(file_offset):
 for start,size,raw in sections:
  if raw<=file_offset<raw+size:return file_offset-raw+start
 raise ValueError('not mapped')
result=[]
for cls in ['IsDifficulty','IsHardcore','IsLevel','WaitForCompletion']:
 name=('TypeDescriptor:ScriptableCondition_'+cls);needle=('.?AVScriptableCondition_'+cls+'@GAME@@\0').encode();pos=b.find(needle)
 if pos<0:result.append({'class':cls,'status':'RTTI name not found'});continue
 td=rva(pos-16);locators=[];seek=0
 while True:
  at=b.find(struct.pack('<I',td),seek)
  if at<0:break
  seek=at+1;start=at-12
  if start<0:continue
  sig,disp,cd,ty,ch,selfrva=struct.unpack_from('<6I',b,start)
  if sig!=1 or selfrva!=rva(start):continue
  refs=[];seek2=0
  while True:
   p=b.find(struct.pack('<Q',image+selfrva),seek2)
   if p<0:break
   seek2=p+1;vt=p+8;funcs=[]
   for i in range(8):
    va=struct.unpack_from('<Q',b,vt+8*i)[0];rel=va-image
    if rel<0 or rel>=len(b)*2:break
    funcs.append({'slot':i,'address':hex(va),'runtimefunction_bytes':ranges.get(rel,rel)-rel})
   refs.append({'vtable':hex(image+rva(vt)),'slots':funcs})
  locators.append({'locator':hex(image+selfrva),'offset':disp,'vtables':refs})
 result.append({'class':cls,'type_descriptor':hex(image+td),'locators':locators})
factory=[{'name':n,'address':hex(image+r),'bytes':ranges.get(r,r)-r} for n,r in exports.items() if n.startswith('?Create@ScriptableCondition@')]
out={'operation':'read-only RTTI/vtable candidates; slot semantics require disassembly confirmation','classes':result,'factory':factory}
Path('condition-function-locations.json').write_text(json.dumps(out,indent=2));print(json.dumps(out,indent=2))
