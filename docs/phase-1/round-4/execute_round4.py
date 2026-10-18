from pathlib import Path
import runpy,subprocess
runpy.run_path('plan_round4.py',run_name='__main__')
s=Path('run_bounded_ghidra.mjs').read_text(encoding='utf8').replace('/docs/phase-1/round-2','/docs/phase-1/round-4').replace('/analysis-runtime/round-2','/analysis-runtime/round-4').replace('/bounded-runtime/','/bounded-runtime-r4/')
old="fs.copyFileSync('C:/Users/Administrator/Documents/Codex/2026-10-18/task-2/BoundedBoundaryProbe.java',out+'/BoundedBoundaryProbe.java');"
new="fs.writeFileSync(out+'/BoundedBoundaryProbe.java',fs.readFileSync('C:/Users/Administrator/Documents/Codex/2026-10-18/task-2/BoundedBoundaryProbe.java','utf8').replace('>2048','>4096'));"
assert old in s
Path('run_round4_ghidra.mjs').write_text(s.replace(old,new),encoding='utf8')
raise SystemExit(subprocess.run(['node','run_round4_ghidra.mjs']).returncode)
