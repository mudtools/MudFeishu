// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.AI.Agent.Tools;

/// <summary>
/// API 结果解包产物（产品族中立）：强类型接口返回 → 统一解包结果，
/// 错误转可读文本回填模型（执行链通用规则 3）。
/// </summary>
/// <remarks>
/// v2.3 中立化迁入（原 <c>Mud.Feishu.AI.AgentTools.FeishuApiOutcome&lt;T&gt;</c>，internal → public：
/// 飞书侧 <c>FeishuApiResultReader</c>（读 <c>FeishuApiResult&lt;T&gt;</c> 产品信封）与本骨架共用同一产物形状）。
/// 信封读取器本身产品间差异大，留各生态适配层（YAGNI 不进共享层）。
/// <para>
/// <b>业务码语义的归属（独立产品线纪律）</b>：本类型<b>只携带</b>产品业务码（<see cref="Code"/>，进载荷的
/// <c>api_code</c>）与产品侧的读数（<see cref="Failure"/>）；<b>码表在产品的读取器里</b>，
/// 中立层不再认识任何产品码段位。
/// </para>
/// </remarks>
/// <typeparam name="T">业务载荷类型。</typeparam>
/// <param name="Ok">是否成功（<c>code == 0</c> 且载荷非空）。</param>
/// <param name="Data">业务载荷（成功时非空）。</param>
/// <param name="ErrorText">失败原因（可读文本，回填模型）。</param>
/// <param name="Code">产品业务 code（进载荷 <c>api_code</c>；无 code 场景为 null）。</param>
/// <param name="Failure">
/// 产品侧对业务码的读数（分类段位 + 码级候选）；产品不认识该码时为 <see langword="null"/>
/// ⇒ 中立层回落 <c>api/upstream_error</c>。
/// </param>
public sealed record ToolApiOutcome<T>(
    bool Ok,
    T? Data,
    string? ErrorText,
    int? Code = null,
    ToolApiFailure? Failure = null) where T : class
{
    /// <summary>构造成功结果。</summary>
    public static ToolApiOutcome<T> Success(T data) => new(true, data, null);

    /// <summary>构造失败结果。</summary>
    /// <param name="errorText">可读错误文本（回填模型）。</param>
    /// <param name="code">产品业务 code（进载荷 <c>api_code</c>）。</param>
    /// <param name="failure">
    /// 产品侧对业务码的读数（分类段位 + 码级候选）——由产品的信封读取器给出；
    /// 无法分类时传 <see langword="null"/>（中立层回落 <c>api/upstream_error</c>）。
    /// </param>
    public static ToolApiOutcome<T> Fail(string errorText, int? code = null, ToolApiFailure? failure = null)
        => new(false, null, errorText, code, failure);
}
