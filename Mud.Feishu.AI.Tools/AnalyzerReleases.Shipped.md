; Shipped analyzer releases
; https://github.com/dotnet/roslyn-analyzers/blob/main/src/Microsoft.CodeAnalysis.Analyzers/ReleaseTrackingAnalyzers.Help.md
;
; R2-04：本文件与 AnalyzerReleases.Unshipped.md 是 RS2008（"为包含规则的分析器项目启用
; 分析器发布跟踪"）的解药，同时也是诊断码的**发布账本**——新增/删除/改语义的诊断码必须在此登记，
; 否则构建期即报 RS2008/RS2002。
;
; 诊断码的**定义集必须与生产源码的上报点集完全相等**，由契约守卫
; GeneratorDiagnosticsContractGuards.Diagnostics_ShouldNotDeclareUnreportedDiagnostics 机械断言。

## Release 1.0

### New Rules

Rule ID | Category | Severity | Notes
--------|----------|----------|-------
MUDFT001 | MudFeishu.AI | Error | FeishuTool 缺少工具名
MUDFT002 | MudFeishu.AI | Error | 接口命名不符合 SDK 范式
MUDFT003 | MudFeishu.AI | Error | 工具名冲突
MUDFT004 | MudFeishu.AI | Error | 返回类型不可映射
MUDFT005 | MudFeishu.AI | Warning | XML summary 缺失
MUDFT006 | MudFeishu.AI | Warning | 参数缺 param 说明
MUDFT008 | MudFeishu.AI | Error | 上传/下载参数类型无法映射 binary
MUDFT009 | MudFeishu.AI | Warning | 输出 Schema 深度截断或循环引用（聚合单条）
MUDFT010 | MudFeishu.AI | Error | 查询参数对象展开失败
MUDFT014 | MudFeishu.AI | Error | Golden 快照 diff
MUDFT015 | MudFeishu.AI | Error | Schema 内部不一致
MUDFT016 | MudFeishu.AI | Error | 工具身份与接口令牌类型不一致
MUDFT017 | MudFeishu.AI | Error | 读写分类与 SDK 事实脱钩
MUDFT018 | MudFeishu.AI | Info | AI 能力覆盖报告（聚合单条）
MUDFT019 | MudFeishu.AI | Error | SDK 源无法解析
MUDFT020 | MudFeishu.AI | Error | 参数类型无解包映射
MUDFT021 | MudFeishu.AI | Warning | 必填参数被声明为可空
MUDFT022 | MudFeishu.AI | Error | 工具未绑定执行器方法
MUDFT023 | MudFeishu.AI | Error | 执行器绑定不成立
MUDFT024 | MudFeishu.AI | Error | 执行器方法签名不符
MUDFT025 | MudFeishu.AI | Error | 执行器构造参数无法解析
MUDFT026 | MudFeishu.Tooling | Error | 工具面生成器内部异常（R2-03：6/6 输出路径统一兜底）

; ── 已删除的诊断码（R1-WP3）──
; MUDFT007 / MUDFT011 / MUDFT012 / MUDFT013：指向仓库中**不存在机制**的僵尸定义
; （[FeishuScopes] / [FeishuToolRisk] / AOT TypeInfoPropertyName 检测 / JsonPropertyName 裁剪追踪，
; 在全仓 .cs 中仅命中他们自身的描述文本），已整体删除。
;
; 这里**不写 Removed Rules 段**：RS2007 明确指出"规则没有以前已提供的版本时不能声明 Removed"——
; 本工程尚未发布过任何版本，这四个码从未随包面世，因此它们不是"移除"而是"从未发货"。
; 账本记在注释里即可；一旦 1.0 发布，后续任何诊断码的删除都必须写 Removed Rules 段（届时 RS2007 判定成立）。
