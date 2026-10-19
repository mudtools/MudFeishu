// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using System.Globalization;
using Mud.AI.Agent.Tools;
using Mud.Feishu.DataModels;

namespace Mud.Feishu.AI.AgentTools;

// 本解包器与 `Channels/` / `Events/` 的强类型解包调用方同在本程序集（internal + IVT 回
// `Mud.Feishu.AI.Tools`）：若放在 `Mud.Feishu.AI.Tools`，本程序集须引用它，与
// 「AI 不得引用 AI.Tools（防环，保持"工具面依赖集成面"单向）」的依赖方向矛盾。
// 保持 internal 而非 public：归属归位不扩大任何一边的公开承诺。
//
// `ToolApiOutcome<T>` 位于中立包 `Mud.AI.Agent.Tools`（骨架 `ToolExecutor` 与集成面共用同一
// 解包产物形状，public）；本文件只保留**读产品信封**的读取器——信封形态（`FeishuApiResult<T>`）
// 产品间差异大，留适配层（YAGNI）。

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
    public static ToolApiOutcome<T> Read<T>(FeishuApiResult<T>? result) where T : class
    {
        if (result is null)
        {
            return ToolApiOutcome<T>.Fail("飞书接口无响应（result 为空）");
        }

        if (result.Code != 0)
        {
            // 业务码的语义由产品侧读出（独立产品线纪律）：中立层不认识飞书码表，
            // 只消费 ToolApiFailure（分类段位 + 码级候选）。
            return ToolApiOutcome<T>.Fail(
                $"飞书接口返回错误 code={result.Code.ToString(CultureInfo.InvariantCulture)}, msg={result.Msg ?? "(无错误信息)"}",
                result.Code,
                FeishuApiErrorVocabulary.Classify(result.Code));
        }

        if (result.Data is null)
        {
            return ToolApiOutcome<T>.Fail("飞书接口返回空数据");
        }

        return ToolApiOutcome<T>.Success(result.Data);
    }
}
