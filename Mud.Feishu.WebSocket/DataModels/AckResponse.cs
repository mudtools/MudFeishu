// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Mud.Feishu.WebSocket.DataModels;

/// <summary>
/// ACK 响应 DTO，用于替代匿名类型序列化（P1-5 修复 / WS-06）。
/// </summary>
/// <remarks>
/// 飞书 WebSocket 协议要求 ACK 格式为：{"code": 200/500, "headers": {}, "data": "base64-encoded"}。
/// 此前使用匿名类型 <c>new { code, headers, data }</c> 进行序列化，
/// 在 Native AOT/Trimming 下存在反射失败风险，且 <c>headers</c> 为 <c>null</c> 时
/// 被 <c>DefaultIgnoreCondition = WhenWritingNull</c> 静默丢弃，
/// 导致 ACK 与飞书协议不一致。
/// </remarks>
public class AckResponse
{
    /// <summary>
    /// 业务处理结果码：200 = 成功，500 = 失败（服务端将重发）。
    /// </summary>
    [JsonPropertyName("code")]
    public int Code { get; set; }

    /// <summary>
    /// 响应头。使用空字典而非 null，保证序列化后始终包含 <c>"headers": {}</c> 字段。
    /// </summary>
    [JsonPropertyName("headers")]
    public Dictionary<string, string> Headers { get; set; } = new();

    /// <summary>
    /// 响应数据（Base64 编码的字节数组，当前为空）。
    /// </summary>
    [JsonPropertyName("data")]
    public string Data { get; set; } = string.Empty;
}
