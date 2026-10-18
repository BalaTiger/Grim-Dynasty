"""Differential test of own C# quest module; no original executable/Steam use."""
import argparse,collections,hashlib,json,struct,subprocess
from pathlib import Path
from gd_resources import ARC,GAME_ROOT
from quest_structure import quest_structure
ROOT=Path(r'D:\MyGitRepo\Grim-Dynasty');BUILD=ROOT/'.tooling/quest-definition-build';EXE=BUILD/'QuestProbe.exe'
def digest(path):
 with path.open('rb') as f:return hashlib.file_digest(f,'sha256').hexdigest()
def same(a,b,path='root'):
 if isinstance(a,dict):
  assert isinstance(b,dict) and a.keys()==b.keys(),(path,'keys')
  for k in a:same(a[k],b[k],path+'.'+k)
 elif isinstance(a,list):
  assert isinstance(b,list) and len(a)==len(b),(path,'length')
  for i,(x,y) in enumerate(zip(a,b)):same(x,y,path+'['+str(i)+']')
 elif isinstance(a,bool):assert type(b) is bool and a==b,(path,a,b)
 elif isinstance(a,float):assert not isinstance(b,bool) and struct.pack('<f',a)==struct.pack('<f',b),(path,a,b)
 else:assert not isinstance(b,bool) and a==b,(path,a,b)
def semantic(b):
 result=quest_structure(b);result.pop('coverage');return result
def execute(*args):
 p=subprocess.run([str(EXE),*map(str,args)],capture_output=True,text=True,encoding='utf8',timeout=30)
 assert p.returncode==0,p.stderr
 return json.loads(p.stdout)
def run():
 archive=GAME_ROOT/'resources/Quests.arc';before=digest(archive);totals=collections.Counter();types=collections.Counter();seen=set();aggregate=hashlib.sha256();sample=None
 with ARC.open(archive) as arc:
  p=subprocess.Popen([str(EXE),str(archive),'--all'],stdout=subprocess.PIPE,stderr=subprocess.PIPE,text=True,encoding='utf8')
  try:
   for line in p.stdout:
    row=json.loads(line);name=row['name'];assert name not in seen;seen.add(name);py=semantic(arc.read(name));same(py,row['definition'],name)
    q=row['definition'];totals['quests']+=1;totals['tasks']+=len(q['tasks'])
    for task in q['tasks']:
     for group in ['on_accept','objectives','on_complete']:
      totals[group+'_nodes']+=len(task[group])
      for node in task[group]:
       for s in node['conditions']['conditions']:totals['conditions']+=1;types['condition:'+s['type']+':v'+str(s['version'])]+=1
       for s in node['actions']:totals['actions']+=1;types['action:'+s['type']+':v'+str(s['version'])]+=1
    aggregate.update(json.dumps(row,sort_keys=True,separators=(',',':'),ensure_ascii=True).encode())
    if name=='gdareab/mq_murdersandworse.qst':sample=q
   error=p.stderr.read();code=p.wait(timeout=10);assert code==0,(code,error)
  finally:
   if p.poll() is None:p.kill();p.wait()
  assert len(seen)==len(arc.entries)==247
 print('All 247 compiled quest definitions match Python, including offsets, typed payloads and text slots',flush=True)
 official=ROOT/'.tooling/resource-reader-validation/quest/gdareab/mq_murdersandworse.qst';fixture_dir=BUILD/'fixtures';fixture_dir.mkdir(exist_ok=True)
 fixture=fixture_dir/'official-murders-and-worse.fixture'
 if fixture.exists():assert fixture.read_bytes()==official.read_bytes(),'Existing official fixture differs; not overwritten'
 else:
  with fixture.open('xb') as f:f.write(official.read_bytes())
 q=execute('--fixture',fixture);same(sample,q,'official sample');same(semantic(official.read_bytes()),q,'official/Python')
 after=digest(archive);assert before==after
 return {'status':'passed','totals':dict(totals),'script_type_version_counts':dict(sorted(types.items())),'comparison':'entire decoded definitions incl IDs/flags/node boundaries/conditions/actions/Lua payloads/localized text slots; Python coverage prose excluded; floats compared as raw float32 bits and booleans checked as booleans','aggregate_sha256':aggregate.hexdigest(),'official_sample':{'name':'gdareab/mq_murdersandworse.qst','sha256':digest(official),'compiled_full_structure_matches_official_sample':True,'task_uids':[t['uid'] for t in sample['tasks']]},'source_archive_sha256':before,'source_hash_unchanged':True,'module_sha256':digest(BUILD/'GrimDynasty.Quests.dll'),'cli_sha256':digest(EXE),'resource_module_sha256':digest(BUILD/'GrimDynasty.Resources.dll'),'dependencies':'own compiled Resources module and .NET only; no original DLLs or Steam API','not_recovered':'condition evaluation, task progression rules, action execution, Lua bodies, game engine, original save format; no original game run'}
if __name__=='__main__':
 ap=argparse.ArgumentParser(description=__doc__);ap.add_argument('--output',type=Path,required=True);a=ap.parse_args()
 if a.output.exists():raise FileExistsError(a.output)
 r=run()
 with a.output.open('x',encoding='utf8') as f:json.dump(r,f,ensure_ascii=False,indent=2)
 print(json.dumps({'status':r['status'],'totals':r['totals']},indent=2))
