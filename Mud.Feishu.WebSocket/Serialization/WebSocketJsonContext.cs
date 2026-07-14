// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

#if NET8_0_OR_GREATER
using System.Text.Json;
using System.Text.Json.Serialization;
using Mud.Feishu.WebSocket.DataModels;

namespace Mud.Feishu.WebSocket.Serialization;

/// <summary>
/// WebSocket协议消息的JSON源生成上下文。
/// 覆盖WebSocket连接过程中的所有协议消息类型。
/// </summary>
[JsonSourceGenerationOptions(
    PropertyNameCaseInsensitive = true,
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    GenerationMode = JsonSourceGenerationMode.Metadata)]
// 注册WebSocket协议消息类型（N-02：共9个类型）
[JsonSerializable(typeof(FeishuWebSocketMessage))]   // 抽象基类，多态场景必需
[JsonSerializable(typeof(EventMessage))]
[JsonSerializable(typeof(AuthMessage))]
[JsonSerializable(typeof(AuthData))]
[JsonSerializable(typeof(AuthResponseMessage))]
[JsonSerializable(typeof(PingMessage))]
[JsonSerializable(typeof(PongMessage))]
[JsonSerializable(typeof(HeartbeatMessage))]
[JsonSerializable(typeof(HeartbeatData))]            // HeartbeatMessage.Data 引用的子类型
internal partial class WebSocketJsonContext : JsonSerializerContext { }
#endif