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
/// 事件订阅请求 DTO，用于替代匿名类型序列化（P1-5 修复 / WS-06）。
/// </summary>
/// <remarks>
/// 飞书 WebSocket 协议要求订阅请求格式为：
/// <c>{ "type": "subscribe", "data": { "events": [...] }, "timestamp": ... }</c>。
/// 此前使用匿名类型 <c>new { type, data = new { events }, timestamp }</c> 进行序列化，
/// 在 Native AOT/Trimming 下存在反射失败风险。
/// </remarks>
public class SubscriptionRequest
{
    /// <summary>
    /// 消息类型，固定为 "subscribe"。
    /// </summary>
    [JsonPropertyName("type")]
    public string Type { get; set; } = "subscribe";

    /// <summary>
    /// 订阅数据，包含要订阅的事件类型列表。
    /// </summary>
    [JsonPropertyName("data")]
    public SubscriptionRequestData Data { get; set; } = new();
}

/// <summary>
/// 事件订阅请求数据，包含要订阅的事件类型列表。
/// </summary>
public class SubscriptionRequestData
{
    /// <summary>
    /// 要订阅的事件类型列表。
    /// </summary>
    [JsonPropertyName("events")]
    public List<string> Events { get; set; } = new();
}
