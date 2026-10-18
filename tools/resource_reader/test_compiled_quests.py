"""Synthetic boundary tests for own compiled QST module; no original game use."""
import argparse,json,re,struct,subprocess
from pathlib import Path
from test_quest_structure import fixture,u,text
from verify_compiled_quests import semantic,same
ROOT=Path(r'D:\MyGitRepo\Grim-Dynasty');BUILD=ROOT/'.tooling/quest-definition-build';DIR=BUILD/'fixtures';DIR.mkdir(exist_ok=True)
ap=argparse.ArgumentParser(description=__doc__);ap.add_argument('--run-id',required=True);a=ap.parse_args()
if not re.fullmatch('[A-Za-z0-9_-]+',a.run_id):raise ValueError('Invalid run id')
results=[]
def case(name,data,accepted):
 path=DIR/(a.run_id+'-'+name+'.fixture')
 with path.open('xb') as f:f.write(data)
 p=subprocess.run([str(BUILD/'QuestProbe.exe'),'--fixture',str(path)],capture_output=True,text=True,encoding='utf8',timeout=10)
 assert (p.returncode==0)==accepted,(name,p.returncode,p.stderr)
 if accepted:same(semantic(data),json.loads(p.stdout),name)
 else:
  try:semantic(data)
  except (ValueError,UnicodeError):pass
  else:raise AssertionError('Python unexpectedly accepted '+name)
 results.append({'name':name,'expected':'accept' if accepted else 'reject','compiled_exit':p.returncode,'both_implementations_agree':True})
case('typed-token-action',fixture(),True)
for name,data in [('unknown-condition',fixture(condition=999)),('unknown-version',fixture(version=9)),('invalid-bool',fixture(boolean=2)),('unknown-operator',fixture(operator=2)),('extra-graph-byte',fixture(extra=b'x')),('text-slot-count',fixture(strings=4)),('truncated-locale',fixture()[:-1]),('trailing-byte',fixture()+b'x')]:case(name,data,False)
b=bytearray(fixture());struct.pack_into('<I',b,20,10001);case('task-count-limit',b,False)
b=bytearray(fixture());struct.pack_into('<I',b,8,0xffffffff);case('locale-offset-outside',b,False)
b=bytearray(fixture());b[:4]=b'QST3';case('unsupported-magic',b,False)
def custom(lua_condition=False):
 # Independent hand-built wire fixture: one objective, one Lua payload, no events.
 lua=text('chapter.callback')+u(1)+u(4)+b'\0'+struct.pack('<f',1.5)+text('明末𠮷')
 if lua_condition:cond=u(1)+u(0)+u(12)+b'\1'+u(2)+lua+u(2)+u(1)+b'\0'+struct.pack('<f',-0.0)+text('');actions=u(0)
 else:cond=u(0);actions=u(1)+u(11)+b'\1'+lua
 graph=u(10)+u(1)+u(1)+u(20)+u(0)+u(0)+u(1)+u(30)+u(0)+cond+actions+u(0)+b'\0\0'
 strings=['组','任务','步骤','说明','目标𠮷']
 locale=u(2)
 for lang in ['enUS','zhCN']:locale+=text(lang)+u(5)+b''.join(text(s) for s in strings)
 return b'QST2'+u(9)+u(12+len(graph))+graph+locale
case('lua-action-unicode-two-locales',custom(),True)
case('lua-condition-float32-return',custom(True),True)
b=custom();b=b.replace(text('zhCN'),text('enUS'));case('duplicate-locale',b,False)
b=bytearray(fixture());pos=b.index(b'Repair bridge');b[pos]=0xff;case('invalid-utf8',b,False)
report={'status':'passed','count':len(results),'tests':results,'scope':'own compiled decoder versus Python on synthetic valid/invalid definitions; no condition execution, original DLL or Steam'}
destination=ROOT/'docs/compiled-quests';destination.mkdir(exist_ok=True)
with (destination/('boundary-'+a.run_id+'.json')).open('x',encoding='utf8') as f:json.dump(report,f,indent=2)
print(json.dumps({'status':'passed','count':len(results)},indent=2))
