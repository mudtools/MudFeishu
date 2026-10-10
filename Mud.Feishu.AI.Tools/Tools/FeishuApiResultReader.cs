// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.AI.Tools;

// R-6（模块边界归位）：本文件现在**只**负责一件事——把生成的强类型接口返回
// （FeishuApiResult<T>）解包为 FeishuApiOutcome<T>。此前同文件里还住着
// ToolArgs（入参读取，已拆到 ToolArgs.cs）与 ToolResultText（出站截断，已拆到 ToolResultText.cs），
// 三者职责互不相干却同处一个以"结果解包"命名的文件里。

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
/// <see cref="FeishuApiResult{T}"/> 统一解包器（分域执行器共用；<c>code != 0</c> → 可读错误文本，
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
