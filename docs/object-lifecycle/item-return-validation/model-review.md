# Item 返回模型独立静审

本次审阅只读工程中保存的指令、输入绑定及源码，未读取原版、运行reader、编译或测试，未修改产品源码。结论是：下述固定版本在其明确声明的条件域内，未发现阻断问题。这个结论不是运行测试通过，也不是完整Item/QuestItem原生兼容验证。

## 固定版本与依据

| 文件 | SHA256 |
| --- | --- |
| src/task_items/ItemConstructorReturn.cs（18784B） | 2a23081b4c995b4bc73c46827d6b8ef3a0b82ffd5cd994f8f1376472a1936feb |
| docs/object-lifecycle/offline-item-return-dependency/inspection.json | fc0f85739966bf84241cc277b6c4d2ae7129a9daa27792ed1245e9cb7b409ff0 |
| docs/object-lifecycle/offline-item-return-dependency/324560-instructions.json | 1998091f5ea21ab1f993401c4c83fa793760f286621a7b7d9d21d6abd8d854ef |
| docs/object-lifecycle/offline-item-return-inputs/inspection.json | 288a5ae6cc72a4a2e68b7c1f03f14476a2f1e5a1a9794fe2f9bdb710a09cba4f |
| docs/rewards/rea-inventory-add/Item-constructor-primary-table-assembly.json | 7a078d11495b5b7c10a07d7b6309c9d633f8ec39dfc444e5f0decfbd5efe1a3f |
| docs/object-lifecycle/item-return-validation/cached-global-contract.md（静态证据已冻结） | 52a4d181902a881e3baae0e1f33a3748833d8d54b8696fedc60e37bf27efdb37 |

本体依据为Game324560..3245DB的精确123B/28指令。rand身份及7A8164文件值已由固定输入报告绑定；历史GameEngine正文只在证据文档列明的四处34B锚点获得本轮raw核验。未把历史整个构造体、数据库加载或全局发布后的状态升级成已恢复行为。

## 原生顺序与部分状态

- `Begin`不在Invocation构造时采样；它验证先前Name阶段确实到323A49，随后只借用一次slot指针与该身份的own字段视图。后续slot重新发布不会改写借用对象。null指针、未知指针、缺backing、身份不匹配和失效视图分别停止，不从文件初值制造运行时对象。
- `CopyFirstField`在323A50读取借用对象+1B58的DWORD并在323A56写Item+B68；`CopySecondField`随后读取同一对象+1B5C并写B6C。第二读取失败保留第一写入。观察模型保留12B原始视图，是因为已证构造曾以QWORD写+1B5C；Item本次两次读取仍严格是4B，没有读取第三DWORD来替代未知值。
- 32456A的B6C值与324588形成的EDI在外部调用前保存。EDI=0时先重读scale、计算并写B64，但仍进入3245A2的调用观察，之后照常到IDIV。源码没有添加原生不存在的零跨度成功早退。
- 外部正常返回EAX由显式观察提供，不执行rand。3245A8重新读取B68、3245B1重新读取scale；未重新读取B6C或全局slot。修改这些own输入的效果与固定寄存器值分开保存。
- IDIV发生在后一次lower乘积之后。除数0或INT_MIN/-1停止在3245B9，保留先前字段复制、可能已发生的早期B64写入及返回EAX观察；未将故障路径包装成成功或回滚。正常路径使用有符号余数，经转换、加法和截断后才在3245CA写最终B64。
- 依赖正常返回后，原caller323A71选择原Item RDI；模型返回同一state.Token，而非将callee的未证RAX内容作为Item身份。`ConditionalNormalReturn`只表示这些显式前提下到323A86；`IsCompleteItemConstructor`始终为false。

## 覆盖域与自有限制

该模型使用独立own字节存储，不能把同一地址静默建成互不相关的两份内存。新增`BorrowedFieldAliasingOutsideCoverage`检查整个GameEngine12B观察区间与Item捕获区间的重叠；两类构造器的token范围使端点加法不溢出。该检查是覆盖域限制，不是原生失败分支。分阶段API要求各阶段内排他访问，亦不宣称原生线程原子性、生命周期或异常恢复。

数字子域明确要求整个Invocation有正常masked、round-to-nearest/even SSE环境的显式观察；不设置MXCSR或模拟状态标志。内部整数dyadic运算只接受结果能精确表示为有限normal binary32或零的情况：需要舍入、subnormal、非有限、溢出及无效转换均停在对应指令，不用double近似冒充已知。CVTT按向零截断，有符号IDIV两种明确fault在执行托管余数前检查。这里只核对声明域及原生顺序；数字位宽/极值的另一份独立审查由主代理协调，未运行任何数值程序。

`WithArchivedScale`是显式选择文件位447A0000的工厂，不自动启用环境条件，也不承诺运行时scale恒定。rand的模块/名字已绑定，但序列、seed、线程状态和真实返回值未恢复；任意有符号EAX fixture不等于rand实际可产生该值。

构造器默认字段40400000/0000000040A00000不作为自动全局输入：后续2C8750及Initialize仍可能覆盖它们。模型保留需要实时own观察的边界，没有用默认常量、有效空对象或占位callback完成整个GameEngine。

## 尚未验证

本审阅没有执行新测试或旧suite。应由主代理在审批后的精确构建中验证新fixture，并把实际测试计数单独记录。原生SSE环境、rand运行时状态、异常/EH、GameEngine数据库加载后的真实字段、官方对象工厂与完整QuestItem读取均不由本局部静审闭合。
