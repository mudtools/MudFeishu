// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Mud.Feishu.AI.Tools.Tools;

/// <summary>
/// R5 / S-10：<b>工具结果的统一 JSON 序列化器</b>。
/// </summary>
/// <remarks>
/// <para>
/// <b>为什么需要它</b>：<c>JsonNode.ToJsonString()</c> 走 <c>JavaScriptEncoder.Default</c>，
/// 会把<b>所有非 ASCII 字符</b>（含全部中文）转义成 <c>\uXXXX</c>。于是工具结果里的
/// 中文提示（如"内容未丢失，请调用 docx.delete_blocks"、错误建议、避坑说明）
/// 模型读到的是转义串 —— <b>token 膨胀约 6 倍，且显著增加误读风险</b>。
/// 这直接削弱 <b>F-8（错误可恢复性）</b>：错误文案越难读，模型自我纠正越不可靠。
/// </para>
/// <para>
/// <b>为什么用 UnsafeRelaxedJsonEscaping 而不是自定义编码器</b>：
/// 它只放宽"非 ASCII + HTML 敏感字符"的转义，产出的仍是<b>合法 JSON</b>
/// （字符串内的引号、反斜杠、控制字符仍按规范转义）。
/// </para>
/// <para>
/// <b>适用范围（刻意收窄）</b>：仅用于<b>模型读取的工具结果</b>。
/// <b>发送给飞书的 message content 不改动</b> —— 那属于对外协议面，
/// 未经真实平台验证前不擅自改变其编码形态（风险不对称：工具结果改错的代价是文案变丑，
/// 对外报文改错的代价是消息发送失败）。
/// </para>
/// </remarks>
internal static class ToolResultJson
{
    private static readonly JsonSerializerOptions Options = new()
    {
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        WriteIndented = false,
    };

    /// <summary>把 JSON 节点序列化为<b>保留中文原文</b>的紧凑 JSON 文本。</summary>
    public static string ToText(JsonNode node) => node.ToJsonString(Options);
}
