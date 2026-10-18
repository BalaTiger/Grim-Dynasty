from pathlib import Path
import subprocess, json, hashlib, sys
game=Path(r'D:\Steam\steamapps\common\Grim Dawn')
out=Path(r'D:\MyGitRepo\Grim-Dynasty\.tooling\resource-reader-validation')
out.mkdir(parents=True,exist_ok=True)
samples=[('database/database.arz','-database','mastery.dbr','records/skills/playerclass01/_classtraining_class01.dbr'),('database/database.arz','-database','sword.dbr','records/items/gearweapons/swords1h/b000_sword.dbr'),('database/database.arz','-database','scrap.dbr','records/items/questitems/scrapmetal.dbr'),('resources/Text_EN.arc','-extract','tags','tags_skills.txt'),('resources/Text_EN.arc','-extract','tags','tags_items.txt'),('resources/Quests.arc','-extract','quest','gdareab/mq_murdersandworse.qst')]
reports=[]
for archive,mode,dest,member in samples:
    if '--quest-only' in sys.argv and mode=='-database':continue
    if '--quest-only' in sys.argv and not member.endswith('.qst'):continue
    if mode=='-extract':(out/dest).mkdir(parents=True,exist_ok=True)
    if mode=='-extract':(out/dest/Path(member).parent).mkdir(parents=True,exist_ok=True)
    cmd=[str(game/'ArchiveTool.exe'),str(game/archive),mode,str(out/dest),member]
    try:r=subprocess.run(cmd,cwd=out,capture_output=True,timeout=20)
    except subprocess.TimeoutExpired as e:
        reports.append({'command':cmd,'error':'timeout; child killed by subprocess.run','stdout':(e.stdout or b'').decode('utf8',errors='replace')})
        continue
    reports.append({'command':cmd,'exit_code':r.returncode,'stdout':r.stdout.decode('utf8',errors='replace'),'stderr':r.stderr.decode('utf8',errors='replace')})
files=[{'path':str(p.relative_to(out)),'bytes':p.stat().st_size,'sha256':hashlib.sha256(p.read_bytes()).hexdigest()} for p in out.rglob('*') if p.is_file()]
(out/('commands-quest.json' if '--quest-only' in sys.argv else 'commands.json')).write_text(json.dumps({'commands':reports,'files':files},indent=2),encoding='utf8')
print(json.dumps({'commands':reports,'files':files},indent=2))
