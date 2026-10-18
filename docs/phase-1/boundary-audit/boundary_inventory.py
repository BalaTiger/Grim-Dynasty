from pathlib import Path
import struct,hashlib,json,re
ROOT=Path(r'D:\Steam\steamapps\common\Grim Dawn')
OUT=Path(r'D:\MyGitRepo\Grim-Dynasty\docs\phase-1\boundary-audit');OUT.mkdir(exist_ok=True)
queries={
 'resource_database':r'DatabaseArchive|LoadTable|BaseResourceManager|ResourceLoader|FileSystem|FileSourceArchive|ArchiveTool',
 'save_lifecycle':r'SaveGame|SaveManager|SaveSystem|SaveData|PlayerSave|SaveCharacter|LoadCharacter|SaveFile|SaveSession|SavePlayer|CharacterFile|SavePath|SaveDirectory|SaveLocation',
 'game_systems':r'Inventory|Merchant|Faction|QuestManager|QuestSystem|ItemManager|PlayerCharacter|SkillManager',
 'path_configuration':r'(Get|Set|Create|Open|Load|Init)[A-Za-z_]*(Path|Directory|Folder|Archive)|[A-Za-z_]*(Path|Directory|Folder)@(FileSystem|GameEngine|GameInfo)',
 'platform_dependency':r'Steam|Galaxy|GOG|AppId',
}
def parse(rel):
 b=(ROOT/rel).read_bytes();u16=lambda p:struct.unpack_from('<H',b,p)[0];u32=lambda p:struct.unpack_from('<I',b,p)[0]
 pe=u32(0x3c);coff=pe+4;opt=coff+20;is64=u16(opt)==0x20b;dd=opt+(112 if is64 else 96);secs=[]
 for i in range(u16(coff+2)):
  p=opt+u16(coff+16)+i*40;secs.append((u32(p+12),max(u32(p+8),u32(p+16)),u32(p+20)))
 def off(r):
  if r<u32(opt+60):return r
  for start,size,raw in secs:
   if start<=r<start+size:return raw+r-start
  raise ValueError('Unmapped RVA')
 def string(r):
  p=off(r);return b[p:b.index(b'\0',p)].decode('ascii',errors='replace')
 imports=[];ir=u32(dd+8)
 if ir:
  p=off(ir)
  for i in range(1024):
   oft,_,_,name,ft=struct.unpack_from('<IIIII',b,p+20*i)
   if not any([oft,name,ft]):break
   symbols=[];table=off(oft or ft);width=8 if is64 else 4
   for k in range(100000):
    val=struct.unpack_from('<Q' if is64 else '<I',b,table+k*width)[0]
    if val==0:break
    symbols.append('ordinal:'+str(val&0xffff) if val>>(width*8-1) else string(val+2))
   imports.append({'module':string(name),'symbols':symbols})
 exports=[];er=u32(dd)
 if er:
  p=off(er);count=u32(p+24);nt=off(u32(p+32));ordtable=off(u32(p+36));func=off(u32(p+28))
  for i in range(count):
   idx=u16(ordtable+2*i);exports.append({'name':string(u32(nt+4*i)),'rva':hex(u32(func+4*idx))})
 categories={}
 for key,pattern in queries.items():
  matches=[x for x in exports if re.search(pattern,x['name'],re.I)]
  relevant=[{'module':m['module'],'symbols':[s for s in m['symbols'] if re.search(pattern,s,re.I)]} for m in imports]
  relevant=[m for m in relevant if m['symbols']]
  categories[key]={'matchingExportCount':len(matches),'exportExamples':matches[:40],'exampleLimit':40,'matchingImports':relevant}
 strings=[]
 literal=re.compile(r'\.arz\b|\.arc\b|database[/\\]|resources[/\\]|save.*(path|director|folder)|[a-z]*savegame|customgame|modname',re.I)
 for match in re.finditer(rb'[\x20-\x7e]{5,}',b):
  s=match.group().decode('ascii')
  if len(s)<=220 and literal.search(s) and ':\\' not in s:strings.append({'offset':hex(match.start()),'value':s})
 return {'relativePath':rel,'sha256':hashlib.sha256(b).hexdigest(),'bytes':len(b),'namedExportCount':len(exports),'importModules':imports,'categories':categories,'candidateAsciiStringCount':len(strings),'candidateAsciiStringExamples':strings[:60],'stringExampleLimit':60,'limitations':['Static PE ordinary imports/exports only; delay imports and runtime-resolved symbols not covered.','Symbol-name categories and strings are interface candidates, not verified behavior or usable SDK APIs.','Bounded printed/exported examples; complete counts reported. ASCII literals only, not UTF-16 or decompressed resources.']}
items=[parse(p) for p in ['x64/Grim Dawn.exe','x64/Engine.dll','x64/Game.dll','AssetManager.exe','ArchiveTool.exe']]
with (OUT/'module-boundaries.json').open('x',encoding='utf8') as f:json.dump(items,f,indent=2)
for x in items:
 print(json.dumps({'file':x['relativePath'],'exports':x['namedExportCount'],'modules':[{ 'name':m['module'],'symbolCount':len(m['symbols'])} for m in x['importModules'] if m['module'].lower() in ['engine.dll','game.dll','steam_api.dll','steam_api64.dll','lua51.dll']],'categories':{k:v['matchingExportCount'] for k,v in x['categories'].items()},'literalCount':x['candidateAsciiStringCount']}))
