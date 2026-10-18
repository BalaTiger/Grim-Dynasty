from pathlib import Path
import json,subprocess
out=Path(r'D:\MyGitRepo\Grim-Dynasty\docs\phase-1\round-4\followup');out.mkdir(exist_ok=True)
plan=[{'module':'x64/Engine.dll','functions':[{'start':'0x180073a60','end':'0x180073c86','bytes':551,'symbol':'?LoadMainDatabase@Engine@GAME@@AEAAXXZ','boundary':'PE exception RuntimeFunction'},{'start':'0x1802dc398','end':'0x1802dc3d3','bytes':60,'symbol':'allocation_helper','boundary':'PE exception RuntimeFunction'}]}]
(out/'bounded-function-plan.json').write_text(json.dumps(plan,indent=2),encoding='utf8')
s=Path('run_bounded_ghidra.mjs').read_text(encoding='utf8').replace('/docs/phase-1/round-2','/docs/phase-1/round-4/followup').replace('/analysis-runtime/round-2','/analysis-runtime/round-4-followup').replace('/bounded-runtime/','/bounded-runtime-r4-followup/')
Path('run_round4_followup.mjs').write_text(s,encoding='utf8')
raise SystemExit(subprocess.run(['node','run_round4_followup.mjs']).returncode)
