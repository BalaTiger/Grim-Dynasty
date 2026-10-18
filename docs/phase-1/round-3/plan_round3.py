from pathlib import Path
import struct,json,hashlib
root=Path(r'D:\MyGitRepo\Grim-Dynasty');out=root/'docs/phase-1/round-3';out.mkdir(exist_ok=True)
b=Path(r'D:\Steam\steamapps\common\Grim Dawn\x64\Game.dll').read_bytes();u16=lambda p:struct.unpack_from('<H',b,p)[0];u32=lambda p:struct.unpack_from('<I',b,p)[0];c=u32(60)+4;o=c+20;image=struct.unpack_from('<Q',b,o+24)[0];secs=[]
for i in range(u16(c+2)):
 p=o+u16(c+16)+40*i;secs.append((b[p:p+8].split(b'\0')[0].decode(),u32(p+12),max(u32(p+8),u32(p+16)),u32(p+20),u32(p+16)))
def off(r):return next(z+r-s for name,s,n,z,rawsize in secs if s<=r<s+n)
def rva(p):return next(s+p-z for name,s,n,z,rawsize in secs if z<=p<z+rawsize)
d=o+112;er=u32(d+24);es=u32(d+28);ranges=[struct.unpack_from('<III',b,off(er)+12*i) for i in range(es//12)]
funcs=[]
for start,label in [(0x593560,'save_base_source'),(0x301350,'string_combine_A'),(0x89e10,'string_combine_B')]:
 matches=[(s,e) for s,e,w in ranges if s==start]
 if len(matches)!=1:raise RuntimeError('Ambiguous boundary')
 s,e=matches[0]
 if e-s>1024:raise RuntimeError('Exceeded 1KiB function budget')
 funcs.append({'start':hex(image+s),'end':hex(image+e-1),'bytes':e-s,'symbol':label,'boundary':'PE exception RuntimeFunction'})
hits=[];needle=struct.pack('<I',0x375ea)
for name,s,n,z,rawsize in secs:
 if name!='.text':continue
 pos=z
 while True:
  p=b.find(needle,pos,z+rawsize)
  if p<0:break
  rv=rva(p);owner=[(st,en) for st,en,w in ranges if st<=rv<en]
  kind='unclassified raw displacement occurrence'
  if p>=2 and b[p-2]==0xc6 and b[p-1] in range(0x80,0x88):kind='candidate immediate byte store'
  elif p>=2 and b[p-2]==0x88 and 0x80<=b[p-1]<=0xbf:kind='candidate register-to-byte store'
  elif p>=2 and b[p-2]==0x80 and 0xb8<=b[p-1]<=0xbf:kind='candidate byte compare'
  hits.append({'displacementFileOffset':hex(p),'displacementRVA':hex(rv),'candidateKind':kind,'contextHex':b[max(z,p-8):p+12].hex(' '),'ownerCandidates':[{'start':hex(image+a),'bytes':e-a} for a,e in owner]});pos=p+1
plan=[{'module':'x64/Game.dll','functions':funcs}]
(out/'bounded-function-plan.json').write_text(json.dumps(plan,indent=2),encoding='utf8')
(out/'state-displacement-candidates.json').write_text(json.dumps({'module':'x64/Game.dll','sha256':hashlib.sha256(b).hexdigest(),'offset':'0x375ea','hits':hits,'limitations':'One finite .text byte scan. Matches are candidates until decoded; same displacement can belong to unrelated object types. No state or save data read.'},indent=2),encoding='utf8')
print(json.dumps({'functions':funcs,'stateHitCount':len(hits),'storeCandidates':[h for h in hits if 'store' in h['candidateKind']]}))
