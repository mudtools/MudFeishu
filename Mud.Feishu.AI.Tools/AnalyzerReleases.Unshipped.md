; Unshipped analyzer release
; https://github.com/dotnet/roslyn-analyzers/blob/main/src/Microsoft.CodeAnalysis.Analyzers/ReleaseTrackingAnalyzers.Help.md
;
; 本工程以分析器形态分发（不作为运行时包），此处登记"下个发布周期"新增/变更的诊断码；
; 一旦发布即移动到 AnalyzerReleases.Shipped.md。1.0 之前全部诊断码已在 Shipped.md 的
; Release 1.0 段落一次登记（见 R2-04），此处只登记其后新增的码。

### New Rules

Rule ID | Category | Severity | Notes
--------|----------|----------|-----------------------------------------------------
MUDFT027 | MudFeishu.AI | Error | 工具名派生常量名冲突（R4-10：字面不同的工具名归一为同一常量名）
MUDFT011 | MudFeishu.AI | Error | AnyOf 条件必填组引用不存在的参数 / 混入 Required 参数 / 单元素组（R5-B6：否则 anyOf 约束空气、静默失效）

