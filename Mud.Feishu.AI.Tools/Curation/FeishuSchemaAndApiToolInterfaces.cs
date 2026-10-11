// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.AI.Tools.Curation;

// <summary>
// 运行时 schema 自省（R6 / S3：补齐官方 CLI 的 schema read 能力）。
// </summary>
// <remarks>
// <para>
// <b>为什么需要它</b>：SDK 有 1228 个方法，AI 工具面只策展了其中一部分（只读 + 写）。未策展的方法对
// 模型完全不可见——模型既不知道方法签名，也没有调用途径。<c>feishu.schema_read</c> 让模型按方法名或
// 关键字查询任意 SDK 方法的 HTTP/路由/参数/令牌/风险事实（数据源是编译期 <c>FeishuToolMethodCatalog</c>）。
// </para>
// <para>
// <b>安全边界（R-12 删除决策）</b>：本域曾同时提供万能兜底通道 <c>feishu.api_call</c>（方法名 + 参数字典
// → HTTP 动态调度），它已<b>整条删除</b>，理由有三：
// ① 与策展工具构成<b>双轨调用路径</b>（同一能力两条路径，投影/幂等/风险语义不同，长期是维护负担）；
// ② 它是整个工具面<b>风险最高的面</b>（任意已登记方法的 HTTP 调度）；
// ③ <b>零外部消费</b>（Demo 未启用，仅测试覆盖）。
// 删除后未策展能力的正确处置是<b>如实告知用户</b>（先用 <c>feishu.capability_lookup</c> 判断"是不存在
// 还是未策展"），而不是提供一条绕过策展审查的通道。`Guidance/feishu.md` 的
// 「## 安全规则」小节与 `Guidance_ToolReferences_ShouldExistInContract` 守卫共同锁定这一点。
// </para>
// </remarks>

/// <summary>
/// 工具接口：feishu.schema_read（运行时 schema 自省；无 SDK 源，只读编译期方法目录）。
/// </summary>
/// <remarks>
/// 给定方法限定名（如 <c>IFeishuTenantV1OkrPeriod.ListPeriodsAsync</c>）或关键字，
/// 返回该方法的 HTTP 方法、路由模板、路径/查询/请求体参数、令牌身份、风险分级、是否已策展。
/// 数据源是编译期 <c>FeishuToolMethodCatalog</c>，无下游调用。
/// </remarks>
[FeishuTool("feishu.schema_read",
    Description = "查询任意飞书 SDK 方法的签名事实（HTTP 方法 / 路由 / 参数 / 令牌 / 风险 / 是否已策展为工具）。当需要调用一个当前工具集未覆盖的 API 时，先用本工具查它的方法签名。本工具只读编译期目录、不调用下游，也不提供任何调用通道——查到的未策展方法无法调用，请如实告知用户。",
    RequiredScopes = ["feishu:base"])]
public interface IFeishuTenantSchemaReadTool
{
    /// <summary>按方法名或关键字查询 SDK 方法签名。</summary>
    /// <returns>JSON 文本：方法条目数组（每条含 interface/method/http/route/token_kind/module/path_params/query_params/body_type/risk/curated/curated_tool），超长截断。</returns>
    Task<string> SchemaReadAsync(
        [ToolParameter("method", "方法限定名（如 IFeishuTenantV1OkrPeriod.ListPeriodsAsync）；提供时精确匹配单个方法", Required = false)] string? method,
        [ToolParameter("keyword", "关键字（如 Okr / Calendar / attendance）；按方法名/接口名/模块大小写不敏感搜索", Required = false)] string? keyword,
        [ToolParameter("module", "模块名（如 Okr / Bitable / Calendar）；只返回该模块下的方法", Required = false)] string? module,
        [ToolParameter("limit", "最大返回条数（默认 20，上限 100）", Required = false)] int? limit,
        CancellationToken cancellationToken = default);
}
