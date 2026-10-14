// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.AI.Abstractions.Tools;

/// <summary>
/// 标注一个工具接口为 LLM FunctionCall 工具（产品族中立声明契约）。
/// </summary>
/// <remarks>
/// <para>
/// v2.3 中立化迁入（原 <c>Mud.Feishu.AI.AgentTools.FeishuToolAttribute</c>）：声明契约必须对
/// <b>任何声明工具的程序集</b>可见——含非 owner 程序集（如飞书仓 <c>Tests/Mud.Feishu.AI.Tests</c>
/// 样例工程，它自带剖面、在同名 <c>Generated</c> 命名空间生成自己的产物，不能引用 owner 程序集——
/// 引用即 CS0433 同名类型歧义）。落位 <c>Mud.AI.Abstractions</c> 后企微/钉钉等生态共用同一份形状。
/// </para>
/// <para>
/// 接口只声明 Schema（工具名、描述、模型可见参数），不复用 HTTP 路由特性
/// （语义分属「工具参数」与「HTTP 绑定」两层）；参数到强类型请求的映射由产品侧绑定类型在执行链内完成。
/// </para>
/// <para>
/// 工具默认「进注册表不启用」，宿主须显式 MapTool（白名单语义）。
/// </para>
/// </remarks>
[AttributeUsage(AttributeTargets.Interface, AllowMultiple = false, Inherited = false)]
public sealed class AgentToolAttribute(string name) : Attribute
{
    /// <summary>工具名（模型可见契约）。</summary>
    public string Name { get; } = name;

    /// <summary>模型侧描述（Skills 友好风格：短参数名 + 调用样例 + 权限一句话）。</summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// 所需权限点清单：随 Schema 输出，供产品侧授权门禁钩子与运维审计消费；SDK 不内建 scope 校验。
    /// </summary>
    public string[] RequiredScopes { get; set; } = [];

    /// <summary>
    /// 是否写操作：写工具在强制授权模式下必须过授权门禁。
    /// </summary>
    public bool IsWrite { get; set; }

    /// <summary>
    /// 该工具落地的 SDK 能力来源，形如 <c>"IFeishuTenantV3User.GetBatchUsersAsync"</c>（可空）。
    /// </summary>
    /// <remarks>
    /// <para>
    /// 声明后，源生成器在<b>编译期</b>用 SDK 符号交叉校验并派生（单一真相源 = SDK，非本特性）：
    /// </para>
    /// <list type="bullet">
    /// <item>HTTP 方法与路由（<c>x-feishu.source</c>，审计与排障用）；</item>
    /// <item>风险分级（危险词命中 → <c>high-risk-write</c>，覆盖 <see cref="IsWrite"/> 的粗分级）；</item>
    /// <item>返回类型可映射性与上传/下载参数可映射性。</item>
    /// </list>
    /// <para>
    /// 无法解析（类型或方法名写错）时报工具名范式诊断并使构建失败——这正是「工具面与 SDK 不脱钩」的机械保证。
    /// </para>
    /// </remarks>
    public string? Source { get; set; }

    /// <summary>
    /// <b>条件必填组</b>：每项形如 <c>"a|b|c"</c>，表示<b>该组内至少要提供一个</b>。
    /// </summary>
    /// <remarks>
    /// <b>为什么需要它</b>：一批工具存在<b>跨参数</b>必填约束（<c>user_id</c>/<c>room_id</c> 二选一、
    /// <c>emails</c>/<c>mobiles</c> 至少一个、<c>summary</c>/<c>description</c>/<c>due</c> 至少一个），
    /// <c>Required = true</c> 表达不了（那会让整组都变必填），只写在描述里靠模型"读懂"属<b>弱约束</b>，
    /// 必须结构化进 Schema。
    /// <para>
    /// <b>产出与校验</b>：源生成器渲染为标准 JSON Schema 关键字 <c>"anyOf": [ {"required":["a"]}, … ]</c>
    /// （同时保留描述文本：结构化给校验器，文本给推理）；组内参数名必须存在于同一工具签名，
    /// 否则报条件必填组诊断（构建期 Error）——避免"组里写错参数名 ⇒ 约束静默失效"。
    /// </para>
    /// <para>
    /// <b>运行时兜底不变</b>：执行器的 <c>ArgumentException</c> 校验保留——Schema 是给模型的提示，
    /// 不是安全边界；不支持 Schema 的宿主仍需正确报错。
    /// </para>
    /// <code>
    /// [AgentTool("calendar.find_free_slots", Source = "IFeishuTenantV4Calendar.GetFreebusyCalendarAsync",
    ///     AnyOf = ["user_id|room_id"])]
    /// </code>
    /// </remarks>
    public string[] AnyOf { get; set; } = [];
}
