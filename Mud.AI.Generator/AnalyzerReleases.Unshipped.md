; Unshipped analyzer release
; https://github.com/dotnet/roslyn-analyzers/blob/main/src/Microsoft.CodeAnalysis.Analyzers/ReleaseTrackingAnalyzers.Help.md
;
; 维护约定：
; 1. 新增静态 DiagnosticDescriptor 必须在此登记，否则构建报 RS2008；
; 2. 修改既有诊断的 Category / Severity 必须在对应行同步，否则构建报 RS2001；
; 3. 剖面注入的动态 {prefix}{slot} 诊断（MUDFT*）由 ToolSurfaceDiagnostics 槽位表运行时构造，
;    不是静态描述符，不进入本表。
;
; v2.3：Mud.AI.Generator 自组件仓 Mud.HttpUtils.Generator 迁入（调优期），
; SDKT 档位随竖井同批迁入本仓；本包随飞书仓版本线（3.0.x）演进。

### New Rules

Rule ID | Category | Severity | Notes
--------|----------|----------|-------
SDKT001 | Mud.AI.ToolSurface | Error | ISdkToolProfile ↔ [SdkToolProfile] 剖面契约成对守卫
SDKT002 | Mud.AI.ToolSurface | Error | 剖面 [SdkToolProfile] 缺少引擎必需槽位（该剖面被跳过，工具面静默消失）
