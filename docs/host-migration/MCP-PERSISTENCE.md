# Rea 在当前任务中的 MCP 持续接入

核实日期：2026-10-19。范围：本机 `codex-cli 0.140.0`、工程 MCP 配置、当前任务工具目录及官方客户端文档。本次仅研究接入方式，并在工程内离线生成该版本的协议 schema；没有安装软件、启动 Rea/App Server、连接控制 socket、修改配置或信任、重启应用或创建任务。

## 结论

工程配置已存在，Rea 的临时 stdio 通信已验证成功，但这两项不等于当前任务已经获得 Rea 原生工具。两次 smoke 的子进程均已正常退出；本次再次精确检查当前任务工具目录，没有 Rea 前缀，也没有 `binary_session`、`open_binary`、`close_binary` 的原生工具入口。

官方 App Server 协议确有 `config/mcpServer/reload`：从磁盘重载 MCP 配置，并为已加载线程排队刷新。因此不能断言 Codex 完全不支持热刷新。限制在于：当前委派任务没有暴露该协议调用工具，也未确认当前任务所属宿主的已有控制连接；尚未证明该入口可刷新此任务的工具目录。[官方 App Server 文档](https://learn.chatgpt.com/docs/app-server)

用户要求避免影响其他任务。本机 0.140.0 的 reload 没有服务器或线程过滤参数，会为宿主已加载线程排队刷新，不能承诺只影响当前任务；本轮没有执行它。即使以后取得控制接口，也须先核实客户端是否提供只刷新 Rea 单个连接的能力，以及对其他任务的实际影响范围。不能把全宿主 reload 当作精确的单任务刷新；在影响范围不能满足用户要求时，不执行该操作。

在“不改信任、不改全局配置、不重启应用、不创建任务”的范围内，目前没有已经验证、可由本任务直接执行的完整热加载步骤。可继续使用经批准的有界 stdio 查询，但必须如实称为直接调用，而非已经接入常驻原生 MCP。

## 本机证据

| 检查 | 结果及含义 |
| --- | --- |
| 工程配置 | `.codex/config.toml` 的 `mcp_servers.rea` 指向 `D:\NodeJS\node.exe` 和工程 `tools/static_analysis/rea_project_server.mjs`；读取时允许三个生命周期工具，默认审批设置为 `prompt`。这是读取时的快照，后端查询工具的审批和配置由后续工作另行处理。 |
| 临时 stdio | `rea-smoke/summary.json`、`rea-project-smoke/summary.json` 均记录 `passed`、`child_exit_code: 0`、`targets_launched: false`；runner 的 finally 关闭 stdin 并等待子进程退出。没有从这两次测试留下的常驻服务器。 |
| CLI 默认查询 | 先前 `codex mcp get rea --json` 未找到 Rea。此结果不是当前桌面/Work 任务工具加载状态的充分判据。 |
| CLI 临时覆盖 | 先前 `codex -c 'mcp_servers.rea=…' mcp get rea --json` 成功解析 stdio 路径和三个工具；没有启动服务器，也没有向正在运行的任务注入工具。输出不含审批模式字段，不能单独证明其运行时执行。 |
| CLI 管理命令 | 本机 `codex mcp --help` 只有 `list/get/add/remove/login/logout/help`，没有 `reload` 或 `reconnect`。 |
| CLI 恢复命令 | 本机 `codex resume --help` 支持恢复已存会话和临时配置覆盖；它不是向当前委派任务注入工具的已验证入口。本次未执行 resume。 |
| App Server 入口 | 本机 `codex app-server --help` 有 `proxy`、`generate-ts`、`generate-json-schema` 等。`proxy --help` 说明可连接运行中的控制 socket，支持 `--sock`；未确认当前桌面/Work 宿主是否使用可由该入口连接的控制 socket，故没有连接或猜测路径。已离线生成默认及包含 experimental 项目的 schema，细节见下文。 |
| 实际工具 | 当前 `ALL_TOOLS` 的精确 Rea/三个工具名搜索为空；也没有面向当前宿主的 MCP reload/reconnect 工具。 |

CLI 帮助命令尝试创建用户目录下的临时 PATH 别名时被沙箱拒绝，随后仍退出 0；没有为此申请提权或采用绕过手段。

## 工程作用域与宿主边界

官方支持在工程 `.codex/config.toml` 配置 MCP，但仅加载受信任工程的配置。同一 Codex host 的客户端可以共享配置；“同一 host”不能仅凭同一台电脑来推定。官方也单独说明 Work Cloud 的本地执行与云端工具接入并不等价。[官方 MCP 配置文档](https://learn.chatgpt.com/docs/extend/mcp?surface=cli)

先前只针对 Rea 和适用信任记录检查了 `C:\Users\zhuzi\.codex\config.toml` 与 `E:\Codex\codex-home\config.toml`，未见 Rea 或适用于该工程/祖先路径的信任表；未据此断言整个运行中宿主的有效信任状态。shell 没有 `CODEX_HOME`，其 CLI 使用默认用户根的迹象不能证明桌面任务也使用该根。[官方 CODEX_HOME 说明](https://learn.chatgpt.com/docs/config-file/environment-variables)

新核实的官方命令页将 `codex mcp` 描述为管理 `~/.codex/config.toml` 的服务器条目。因此，默认 CLI 查询失败不能用来否定工程配置的有效性；本次未审计 0.140.0 的完整 Rust 配置加载实现，也不把该描述扩张成所有查询场景都只读用户层的保证。[官方开发者命令](https://learn.chatgpt.com/docs/developer-commands?surface=cli)

## 能证明的最小接入流程

以下是宿主具备相应入口后的流程，不表示已经执行，也不是新的授权请求。

1. 保留已有工程配置。由当前任务的实际宿主确认该工程配置已经进入有效配置层，且满足既有信任规则。若项目层被拒绝，当前限制下停在这里；不要改写全局配置或增加信任以绕过。
2. 若当前宿主已经提供经过验证、可操作当前任务的 App Server 控制连接，先核实客户端是否有只刷新 Rea 单个连接的入口，并确认它不会影响其他任务。`config/mcpServer/reload` 不能充当这一能力的替代证明：本机 0.140.0 的 `params` 只能省略或为 `null`，没有服务器或任务筛选参数，会为该宿主已加载线程排队刷新。只有确认影响范围符合用户要求后，才可考虑适用的刷新操作；否则停在这里。本轮没有执行 reload，也未验证单连接刷新入口。单独启动一个新 App Server 并不能证明刷新了原任务。
3. 通过同一宿主的 `mcpServerStatus/list` 传入实际 `threadId` 和 `detail: "toolsAndAuthOnly"`，在返回 `data` 中筛选 `name == "rea"` 并核对工具。若有 `nextCursor`，继续分页。最新官方文档提到的 `serverName` **不在本机 0.140.0 schema 中**，不能使用；官方说明带 `threadId` 会复用该线程的 MCP 连接。目前没有可用于此验证的已确认通道，没有发送请求。
4. 最终以当前任务实际工具目录出现获准 Rea 工具，并能按审批规则完成一次静态状态查询为验收。配置解析成功、服务器进程存在或另一个 CLI 的 `/mcp` 列表都不能替代这项验证。

上述 reload/status 方法及入参现已由本机 0.140.0 离线生成的 schema 核实，但当前桌面/Work 宿主的版本、实际连接和可达性仍未验证。不能用 shell 安装的 CLI 版本代替当前运行宿主版本。[官方 App Server 文档](https://learn.chatgpt.com/docs/app-server)

## 本机 0.140.0 离线 schema 校正

执行前确认目标目录不存在，随后分别执行以下命令，均退出 0。第二个输出目录也是新建，未覆盖已有文件。这两个命令只生成协议文件，没有启动或连接 App Server：

```powershell
codex app-server generate-json-schema --out '.tooling\mcp-schema'
codex app-server generate-json-schema --experimental --out '.tooling\mcp-schema\experimental'
```

生成器的帮助标题标注 `[experimental]`；`--experimental` 的作用是包含实验性方法和字段。此次核实的两个方法已经出现在**不带该开关**的默认 `ClientRequest.json` 中；其请求形状在两份输出中一致。不能因生成器名称标注 experimental，就断言这两个方法需要开启实验性 API。

| 方法/类型 | 本机准确形状 |
| --- | --- |
| `config/mcpServer/reload` | 请求必需 `id`、`method`；`params` 非必需，提供时类型为 `null`，不是空对象。没有 `threadId`、`serverName` 或工程路径参数。 |
| `McpServerRefreshResponse` | 空对象形状，不携带逐服务器连接完成结果；不能仅凭返回 `{}` 宣称 Rea 已加载。 |
| `mcpServerStatus/list` | 请求必需 `id`、`method`、`params`；`params` 是对象，其字段均可省略：`threadId`（string/null）、`cursor`（string/null）、`limit`（uint32/null）、`detail`（`full`/`toolsAndAuthOnly`/null）。 |
| `ListMcpServerStatusResponse` | 必需 `data` 数组，另有可选 `nextCursor`；服务器条目包括 `name`、`tools`、`authStatus` 等。客户端筛选 `name == "rea"`。 |
| `McpServerStatusUpdatedNotification` | `name`、`status` 必需；`threadId`、`error` 可选。状态枚举为 `starting/ready/failed/cancelled`。即使观察到 ready，仍需确认当前任务工具目录。 |

以下是供**已初始化且已确认属于当前宿主**的控制连接使用的协议形状示例，不是可粘贴执行的 shell 命令，也不是执行授权，本次未发送。第一条为宿主范围的 reload，不是单连接或单任务刷新；取得接口并不解除前述对其他任务影响的限制。请求 ID 必须在该连接中唯一；示例任务 ID 必须替换为宿主确认的实际线程 ID，不能直接假定来源聊天 ID 就是它。

```json
{"id":401,"method":"config/mcpServer/reload","params":null}
{"id":402,"method":"mcpServerStatus/list","params":{"threadId":"<actual-host-thread-id>","detail":"toolsAndAuthOnly"}}
```

本地证据：

- [默认 ClientRequest schema](../../.tooling/mcp-schema/ClientRequest.json)，SHA256 `1fd7b338ff4931cb378f3aad2053b67aac5edebd60dc6ad61cd81436ff6ebf70`。
- [默认状态查询参数](../../.tooling/mcp-schema/v2/ListMcpServerStatusParams.json)，SHA256 `701916a7d444afbbc68aef9e72ab4e5c3111a8fd97560072e9b84713adf9ddc0`。
- [包含 experimental 的状态查询参数](../../.tooling/mcp-schema/experimental/v2/ListMcpServerStatusParams.json)，与默认参数文件 SHA256 相同；其中也没有 `serverName`。
- [刷新响应](../../.tooling/mcp-schema/v2/McpServerRefreshResponse.json)，SHA256 `54a77812db02175dc69053870e582d3b314af6f161f0c76846f3563b0f9487c4`。

这些文件位于工程本地 `.tooling` 中，不依赖安装额外工具。它们证明本机 CLI 所实现的协议形状，不证明当前委派任务已经能够调用对应方法。

## 用户界面和命令的准确含义

- 官方 `/mcp`（及文档中的 `/mcp verbose`）用于查看当前会话的 MCP 工具/诊断，不是已文档化的重载命令。当前委派界面是否支持该命令需由实际客户端确认；不能直接向此聊天发送后就声称加载完成。
- `/resume` 或 `codex resume` 用于恢复会话。没有证据说明从另一个 shell 恢复会话会把 Rea 注入正在运行的 Work/Codex 委派任务，因此不将其列为当前任务热加载办法。
- 查阅的官方命令页没有 `/reload` 或 `/mcp reconnect`；本机 CLI 管理帮助也没有对应子命令，不提供这些猜测命令。
- 官方桌面配置流程写明保存服务器后选择 `Restart`，但本次没有核实当前界面的该按钮对象与作用域，也不会把它解释成已获准的应用重启操作。当前限制下不执行。[官方 MCP 界面流程](https://learn.chatgpt.com/docs/extend/mcp?surface=cli)

“持续接入”应指宿主加载并管理 MCP 会话及其工具，配置保留在工程中；无需为此安装 Windows 服务、计划任务或开机启动项。Rea 的现有 stdio wrapper 可以由合适的宿主管理，但单纯留下一个无人连接的进程不会增加当前模型可调用工具。

## 相关记录

- [此前 CLI 入口诊断](MCP-ENTRY-DIAGNOSIS.md)
- [直接 Rea smoke](rea-smoke/summary.json)
- [工程 wrapper smoke](rea-project-smoke/summary.json)
- [原生后端安装与静态查询计划](NATIVE-BACKEND-APPROVAL-PLAN.md)

本次交付为此文档。当前阻点是宿主接入能力尚未验证，不是安装失败或自动审批拒绝；没有待执行的重启、信任变更或全局注册动作。
