> 最新状态：用户已选择方案2；Rea6.5.0已备份注册并通过stdio握手（207工具）及open_binary文件识别。原生深度分析后端仍缺失。当前事实以REA-INSTALL-RESULT.md为准，下文保留早期审计历史。

> 后续状态更新：已获准执行并完成6.5.0 dry-run，status=planned、appliedActions=[]；已执行预览及缓存安装的细节、路径限制审查和native self-test失败见REA-SCOPE-REVIEW.md。尚未注册MCP；以下“未执行setup/仅静态读取”描述仅适用于此前阶段。两目录硬边界未能验证，禁止据此直接执行实际注册命令。
# Rea 6.5.0 精确审批材料 v2

状态：只下载并静态阅读发布包，没有安装、执行其代码、运行setup/doctor、修改MCP配置或接通工具。

## 已核实版本与来源

2026-10-18，主会话06:16 UTC证据及本执行环境Node正常HTTPS独立复核一致：

- https://registry.npmjs.org/rea-agents/6.6.0 → HTTP404；早先根据GitHub main提出6.6.0命令已撤回，禁止执行。
- https://registry.npmjs.org/rea-agents/latest → HTTP200，version=6.5.0。
- https://registry.npmjs.org/rea-agents/6.5.0 → HTTP200，package.json版本6.5.0。
- 发布包：https://registry.npmjs.org/rea-agents/-/rea-agents-6.5.0.tgz ，本地下载5,634,033 bytes。
- SHA-512 integrity：sha512-C6BG7LyrAdR74mSjertM6PrxJkcNZ2bfQNOJornPo9erDEp2tyPRVYPnV5Yodr+1RqewCQla68WulXlVnveD+A==，本机计算完全匹配registry与主会话证据。
- Node engines：^22.19.0 || ^24.11.0 || >=26.0.0；本机v24.14.1满足。没有关闭TLS验证。

来源身份为用户指定https://github.com/morluto/rea链接的npm包rea-agents。完整性匹配证明下载与registry元数据一致，不等同于对全部代码及依赖的安全审计；本轮没有验证registry签名或provenance证明。GitHub猜测的两个6.5.0标签均404，所以以实际npm发布包作为版本行为证据，不用main冒充6.5.0。

## 6.5.0包内只读行为核对

用已有Python标准库tarfile直接读取归档成员，未导入执行包内JS，未extractall或安装依赖。

| 包内文件 | 核实行为 |
|---|---|
| dist/cli/setupCommands.js | setup支持client数组（可选codex）、skill布尔、dryRun布尔、yes布尔、installHopper默认false、allDetected默认false；dryRun导致approved=false，显式client范围限定本次配置目标 |
| dist/cliOutput.js | 识别--json通用输出参数 |
| dist/application/SupportedClients.js | codex目录取环境CODEX_HOME，否则home/.codex，config.toml，格式toml |
| dist/application/Setup.js | dryRun在应用配置动作之前返回planned；未实际运行验证任何预览输出 |
| dist/application/ClientRegistrationIdentity.js | Windows包执行器注册为cmd.exe /d /c npx -y rea-agents@6.5.0 mcp，版本来自包元数据 |
| dist/application/ClientConfigurationDocument.js | TOML服务器键mcp_servers，保留/合并无关配置的实现；仍须实际计划和读回核验 |
| dist/application/SetupClientConfiguration.js | 预检格式/路径，创建.rea.backup与必要恢复备份，配置后读回；Codex加入startup_timeout_sec=30 |

本会话CODEX_HOME未设置，预期宿主配置精确路径：C:\Users\Administrator\.codex\config.toml；备份候选：同路径后缀.rea.backup，若已有不同快照可能新增.rea.backup.<sha256>.recovery。最终以实际预览计划为准，不改已有无关项。

预期注册形态（只是审阅说明，不是已写配置）：

```toml
[mcp_servers.rea]
command = "cmd.exe"
args = ["/d", "/c", "npx", "-y", "rea-agents@6.5.0", "mcp"]
startup_timeout_sec = 30
```

## 需用户批准的精确命令

第一步仅审批下载/执行6.5.0及其依赖来产生计划，暂不批准持久配置：

```powershell
npx --yes rea-agents@6.5.0 setup --client codex --skill=false --dry-run --json
```

第二步必须在第一步真实计划明确后审批，下面是拟议实际安装缓存及持久注册命令；若预览路径或权限与本提案不同需重新审阅：

```powershell
npx --yes rea-agents@6.5.0 setup --client codex --skill=false --yes --json
```

第一个--yes属于npx（允许获取包）；setup后面的--yes才是Rea应用配置批准。--json保证结构化非交互行为；--client codex不注册其他宿主；--skill=false不安装个人skill；不使用--install-hopper、--all-detected或全局npm install。npx仍会获取依赖并写npm缓存，setup可能产生自身日志/状态，不能称为纯文件只读操作；具体目录需从预览和运行环境核验。

## 持久访问能力与操作限制

这是用户级stdio MCP，供该Codex宿主持续调用，不天然限定D:\MyGitRepo\Grim-Dynasty。原生静态分析可能还需要外部反编译器；本次命令不批准安装这些工具，不保证配置成功就可反编译。

Rea工具可覆盖二进制、应用、进程和网络分析；只读使用承诺不是硬权限隔离。若以后批准，任务操作范围限定已列原安装文件的静态读取及工程内分析输出；禁止访问任何云/真实存档、修改原版/Steam设置、进程附加或注入、DRM/反作弊绕过。不能把注册全套MCP说成已授予“静态分析专用权限”。若宿主无法强制白名单，需把这点作为批准时明确接受或缩小的访问范围。

当前本机PATH无rea、会话无Rea工具、配置无rea项，仍未接通。早先Invoke-RestMethod/curl的TLS错误是特定客户端历史失败；Node正常HTTPS现已验证可用，发布包核验不再阻塞。持久配置批准仍未获得，setup从未执行。