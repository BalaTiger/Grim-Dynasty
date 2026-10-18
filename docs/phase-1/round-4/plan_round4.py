from pathlib import Path
import json,struct
out=Path(r'D:\MyGitRepo\Grim-Dynasty\docs\phase-1\round-4');j=json.loads((out/'initialization-locations.json').read_text(encoding='utf8'))
prefixes=['??0Engine@','?Initialize@Engine@','?AddSourceArchive@FileSystem@','InitializeObjects','ShutdownObjects','?PostLuaInitialize@GameEngine@']
plans=[]
for m in j['initializationExports']:
 funcs=[]
 for s in m['symbols']:
  if not any(s['symbol'].startswith(p) for p in prefixes):continue
  bound=s['bounds']
  if len(bound)!=1 or bound[0]['bytes']>4096:raise RuntimeError('Unexpected boundary/budget')
  start=int(s['address'],16);size=bound[0]['bytes'];funcs.append({'start':hex(start),'end':hex(start+size-1),'bytes':size,'symbol':s['symbol'],'boundary':'PE exception RuntimeFunction'})
 if funcs:plans.append({'module':m['module'],'functions':funcs})
(out/'bounded-function-plan.json').write_text(json.dumps(plans,indent=2),encoding='utf8')
# Keep the scan limitation and entry-section evidence as metadata only.
b=Path(r'D:\Steam\steamapps\common\Grim Dawn\x64\Grim Dawn.exe').read_bytes();u16=lambda p:struct.unpack_from('<H',b,p)[0];u32=lambda p:struct.unpack_from('<I',b,p)[0];coff=u32(60)+4;opt=coff+20;entry=u32(opt+16);sections=[]
for i in range(u16(coff+2)):
 p=opt+u16(coff+16)+40*i;name=b[p:p+8].split(b'\0')[0].decode();s=u32(p+12);n=max(u32(p+8),u32(p+16));sections.append({'name':name,'rva':hex(s),'size':n,'containsEntry':s<=entry<s+n})
(out/'main-entry-metadata.json').write_text(json.dumps({'entryRVA':hex(entry),'sections':sections,'directIATCandidateCount':len(j['candidateCallSites']),'limitations':'No wrapper body analysis, unpacking or execution. Empty FF15/FF25 match set does not prove absent calls, encryption or DRM.'},indent=2),encoding='utf8')
print(json.dumps({'functions':plans,'totalExplicitBytes':sum(f['bytes'] for p in plans for f in p['functions'])}))
