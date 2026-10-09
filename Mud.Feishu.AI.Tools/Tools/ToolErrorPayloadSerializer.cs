// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！
//  任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using System.Globalization;
using System.Text;
using System.Text.Json;

namespace Mud.Feishu.AI.Tools.Tools;

/// <summary>
/// B2 错误契约：将 <see cref="ToolError"/> 序列化为紧凑 JSON 首行（≤ 512 字节）。
/// </summary>
/// <remarks>
/// <para>
/// 手工拼接 JSON（不使用 <c>JsonSerializer</c> 反射重载——AOT 安全，对齐 IL2026/IL3050 = 0 纪律）。
/// 载荷字段为闭集枚举 + 数字 + 短字符串，无凭据面，出站净化不作用于载荷本身。
/// </para>
/// <para>
/// <b>截断保护</b>：载荷 ≤ 512 字节且置于首行，正文截断不得吃掉载荷（守卫断言）。
/// </para>
/// </remarks>
internal static class ToolErrorPayloadSerializer
{
    /// <summary>载荷最大字节上界（守卫断言基线）。</summary>
    public const int MaxPayloadBytes = 512;

    /// <summary>
    /// 将 <see cref="ToolError"/> 序列化为紧凑 JSON 行（无换行符）。
    /// </summary>
    public static string Serialize(ToolError error)
    {
        // 手工拼接 JSON：字段闭集、值短，不需要转义（category/subtype/tool/trace 均为 ASCII 闭集或白名单化的短串）。
        var sb = new StringBuilder(256);
        sb.Append('{');
        sb.Append("\"tool_error\":{");
        sb.Append("\"category\":\"").Append(error.Category).Append('"');
        sb.Append(",\"subtype\":\"").Append(error.Subtype).Append('"');
        sb.Append(",\"retryable\":").Append(error.Retryable ? "true" : "false");

        if (error.RetryAfterSeconds.HasValue)
        {
            sb.Append(",\"retry_after_seconds\":").Append(error.RetryAfterSeconds.Value.ToString(CultureInfo.InvariantCulture));
        }

        if (error.ApiCode.HasValue)
        {
            sb.Append(",\"api_code\":").Append(error.ApiCode.Value.ToString(CultureInfo.InvariantCulture));
        }

        sb.Append(",\"attempts\":").Append(error.Attempts.ToString(CultureInfo.InvariantCulture));

        if (!string.IsNullOrEmpty(error.Trace))
        {
            sb.Append(",\"trace\":\"").Append(error.Trace).Append('"');
        }

        if (!string.IsNullOrEmpty(error.Tool))
        {
            sb.Append(",\"tool\":\"").Append(error.Tool).Append('"');
        }

        sb.Append("}}");

        var result = sb.ToString();

        // 安全网：若载荷超过上界（理论上不应发生——字段都是短值），截断 trace 后重试。
        if (result.Length > MaxPayloadBytes)
        {
            // trace 是唯一可能超长的字段（来自 activity.Id），去掉它重试。
            error = error with { Trace = null };
            return Serialize(error);
        }

        return result;
    }
}
