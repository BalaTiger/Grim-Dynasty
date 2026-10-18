"""Boundary tests against compiled library through own memory-fixture runner."""
import argparse,hashlib,json,struct,subprocess,re
from pathlib import Path
from test_gd_resources import arz,arc
ROOT=Path(r'D:\MyGitRepo\Grim-Dynasty');build=ROOT/'.tooling/resource-containers-build';out=build/'fixtures';out.mkdir(exist_ok=True);results=[]
ap=argparse.ArgumentParser(description=__doc__);ap.add_argument('--run-id',required=True);args=ap.parse_args()
if not re.fullmatch('[A-Za-z0-9_-]+',args.run_id):raise ValueError('Invalid run id')
def case(label,data,kind,success):
 path=out/(args.run_id+'-'+label+'.fixture')
 with path.open('xb') as f:f.write(data)
 p=subprocess.run([str(build/'ResourceFixtureProbe.exe'),str(path),kind,'records/test.dbr' if kind=='arz' else 'test.txt'],capture_output=True,text=True,encoding='utf8',timeout=10)
 assert (p.returncode==0)==success,(label,p.returncode,p.stderr)
 result=json.loads(p.stdout) if success else None
 results.append({'name':label,'expected':'accept' if success else 'reject','exit_code':p.returncode,'passed':True,'error':p.stderr.strip() if not success else None})
 return result
r=case('arz-zero-array-float',arz(),'arz',True)
assert r['fields']=={'zero':0,'name':'Test','arr':[0,-2],'float':1.25}
assert r['field_types']=={'zero':0,'name':2,'arr':3,'float':1}
b=bytearray(arz());st=struct.unpack_from('<I',b,16)[0];old=b[st:];unicode_value='明末𠮷';encoded=unicode_value.encode('utf8');new=old.replace(struct.pack('<I',4)+b'Test',struct.pack('<I',len(encoded))+encoded,1);b[st:]=new;struct.pack_into('<I',b,20,len(new))
r=case('arz-unicode-string',b,'arz',True);assert r['fields']['name']==unicode_value
case('arz-invalid-type',arz(9),'arz',False)
case('arz-invalid-string-index',arz(idx=999),'arz',False)
case('arz-truncated-table',arz()[:-1],'arz',False)
b=bytearray(arz());struct.pack_into('<I',b,20,0xffffffff);case('arz-overflow-extent',b,'arz',False)
b=bytearray(arz());struct.pack_into('<I',b,12,1000001);case('arz-record-count-limit',b,'arz',False)
b=bytearray(arz());struct.pack_into('<H',b,2,4);case('arz-unsupported-version',b,'arz',False)
for multipart in [False,True]:
 r=case('arc-mixed-parts' if multipart else 'arc-raw-member',arc(multipart),'arc',True)
 assert r=={'bytes':11,'sha256':hashlib.sha256(b'hello hello').hexdigest()}
case('arc-wrong-adler32',arc(corrupt=True),'arc',False)
case('arc-truncated-entry',arc()[:-1],'arc',False)
b=bytearray(arc());struct.pack_into('<I',b,16,13);case('arc-invalid-part-table-size',b,'arc',False)
b=bytearray(arc());struct.pack_into('<I',b,len(b)-4,0xffffffff);case('arc-invalid-name-extent',b,'arc',False)
p=subprocess.run([str(build/'ResourceProbe.exe'),str(out/'not-a-game-resource.arz'),'--index'],capture_output=True,text=True,encoding='utf8',timeout=10)
assert p.returncode==2 and 'only authorized installation' in p.stderr
results.append({'name':'production-path-whitelist','expected':'reject before opening','exit_code':p.returncode,'passed':True})
report={'status':'passed','tests':results,'count':len(results),'fixture_provenance':'own synthetic data; no original resources or saves embedded'}
with (ROOT/('docs/resource-contracts/compiled-containers-boundary-'+args.run_id+'.json')).open('x',encoding='utf8') as f:json.dump(report,f,indent=2)
print(json.dumps({'status':'passed','count':len(results)},indent=2))
