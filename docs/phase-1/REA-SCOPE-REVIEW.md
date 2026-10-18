> 最新状态：用户已选择方案2；Rea6.5.0已备份注册并通过stdio握手（207工具）及open_binary文件识别。原生深度分析后端仍缺失。当前事实以REA-INSTALL-RESULT.md为准，下文保留早期审计历史。

# Rea 6.5.0 路径权限审查与实际预览结果

2026-10-18。用户批准先预览，并希望目录限定：D:\Steam\steamapps\common\Grim Dawn只读，D:\MyGitRepo\Grim-Dynasty可写。若不能强制限制，不注册无约束持久服务。

## 当前结论

**已完成安装预览，未注册MCP。** 包与依赖仅安装到本任务npm缓存用于预览，没有全局安装、个人skill或Hopper安装。未发现6.5.0支持把全套MCP工具全局限制到这两个路径、并分别赋予只读/可写的现成配置。当前不满足目录硬边界要求，因此停止实际注册，等待主会话与用户决定。cwd、提示词、静态分析模式或单次输入路径均不构成权限隔离。不自建包装器假称沙箱，不改OS安全设置。

## 实际执行

最初使用原提案的npx命令，失败于默认npm缓存：

```text
EPERM: operation not permitted, open C:\Users\Administrator\AppData\Local\npm-cache\_cacache\tmp\1f70f30c
```

错误输出显示默认registry为https://registry.npmmirror.com/rea-agents。没有更改全局npm设置，改用命令级官方registry与可写任务缓存，执行：

```powershell
npx --yes --registry=https://registry.npmjs.org --cache='C:\Users\Administrator\Documents\Codex\2026-10-18\task-2\.npm-cache' rea-agents@6.5.0 setup --client codex --skill=false --dry-run --json
```

退出码0，status=planned，appliedActions=[]。仅一个拟议动作configure_client:codex，operation=update，target=C:\Users\Administrator\.codex\config.toml，backupPath=C:\Users\Administrator\.codex\config.toml.rea.backup。拟议启动器：cmd.exe /d /c npx -y rea-agents@6.5.0 mcp。没有额外provider环境变量。配置复查没有[mcp_servers.rea]段。

注意：npx的cache/registry命令级参数不自动固化到以上未来MCP启动器。即使后来允许注册，也要单独审阅未来启动器使用的缓存/registry设置，不能假定本次官方registry覆盖会约束后续调用。

## 硬边界证据

已静态检查核验过SHA512的发布包：

- dist/config/environment.js与parseConfig.js的公开配置覆盖provider路径、超时、schema和日志等，未见全局allowed-roots/read-only-roots或分目录读写权限设置。
- setup参数仅client/skill/yes/dryRun/providers等，没有全局目录ACL参数。
- dist/contracts/sessionToolContracts.js中capture_process_scenario明确描述目标以当前用户权限运行，并声明“this is not a security sandbox”。它能接收任意命令、cwd及环境覆盖；限制cwd不限制文件访问。
- export_evidence_bundle/close_binary等工具可接受调用方绝对输出路径；localPathStringSchema仅检查非空和NUL字符，不是全局允许目录验证。
- 搜索到的isPathWithinRoot用于具体引用导入、应用包和Electron局部范围检查，未构成全套MCP两目录强制访问策略。原生后端的文件身份/DACL/进程所有权检查不能自动等同用户请求的两目录授权列表。

这不是全包形式化安全证明；结论是当前发布接口和实际预览均未提供足以承诺两目录强制隔离的配置，不能注册后声称已做到。

## 实际运行能力阻碍

预览doctor摘要healthy=false，availableProviders=[]：Ghidra未配置，IDA未配置。JADX检查触发Windows native self-test失败：

```text
Open Windows path component failed [C:\Users\ADMINI~1] (Win32 5): 拒绝访问。
```

原包建议检查本地临时目录和文件系统/DACL/Job Object约束。本轮未更改权限、OS安全或临时目录来绕过该错误，未安装分析引擎。即使持久注册成功也不能声称已具备Grim Dawn原生反编译能力。

## 最小权限方案与待决策

当前可继续：保留项目内只读审计脚本与静态证据；已完成的PE标准库分析不需要Rea持久服务。任务缓存可以暂留供审阅，未创建自动启动或配置注册。

若用户坚持两目录硬边界，需要已有宿主/OS沙箱明确提供并实际测试“游戏只读、项目可写、禁止其他用户数据”的子进程及文件策略，且运行时依赖、npm缓存、日志与临时目录也需清楚列为必要例外。当前没有已验证的这类支持；不能用提示词代替。建立VM/账户/容器或改DACL超出本次授权，不执行。

若用户愿意接受当前用户级服务的较宽实际权限，须明确接受“无法硬限两目录”，再另行批准持久注册与后端修复；当前没有这个批准。原版、真实/云存档、Steam设置、DRM/反作弊边界仍保持。