import fs from 'node:fs';import path from 'node:path';import {spawn} from 'node:child_process';
const base='D:/MyGitRepo/Grim-Dynasty',out=base+'/docs/phase-1/round-2',tool=base+'/.tooling';
const paths=JSON.parse(fs.readFileSync(tool+'/installed-paths.json','utf8'));
const plans=JSON.parse(fs.readFileSync(out+'/bounded-function-plan.json','utf8'));
fs.copyFileSync('C:/Users/Administrator/Documents/Codex/2026-10-18/task-2/BoundedBoundaryProbe.java',out+'/BoundedBoundaryProbe.java');
const home=tool+'/bounded-runtime/home',temp=tool+'/bounded-runtime/temp',projects=base+'/analysis-runtime/round-2/projects',config=tool+'/bounded-runtime/config',cache=tool+'/bounded-runtime/cache';
for(let p of [home,temp,projects,config,cache])fs.mkdirSync(p,{recursive:true});
if(!fs.existsSync(base+'/analysis-runtime/.gitignore'))fs.writeFileSync(base+'/analysis-runtime/.gitignore','*\n!.gitignore\n');
const env={...process.env,JAVA_HOME:paths.jdk,GHIDRA_HEADLESS_MAXMEM:'512M',TEMP:temp,TMP:temp,HOME:home,USERPROFILE:home,APPDATA:config,LOCALAPPDATA:cache,XDG_CACHE_HOME:cache,GHIDRA_JAVA_OPTIONS:`-Duser.home=${home} -Djava.io.tmpdir=${temp}`};
delete env._JAVA_OPTIONS;delete env.JAVA_TOOL_OPTIONS;delete env.JDK_JAVA_OPTIONS;
for(const p of plans.filter(p=>!process.argv.includes('--engine-only')||p.module.includes('Engine'))){
 const name=path.basename(p.module,'.dll');
 for(const f of p.functions){if(f.start==='0x1800738e0'){f.bytes=25;f.end='0x1800738f8';f.boundary='Verified 25-byte leaf body including local RET and tail JMP';}if(f.start==='0x1800777d0'){f.bytes=8;f.end='0x1800777d7';f.boundary='Verified 8-byte getter ending RET';}}
 const tsv=out+'/'+name+'-functions.tsv';fs.writeFileSync(tsv,p.functions.map(f=>[f.start,f.end,f.symbol].join('\t')).join('\n'));
 const args=[projects,'bounded_'+name,'-import','D:/Steam/steamapps/common/Grim Dawn/'+p.module,'-noanalysis','-readOnly','-deleteProject','-max-cpu','1','-scriptPath',out,'-postScript','BoundedBoundaryProbe.java',tsv,out+'/'+name+'-function-evidence-bounded-v2.json','-log',out+'/'+name+'-headless.log','-scriptlog',out+'/'+name+'-script.log'];
 console.log('Start '+name+' -noanalysis '+p.functions.map(f=>f.bytes).join('+')+' byte function budgets');
 const q=s=>"'"+s.replaceAll("'","''")+"'";
 const wrapper=out+'/'+name+'-launch.ps1';fs.writeFileSync(wrapper,'& '+[paths.ghidra+'/support/analyzeHeadless.bat',...args].map(q).join(' ')+'\nexit $LASTEXITCODE\n');
 await new Promise((resolve,reject)=>{const c=spawn('powershell.exe',['-NoProfile','-File',wrapper],{env,cwd:out,stdio:['ignore','pipe','pipe']});let stdout='',stderr='';const timer=setTimeout(()=>{c.kill();reject(Error('Headless timeout; cleanup outcome needs inspection'));},180000);c.stdout.on('data',b=>{stdout+=b.toString();if(/BOUNDARY_PROBE/.test(b.toString()))console.log(b.toString().trim())});c.stderr.on('data',b=>stderr+=b.toString());c.on('close',code=>{clearTimeout(timer);fs.writeFileSync(out+'/'+name+'-stdout.txt',stdout);fs.writeFileSync(out+'/'+name+'-stderr.txt',stderr);console.log(name+' exit '+code);code===0?resolve():reject(Error(name+' headless failed '+stderr.slice(-1200)))})});
}
fs.writeFileSync(out+'/effective-function-plan.json',JSON.stringify(plans,null,2));
