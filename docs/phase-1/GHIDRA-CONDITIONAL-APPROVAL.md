> 最新状态：用户已明确批准，项目内Ghidra12.1.4与JDK21已安装并通过实际headless及单函数反编译；当前结果以GHIDRA-INSTALL-RESULT.md和ghidra-smoke证据为准。下文保留早期审计状态。

# Ghidra/JDK 条件安装核查

2026-10-18。用户条件是“这是Rea推荐的反编译方式吗？是的话允许执行”。主会话要求区分推荐与支持；若仅支持，先回报再决定。

## 结论

目前能证实 Ghidra 是 Rea 支持且验证过的原生分析后端，不能证实它是官方推荐或默认优先方式。因此条件尚未满足，未下载或安装 Ghidra/JDK，未新增配置或运行headless。不是权限拒绝，状态是等待主会话与用户根据真实事实作决定。

## Rea 6.5.0 发布包证据

只读检查已核验SHA512的官方发布包：https://registry.npmjs.org/rea-agents/-/rea-agents-6.5.0.tgz 。

- README.md 的“Do I need Hopper, Ghidra or IDA?”将三者列为深度原生分析后端，说明Ghidra/IDA使用已有安装；没有优先推荐Ghidra。此前建议选择Ghidra是本任务基于无现成商业后端的工程判断，不能归称Rea官方推荐。
- native/windows/README.md 明确Ghidra与完整JDK为用户自行提供的依赖；存在Windows原生控制不等于自动安装Ghidra。
- dist/application/binary/AnalysisProviderRegistry.js 第128–166行：自动模式仅在恰好一个候选可用时选定；多个可用候选返回ambiguous，没有按Ghidra/Hopper/IDA优先级排序。
- dist/ghidra/GhidraInstallationPolicy.js 将验证构建与兼容版本线区分，并按安装的application.java.min/max验证JDK；不能把verified说成recommended。

## 官方在线资料的辅助核对

以下页面属于repository main，不能替代6.5.0包内证据：

- https://github.com/morluto/rea/blob/main/docs/installation.md#ghidra ：描述连接已有Ghidra、验证Ghidra12.1.4/JDK21及支持版本线；Windows支持为实验性P0。全文未找到recommend表述。
- https://github.com/morluto/rea/blob/main/docs/windows-ghidra-p0.md ：列出Windows只读边界、已验证版本和完整JDK要求，没有将Ghidra宣布为首选。
- https://github.com/morluto/rea/blob/main/docs/native-investigation.md ：要求从已安装provider中选择，Ghidra是给出的工作流示例。示例不等同优先推荐。

## Ghidra/JDK 版本条件

Ghidra官方12.1.4资料：https://github.com/NationalSecurityAgency/ghidra/blob/Ghidra_12.1.4_build/GhidraDocs/GettingStarted.md 。其软件前提是64位JDK21，JDK21仍是本任务拟议版本，不需要改变为其他主版本。

发布页：https://github.com/NationalSecurityAgency/ghidra/releases/tag/Ghidra_12.1.4_build ，资产ghidra_12.1.4_PUBLIC_20260921.zip，官方SHA256为ddac49f903da9d5bac833e5cc79395098b9c33cfd3279be5f31bd00387d2d4db。没有下载该资产。

若用户接受“受支持且已验证，由本任务建议采用”而明确继续授权，可按上一安装报告规划在工程.tooling下安装Ghidra12.1.4和正式来源JDK21，固定JDK补丁版本及下载hash后核验，不全局修改PATH/JAVA_HOME；再备份配置、doctor/headless自检和单个小模块/函数的只读反编译。目前未开始这些动作。

现有Rea6.5.0持久注册和207工具握手结果保持；目前只有open_binary身份识别成功，没有反编译文本。
