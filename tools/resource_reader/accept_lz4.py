from pathlib import Path
import hashlib,json,subprocess
from gd_resources import ARZ,ARC,GAME_ROOT,span,lz4_block
root=Path(r'D:\MyGitRepo\Grim-Dynasty');fixtures=root/'.tooling/resource-reader-build/fixtures';fixtures.mkdir(parents=True,exist_ok=True);exe=root/'.tooling/resource-reader-build/Lz4Probe.exe';results=[]
def check(label,packed,size,expected):
 path=fixtures/(str(len(results))+'.lz4block')
 with path.open('xb') as f:f.write(packed)
 r=subprocess.run([str(exe),str(path),str(size)],capture_output=True,text=True,timeout=15)
 assert r.returncode==0,(label,r.stderr)
 actual=json.loads(r.stdout);assert actual['sha256']==expected and actual['decoded_bytes']==size,label
 results.append({'label':label,'packed_bytes':len(packed),'decoded_bytes':size,'expected_sha256':expected,'compiled_result':actual,'match':True})
with ARZ.open(GAME_ROOT/'database/database.arz') as db:
 for name in ['records/skills/playerclass01/_classtraining_class01.dbr','records/items/gearweapons/swords1h/b000_sword.dbr','records/items/questitems/scrapmetal.dbr']:
  e=db.records[name];check(name,span(db.data,e['offset'],e['packed']),e['unpacked'],db.record(name)['body_sha256'])
for archive,member in [('Text_EN.arc','tags_skills.txt'),('Text_EN.arc','tags_items.txt'),('Quests.arc','gdareab/mq_murdersandworse.qst')]:
 with ARC.open(GAME_ROOT/'resources'/archive) as arc:
  e=arc.entries[member]
  for i,(offset,packed,unpacked) in enumerate(arc.parts[e['first']:e['first']+e['parts']]):
   if packed==unpacked:continue
   raw=span(arc.data,offset,packed);body=lz4_block(raw,unpacked);check(archive+'/'+member+':part'+str(i),raw,unpacked,hashlib.sha256(body).hexdigest())
result={'status':'passed','implementation':'own C# source compiled with preinstalled Framework64/v4.0.30319/csc.exe; no original DLL dependency','samples':results,'count':len(results),'total_decoded_bytes':sum(x['decoded_bytes'] for x in results),'limitations':'valid selected blocks cross-checked against Python reader; database fields and archive members independently checked against official ArchiveTool in earlier acceptance; not invalid-input parity proof or game-runtime parity','compiler_sha256':hashlib.sha256(Path(r'C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe').read_bytes()).hexdigest(),'exe_sha256':hashlib.sha256(exe.read_bytes()).hexdigest()}
with (root/'docs/resource-contracts/compiled-lz4-acceptance.json').open('x',encoding='utf8') as f:json.dump(result,f,indent=2)
print(json.dumps({k:result[k] for k in ['status','count','total_decoded_bytes']},indent=2))
