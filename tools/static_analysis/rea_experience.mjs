import fs from 'node:fs';import {spawn} from 'node:child_process';
const base='D:/MyGitRepo/Grim-Dynasty',out=base+'/docs/rewards/rea-experience';if(fs.existsSync(out))throw Error('Evidence exists');fs.mkdirSync(out,{recursive:true});
const p=JSON.parse(fs.readFileSync(base+'/.tooling/installed-paths.json','utf8'));const temp=base+'/.tooling/resource-reader-runtime';fs.mkdirSync(temp,{recursive:true});
const child=spawn('cmd.exe',['/d','/c','npx','-y','rea-agents@6.5.0','mcp'],{cwd:out,env:{...process.env,GHIDRA_INSTALL_DIR:p.ghidra,JAVA_HOME:p.jdk,GHIDRA_HEADLESS_MAXMEM:'512M',TEMP:temp,TMP:temp,npm_config_registry:'https://registry.npmjs.org',npm_config_cache:'C:/Users/Administrator/Documents/Codex/2026-10-18/task-2/.npm-cache'},stdio:['pipe','pipe','pipe']});
let buffer='',stderr='',id=0;const waiting=new Map();child.stderr.on('data',b=>stderr+=b.toString());
child.stdout.on('data',b=>{buffer+=b.toString();let n;while((n=buffer.indexOf('\n'))>=0){const l=buffer.slice(0,n);buffer=buffer.slice(n+1);try{let m=JSON.parse(l);let f=waiting.get(m.id);if(f){waiting.delete(m.id);f(m)}}catch{}}});
function request(method,params){const n=++id;return new Promise((resolve,reject)=>{const t=setTimeout(()=>{waiting.delete(n);reject(Error('timeout '+method))},420000);waiting.set(n,m=>{clearTimeout(t);resolve(m)});child.stdin.write(JSON.stringify({jsonrpc:'2.0',id:n,method,params})+'\n')});}
async function call(name,args,file=name){console.log(name);let r=await request('tools/call',{name,arguments:args});fs.writeFileSync(out+'/'+file+'.json',JSON.stringify(r,null,2));if(r.error||r.result?.isError)throw Error(JSON.stringify(r).slice(0,2000));return r.result.structuredContent??r.result;}
try{let init=await request('initialize',{protocolVersion:'2024-11-05',capabilities:{},clientInfo:{name:'Grim-Dynasty-resource-reader',version:'1'}});fs.writeFileSync(out+'/initialize.json',JSON.stringify(init,null,2));child.stdin.write(JSON.stringify({jsonrpc:'2.0',method:'notifications/initialized'})+'\n');
await call('open_binary',{path:'D:/Steam/steamapps/common/Grim Dawn/x64/Game.dll',provider_id:'ghidra'});
const started=Date.now();let consumed=0,idle=Date.now();
const planPath='C:/Users/Administrator/Documents/Codex/2026-10-18/task-2/rea-experience-plan.json';
while(Date.now()-started<1200000 && consumed<64){
 const plan=JSON.parse(fs.readFileSync(planPath,'utf8'));
 if(consumed<plan.calls.length){
  const item=plan.calls[consumed];
  if(!['procedure_pseudo_code','procedure_assembly','xrefs'].includes(item.tool))throw Error('Unapproved plan tool');
  await call(item.tool,item.args,item.label);consumed++;idle=Date.now();
  fs.writeFileSync(out+'/progress.json',JSON.stringify({consumed,pending:plan.calls.length-consumed,last:item.label}));
 }else if(plan.close){break}else{
  if(Date.now()-idle>300000)break;
  await new Promise(resolve=>setTimeout(resolve,1000));
 }
}
}catch(e){console.error(e.message);process.exitCode=1}finally{try{await call('close_binary',{})}catch{}fs.writeFileSync(out+'/stderr.txt',stderr);child.stdin.end();setTimeout(()=>child.kill(),1000).unref()}


