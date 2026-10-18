from pathlib import Path
import collections,hashlib,json
from gd_resources import ARC,GAME_ROOT
from quest_structure import quest_structure
path=GAME_ROOT/'resources/Quests.arc';operators=collections.Counter();difficulty=collections.Counter();types=collections.Counter();examples=[]
with ARC.open(path) as archive:
 for member in archive.entries:
  q=quest_structure(archive.read(member))
  for task in q['tasks']:
   for group in ['on_accept','objectives','on_complete']:
    for node in task[group]:
     collection=node['conditions'];operators[(str(collection['operator']),len(collection['conditions'])==0)]+=1
     for condition in collection['conditions']:
      types[(condition['type_id'],condition['type'],condition['version'])]+=1
      if condition['type_id']==4:
       difficulty[(condition['comparison_id'],condition['parameters']['difficulty'])]+=1
       if len(examples)<6:examples.append({'member':member,'task_uid':task['uid'],'group':group,'condition':condition})
with path.open('rb') as f:sha=hashlib.file_digest(f,'sha256').hexdigest()
report={'source_archive':str(path),'sha256':sha,'kind':'official resource parameter evidence, not observed runtime behavior','collections':[{'operator':op,'empty':empty,'count':n} for (op,empty),n in operators.items()],'difficulty_parameters':[{'comparison_id':op,'difficulty':value,'count':n} for (op,value),n in sorted(difficulty.items())],'types':[{'type_id':t,'name':name,'version':v,'count':n} for (t,name,v),n in sorted(types.items())],'examples':examples,'limitation':'Actual catalog only exercises equality and thresholds 0/1/2 for difficulty; other comparison/boundary semantics need native function evidence'}
out=Path(r'D:\MyGitRepo\Grim-Dynasty\docs\condition-evaluator\data-inventory.json')
with out.open('x',encoding='utf8') as f:json.dump(report,f,indent=2)
print(json.dumps({'difficulty_conditions':sum(difficulty.values()),'collection_nodes':sum(operators.values())}))
