// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.DataModels;

namespace Mud.Feishu.AI.Tools;

// R-9 / 阶段 5.0（下沉批）：本类型从 `Mud.Feishu.AI.Tools` 下沉到本程序集（保持 internal + IVT 回 AI.Tools）。
//
// 为什么必须下沉（R1.3 评审发现的**阻塞项**）：`Channels/` 与 `Events/` 的实现（4 + 8 个文件）都要解包
// 强类型接口返回，而它们是 R-9 要迁到**本程序集**的。若不先下沉，迁移后本程序集需要引用 `Mud.Feishu.AI.Tools`
// ——与新增不变量「AI 不得引用 AI.Tools（防环，保持"工具面依赖集成面"单向）」直接矛盾，编译必挂。
//
// 为什么用 internal + IVT 而不是 public：公开化会反向抵消 R-7 的公开面收敛目标
// （下沉是"归属归位"，不该顺带扩大任何一边的公开承诺）。
//
// 为什么**不**顺带下沉 ToolArgs / ToolResultText：它们是工具**入参**与**出站预算**语义，
// 与事件面无关；下沉会把工具面细节灌进集成面。

/// <summary>
/// <c>FeishuApiResult</c> 解包结果（Phase 1 §3.3.3 通用规则 3：统一解包，错误转可读文本回填模型）。
/// </summary>
/// <typeparam name="T">业务载荷类型。</typeparam>
/// <param name="Ok">是否成功（<c>code == 0</c> 且载荷非空）。</param>
/// <param name="Data">业务载荷（成功时非空）。</param>
/// <param name="ErrorText">失败原因（可读文本，回填模型）。</param>
/// <param name="Code">飞书业务 code（错误分类消费；无 code 场景为 null）。</param>
internal sealed record FeishuApiOutcome<T>(bool Ok, T? Data, string? ErrorText, int? Code = null) where T : class
{
    /// <summary>构造成功结果。</summary>
    public static FeishuApiOutcome<T> Success(T data) => new(true, data, null);

    /// <summary>构造失败结果。</summary>
    /// <param name="errorText">可读错误文本（回填模型）。</param>
    /// <param name="code">飞书业务 code（P1D-2b 错误分类消费；无 code 场景为 null）。</param>
    public static FeishuApiOutcome<T> Fail(string errorText, int? code = null) => new(false, null, errorText, code);
}

/// <summary>
/// <see cref="FeishuApiResult{T}"/> 统一解包器（分域执行器与集成面共用；<c>code != 0</c> → 可读错误文本，
/// 不吞错误、不抛裸异常）。
/// </summary>
internal static class FeishuApiResultReader
{
    /// <summary>解包标准结果。</summary>
    /// <typeparam name="T">业务载荷类型。</typeparam>
    /// <param name="result">源生成客户端返回结果（可能为 null——网络层已吞异常的情况）。</param>
    /// <returns>解包结果。</returns>
    public static FeishuApiOutcome<T> Read<T>(FeishuApiResult<T>? result) where T : class
    {
        if (result is null)
        {
            return FeishuApiOutcome<T>.Fail("飞书接口无响应（result 为空）");
        }

        if (result.Code != 0)
        {
            return FeishuApiOutcome<T>.Fail(
                $"飞书接口返回错误 code={result.Code.ToString(CultureInfo.InvariantCulture)}, msg={result.Msg ?? "(无错误信息)"}",
                result.Code);
        }

        if (result.Data is null)
        {
            return FeishuApiOutcome<T>.Fail("飞书接口返回空数据");
        }

        return FeishuApiOutcome<T>.Success(result.Data);
    }
}
