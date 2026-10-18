"""Differential acceptance of independently compiled containers. No game launch.

Reads only authorized resources, existing official samples and own fixtures.
Creates a new JSON report; preserves existing report paths.
"""
import argparse,hashlib,json,struct,subprocess,time
from pathlib import Path
from gd_resources import ARZ,ARC,GAME_ROOT
from accept_resources import compare_dbr
ROOT=Path(r'D:\MyGitRepo\Grim-Dynasty')
EXE=ROOT/'.tooling/resource-containers-build/ResourceProbe.exe'
def hash_file(p):
 with p.open('rb') as f:return hashlib.file_digest(f,'sha256').hexdigest()
def call(path,*args):
 p=subprocess.run([str(EXE),str(path),*args],capture_output=True,text=True,encoding='utf8',timeout=90)
 assert p.returncode==0,(args,p.returncode,p.stderr)
 return json.loads(p.stdout)
def typed_digest(r):
 out=bytearray()
 for k in sorted(r['fields']):
  name=k.encode();typ=r['field_types'][k];value=r['fields'][k];values=value if isinstance(value,list) else [value]
  out.extend(struct.pack('<I',len(name)));out.extend(name);out.extend(struct.pack('<HI',typ,len(values)))
  for x in values:
   if typ==2:b=x.encode();out.extend(struct.pack('<I',len(b)));out.extend(b)
   elif typ==1:out.extend(struct.pack('<f',x))
   else:out.extend(struct.pack('<i',x))
 return hashlib.sha256(out).hexdigest()
def rows(path):
 p=subprocess.Popen([str(EXE),str(path),'--verify-all'],stdout=subprocess.PIPE,stderr=subprocess.PIPE,text=True,encoding='utf8')
 try:
  for line in p.stdout:yield json.loads(line)
  error=p.stderr.read();code=p.wait(timeout=10);assert code==0,(path,code,error)
 finally:
  if p.poll() is None:p.kill();p.wait()

def run():
 start=time.monotonic();paths=[GAME_ROOT/'database/database.arz',GAME_ROOT/'resources/Text_EN.arc',GAME_ROOT/'resources/Quests.arc'];before={str(p):hash_file(p) for p in paths};report={}
 with ARZ.open(paths[0]) as db:
  index=call(paths[0],'--index');assert index['header']==db.header
  actual={x['name'].replace('\\','/').casefold():x for x in index['entries']};assert actual==db.records,'ARZ index mismatch'
  print('ARZ complete index matched: '+str(len(actual)),flush=True)
  samples=[]
  for name,export in [('records/skills/playerclass01/_classtraining_class01.dbr','mastery.dbr'),('records/items/gearweapons/swords1h/b000_sword.dbr','sword.dbr'),('records/items/questitems/scrapmetal.dbr','scrap.dbr')]:
   r=call(paths[0],'--record',name);py=db.record(name)
   assert r==py,('sample full decoded value mismatch',name)
   # JSON represents integral-valued floats without a decimal point; retain
   # type from the explicitly decoded field_types before official CSV compare.
   for field,typ in r['field_types'].items():
    if typ==1:r['fields'][field]=[float(x) for x in r['fields'][field]] if isinstance(r['fields'][field],list) else float(r['fields'][field])
   official=compare_dbr(r,ROOT/'.tooling/resource-reader-validation'/export)
   samples.append({'name':name,'full_python_fields_equal':True,'official_dbr_comparison':official})
  count=0;fields=0;aggregate=hashlib.sha256();seen=set()
  for r in rows(paths[0]):
   name=r['name'];assert name not in seen;seen.add(name);py=db.record(name)
   assert r['class']==py['class'] and r['body_sha256']==py['body_sha256'] and r['field_count']==len(py['fields']) and r['typed_digest']==typed_digest(py),name
   aggregate.update(json.dumps(r,sort_keys=True,separators=(',',':')).encode());count+=1;fields+=r['field_count']
   if count%5000==0:print('ARZ decoded values matched: '+str(count),flush=True)
  assert count==len(db.records)
  report['arz']={'index_records_compared':count,'record_bodies_compared':count,'decoded_fields_compared':fields,'comparison':'record body SHA256 plus independently canonicalized decoded names/types/array counts/values; float32 raw bits preserved','aggregate_sha256':aggregate.hexdigest(),'official_samples':samples}
 for path in paths[1:]:
  with ARC.open(path) as arc:
   index=call(path,'--index');assert index['header']==arc.header
   actual={x['name'].replace('\\','/').casefold():x for x in index['entries']};assert actual==arc.entries,'ARC index mismatch'
   count=0;total=0;seen=set()
   for r in rows(path):
    assert r['name'] not in seen;seen.add(r['name']);body=arc.read(r['name'])
    assert r['bytes']==len(body) and r['sha256']==hashlib.sha256(body).hexdigest();count+=1;total+=len(body)
   assert count==len(arc.entries)
   report[path.name]={'index_entries_compared':count,'members_decompressed_and_checked':count,'bytes_compared':total,'adler32_verified_by_both':True}
   print(path.name+' all members matched: '+str(count),flush=True)
 official=[]
 for path,member,file in [(paths[1],'tags_skills.txt','tags/tags_skills.txt'),(paths[1],'tags_items.txt','tags/tags_items.txt'),(paths[2],'gdareab/mq_murdersandworse.qst','quest/gdareab/mq_murdersandworse.qst')]:
  r=call(path,'--member',member);expected=hash_file(ROOT/'.tooling/resource-reader-validation'/file);assert r['sha256']==expected
  official.append({'member':member,'compiled_matches_official_sha256':True,'sha256':expected})
 after={str(p):hash_file(p) for p in paths};assert before==after
 report.update({'status':'passed','official_arc_samples':official,'input_sha256_before':before,'source_hashes_unchanged':True,'elapsed_seconds':round(time.monotonic()-start,3),'library_sha256':hash_file(EXE.parent/'GrimDynasty.Resources.dll'),'exe_sha256':hash_file(EXE),'scope':'base database and English-text/quest archives only; no DLC merge, game execution, writes/repacking or saves'})
 return report
if __name__=='__main__':
 ap=argparse.ArgumentParser(description=__doc__);ap.add_argument('--output',type=Path,required=True);a=ap.parse_args()
 if a.output.exists():raise FileExistsError(a.output)
 r=run()
 with a.output.open('x',encoding='utf8') as f:json.dump(r,f,ensure_ascii=False,indent=2)
 print(json.dumps({k:r[k] for k in ['status','elapsed_seconds','source_hashes_unchanged']},indent=2))
