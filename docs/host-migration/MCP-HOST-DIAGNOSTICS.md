# 当前宿主的 Rea MCP 只读诊断入口

核实日期：2026-10-19。用户已报告新建同工程任务仍没有 Rea，因此本轮不重复创建任务、恢复会话或重启。本轮只查阅本机 `codex-cli 0.140.0` 的帮助、工程内已有离线协议 schema、当前可用工具元数据及官方文档；仅新增此文档。

没有读取私人会话、历史、凭据、`auth.json` 或任何运行日志；没有扫描用户目录，没有连接控制 socket、启动 App Server、修改配置或信任。

## 结论与仍不可达的原因

已验证最有用的协议入口是 `config/read`、`configRequirements/read`、`mcpServerStatus/list` 及配置/MCP 启动通知。它们可以分别回答配置来源、受管要求和实际连接状态；其中前两个不能代替第三个。

当前任务没有暴露这些宿主诊断调用工具，也没有 Rea 工具。现有 `app-server proxy` 命令虽支持控制 socket，但尚未确认当前 Codex/Work 委派任务对应的运行宿主、宿主版本、控制连接或线程 ID 映射。因此本轮停在准确的接口形状和边界，未向猜测的连接发送请求。没有任何诊断结果足以认定 Rea 因某个信任表、C/E 配置根或受管策略而被跳过。

官方说明桌面内置 Codex 与 shell CLI 可以不同版本；本机离线 schema 只证明生成它的 `0.140.0` CLI 支持这些形状。另起 CLI/App Server 的配置读取即使成功，也不能冒充当前宿主的有效配置或现有线程的 MCP 连接。[官方排障文档](https://learn.chatgpt.com/docs/reference/troubleshooting)

## 本机 schema 已验证的只读协议

以下示例仅说明请求格式，**本轮未发送**。只有实际宿主连接经过确认并已初始化，且接口返回内容可按本任务范围收窄后，才适合用于后续诊断。示例 ID 需在该连接内唯一；实际线程 ID 必须由该宿主确认，不能将来源聊天 ID 自动视为宿主线程 ID。

### 1. `config/read`：工程相关的磁盘配置层

```json
{"id":501,"method":"config/read","params":{"cwd":"C:\\Users\\zhuzi\\Documents\\Grim-Dynasty","includeLayers":true}}
```

- `params` 是必需的对象；本机支持可选 `cwd`（string/null）和 `includeLayers`（boolean）。`cwd` 用于解析工程至仓库根之间的工程配置层。
- 响应必需 `config`、`origins`；可选 `layers`。`origins` 给出配置键的来源元数据；每个 layer 包括 `name`、`version`、`config`，以及可选 `disabledReason`。
- 来源类型可以是 `user`（含配置文件路径及可选 profile）、`project`（`dotCodexFolder`）、`sessionFlags`、系统/企业受管层等。它比猜测 shell 的 `CODEX_HOME` 更适合确定**被查询宿主**实际采用了哪些层。
- 若取得结果，最小报告应只保留工程层路径/类型/版本/`disabledReason`、`mcp_servers.rea` 是否出现在有效配置及其相关来源；需要时只核对 Rea 的启动路径、开关和工具允许列表。
- 该接口没有按配置键过滤的参数，完整 `config`/`layers[*].config` 可含无关设置或任意环境变量。后续应由宿主的定向诊断展示，或经获准的本地适配器过滤后再输出所需字段，不能直接打印整份响应。

官方将该接口定义为磁盘配置经过层级解析后的有效配置。它不能单独证明一个已经运行的线程刷新了 MCP 连接。[官方 App Server 文档](https://learn.chatgpt.com/docs/app-server)

本地 schema：[参数](../../.tooling/mcp-schema/v2/ConfigReadParams.json)、[响应与配置层定义](../../.tooling/mcp-schema/v2/ConfigReadResponse.json)。

### 2. `configRequirements/read`：已公开的受管要求

```json
{"id":502,"method":"configRequirements/read","params":null}
```

- 准确方法名是 `configRequirements/read`，不是 `configurationRequirements`。`params` 只能省略或为 `null`。
- 响应 `requirements` 可为 `null`；schema 说明这表示没有配置该接口所读取的 requirements，例如对应文件/MDM 条目。
- 本机默认 schema 公开审批策略、沙箱模式、权限 profile、功能要求、Windows 沙箱实现等字段。包含 experimental 的输出另外公开 `allowedApprovalsReviewers`、`hooks`、`network` 等；**两份均没有专门的 `mcp_servers` 要求字段**。
- 因而该接口可补充受管限制信息，但不能保证能直接解释 Rea 白名单/禁用原因；缺少 MCP 字段或返回 `null` 不足以排除其他宿主策略或工具接入层的限制。不得为了获取更多字段自行开启实验性功能。

本地 schema：[响应](../../.tooling/mcp-schema/v2/ConfigRequirementsReadResponse.json)、[包含 experimental 的响应](../../.tooling/mcp-schema/experimental/v2/ConfigRequirementsReadResponse.json)。`config/read` 和 `configRequirements/read` 两个方法都已经包含在默认请求 schema；默认与 experimental 的方法请求形状一致。

### 3. 现有线程的 `mcpServerStatus/list`

```json
{"id":503,"method":"mcpServerStatus/list","params":{"threadId":"<actual-host-thread-id>","detail":"toolsAndAuthOnly"}}
```

本机 0.140.0 支持 `threadId`、`detail`、`cursor`、`limit`；不支持最新官方文档提到的 `serverName`。在返回 `data` 中筛选 `name == "rea"`，必要时按 `nextCursor` 分页。官方说明指定 `threadId` 时复用该线程的 MCP 连接；这比另起 CLI 的 `mcp get` 更接近实际加载状态。不要省略任务标识后把应用级结果称为当前任务结果。

这里只检查目录与连接信息，不调用 Rea，不做 OAuth 或重载。尚未审计该宿主实现是否会在状态查询时启动任何未初始化连接；后续应先确认这一点，避免把有潜在连接初始化效果的调用一概称为零副作用。无论服务器列表结果如何，仍需检查当前任务的实际工具目录。

本地 schema：[查询参数](../../.tooling/mcp-schema/v2/ListMcpServerStatusParams.json)、[响应](../../.tooling/mcp-schema/v2/ListMcpServerStatusResponse.json)。

### 4. 配置与 MCP 启动通知

| 本机准确通知名称 | 可用诊断字段 | 最小用途 |
| --- | --- | --- |
| `configWarning` | `summary` 必需；`details`、`path`、`range` 可选 | 保留与本工程 `.codex/config.toml` 或 Rea 有关的禁用/解析警告及文件位置。 |
| `mcpServer/startupStatus/updated` | `name`、`status` 必需；`threadId`、`error` 可选 | 筛选 `name == "rea"` 和实际任务标识，区分 `starting/ready/failed/cancelled`。 |

这是宿主向已连接客户端发送的通知，不是按名称调用的查询方法；不能假设后接入就能回放既往事件。最新官方文档还描述启动失败的 `failureReason`，但本机默认通知 schema 没有这个字段，不能依赖它。错误文本也只保留 Rea 启动所需的最小片段，不导出其他任务的通知。

本地 schema：[通知目录](../../.tooling/mcp-schema/ServerNotification.json)、[配置警告](../../.tooling/mcp-schema/v2/ConfigWarningNotification.json)、[MCP 启动状态](../../.tooling/mcp-schema/v2/McpServerStatusUpdatedNotification.json)。

## 本机 CLI 支持的入口及本轮边界

本轮只运行表中相关 `--help`，没有运行实际诊断子命令。

| 入口 | 已验证含义 | 为什么没有执行诊断 |
| --- | --- | --- |
| `codex doctor --json` | 帮助称提供脱敏的机器可读报告；检查安装、配置、auth、运行健康。 | 没有 `config-only` 或排除 auth 的选项。用户禁止读取凭据；不能把输出脱敏等同于过程不读取 auth。且它也未证明指向当前宿主。 |
| `codex app-server daemon version` | 帮助称输出本地 CLI 与运行中 App Server 的版本。 | 尚未确认它查询的管理实例对应当前宿主；可能访问控制连接，本轮禁止连接未确认 socket。只读了帮助。 |
| `codex app-server proxy --sock ...` | 已有帮助确认可连接运行中的 App Server 控制 socket。 | 没有已确认的当前宿主 socket，不猜测路径、不扫描用户目录、不连接。 |
| `codex debug app-server` | 本机唯一实际子命令是 `send-message-v2`。 | 不是已经验证的配置只读查询；不发送消息、不启动诊断会话。 |
| `codex mcp get/list` | 可查看 CLI 配置的服务器条目。 | 先前临时覆盖已证明解析入口；重复运行不能回答当前桌面/Work 宿主已加载什么。 |

帮助命令仍有临时 PATH 别名创建被沙箱拒绝的提示，但正常退出 0；未提权或尝试绕过。当前已暴露工具中没有宿主配置读取、MCP 状态读取或日志诊断工具；没有借用会话读取工具查看私人历史。

## 日志：只确定官方入口，不读取内容

官方配置参考将 `log_dir` 默认位置列为 `$CODEX_HOME/log`；环境变量文档说明明文 `codex-tui.log` 需要显式配置 `log_dir` 才启用，并说明 `RUST_LOG` 可调整诊断级别。这些是位置/开关约定，**不是本次已经确认存在的日志文件**，也不表明该目录属于当前桌面/Work 宿主。[配置参考](https://learn.chatgpt.com/docs/config-file/config-reference) [环境变量文档](https://learn.chatgpt.com/docs/config-file/environment-variables)

已查官方 Windows 文档没有提供可用于本任务的固定应用日志路径。本轮不套用其他系统路径，不读取 SQLite 状态库、会话存储、整份日志或目录清单，也不设置 `RUST_LOG`/`log_dir` 产生新的诊断数据。

后续若确需日志，先由实际宿主的官方诊断界面/已确认入口提供日志身份、范围和过滤能力，仅提取本工程 Rea 的配置加载或启动错误。未确认这些条件前，不以 `Get-Content`、搜索整份日志或导出诊断包代替定向检查。

## 本轮可交付与未完成项

已完成：实际 0.140.0 接口形状核验、CLI 支持入口核验、默认与实验性字段边界说明，以及本文件。既有 schema 未改写；旧诊断证据未改写。

尚未获得：当前宿主的有效配置层、Rea 禁用/启动错误或当前宿主连接结果。确切缺口是缺少已确认且可定向查询的当前宿主诊断通道；不是某次查询被拒绝，也不是自动审批失败。不会以新建任务、重启、修改信任或宿主级 reload 继续试错；全宿主 reload 无服务器/线程筛选且可能影响其他任务，仍不执行。

相关记录：[MCP 持续接入与协议形状](MCP-PERSISTENCE.md)、[此前 CLI 入口诊断](MCP-ENTRY-DIAGNOSIS.md)。
