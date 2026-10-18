"""Read-only graph acceptance, including explicit rejection coverage."""
from pathlib import Path
import json,hashlib,collections
from gd_resources import ARC,GAME_ROOT
from quest_structure import quest_structure
SAMPLE='gdareab/mq_murdersandworse.qst'
with ARC.open(GAME_ROOT/'resources/Quests.arc') as arc:
 sample=arc.read(SAMPLE);q=quest_structure(sample);passed=[];rejected=[]
 for name in arc.entries:
  try:r=quest_structure(arc.read(name));passed.append({'name':name,'tasks':len(r['tasks'])})
  except (ValueError,UnicodeError) as e:rejected.append({'name':name,'reason':str(e)})
 official=Path(r'D:\MyGitRepo\Grim-Dynasty\.tooling\resource-reader-validation\quest\gdareab\mq_murdersandworse.qst').read_bytes()
 assert official==sample
 assert quest_structure(official)==q
 assert len(q['tasks'])==4 and q['graph_end']==783
 assert q['tasks'][1]['objectives'][0]['conditions']['conditions'][0]['parameters']['token']=='GD_FARMLANDBRIDGE_REPAIRED'
 assert q['tasks'][1]['on_complete'][0]['actions'][0]['parameters']['task']==q['tasks'][2]['uid']
 assert q['tasks'][2]['on_complete'][0]['actions'][0]['parameters']['task']==q['tasks'][3]['uid']
 assert q['tasks'][0]['on_complete'][0]['actions'][0]['parameters']['amount']==3
 result={'sample':SAMPLE,'sample_graph':q,'official_extracted_sample_bytes_equal':True,'official_extracted_sample_graph_equal':True,'sample_sha256':hashlib.sha256(sample).hexdigest(),'catalog_scan':{'fully_parsed':len(passed),'explicitly_rejected':len(rejected),'passed':passed,'rejected':rejected},'status':'sample graph passed; catalog coverage explicitly bounded'}
 Path('quest-graph-acceptance.json').write_text(json.dumps(result,ensure_ascii=False,indent=2),encoding='utf8')
 print(json.dumps({'fully_parsed':len(passed),'rejected':len(rejected),'sample_passed':True},indent=2))
