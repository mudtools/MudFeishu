// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using ProtoBuf;

namespace Mud.Feishu.WebSocket;

/// <summary>
/// ProtoBuf 消息头部信息
/// <para>用于存储 WebSocket 二进制消息中的元数据键值对</para>
/// <para>常用于消息类型标识、认证信息等</para>
/// </summary>
[ProtoContract]
public class ProtoHeader
{
    /// <summary>
    /// 头部信息的键名
    /// <para>常见值包括：type（消息类型）、auth（认证信息）等</para>
    /// <para>示例值："type"、"auth"、"content-type"</para>
    /// </summary>
    [ProtoMember(1, Name = "key")]
    public string Key { get; set; } = string.Empty;

    /// <summary>
    /// 头部信息的值
    /// <para>与 Key 对应的具体值</para>
    /// <para>当 Key 为 "type" 时，可能的值："ping"、"pong"、"card"、"event" 等</para>
    /// <para>示例值："ping"、"event"、"application/json"</para>
    /// </summary>
    [ProtoMember(2, Name = "value")]
    public string Value { get; set; } = string.Empty;
}

/// <summary>
/// 飞书 WebSocket 事件的 ProtoBuf 数据结构
/// <para>用于序列化和反序列化 WebSocket 二进制消息的完整数据包</para>
/// <para>包含消息序号、服务信息、头部元数据、有效载荷等完整信息</para>
/// </summary>
[ProtoContract]
public class EventProtoData
{
    /// <summary>
    /// 消息序列号
    /// <para>用于保证消息的有序性和去重处理</para>
    /// <para>每条消息都有唯一的递增序列号</para>
    /// <para>示例值：12345678901234567890</para>
    /// </summary>
    [ProtoMember(1)]
    public ulong SeqID { get; set; }

    /// <summary>
    /// 日志ID（旧版本）
    /// <para>用于消息追踪和日志关联</para>
    /// <para>建议使用 LogIDNew 属性替代</para>
    /// <para>示例值：98765432109876543210</para>
    /// </summary>
    [ProtoMember(2)]
    public ulong LogID { get; set; }

    /// <summary>
    /// 服务类型标识
    /// <para>标识消息所属的服务模块</para>
    /// <para>不同数值代表不同的飞书服务</para>
    /// <para>示例值：1001（基础服务）、2001（消息服务）等</para>
    /// </summary>
    [ProtoMember(3, Name = "service")]
    public int Service { get; set; }

    /// <summary>
    /// 方法标识
    /// <para>标识具体的服务方法或操作类型</para>
    /// <para>与服务标识配合使用，确定具体的操作</para>
    /// <para>示例值：1（连接）、2（断开）、3（发送消息）等</para>
    /// </summary>
    [ProtoMember(4, Name = "method")]
    public int Method { get; set; }

    /// <summary>
    /// 消息头部信息数组
    /// <para>包含消息的元数据键值对</para>
    /// <para>常用于消息类型标识、认证信息、内容类型等</para>
    /// <para>示例：[{Key: "type", Value: "event"}, {Key: "auth", Value: "token"}]</para>
    /// </summary>
    [ProtoMember(5, Name = "headers")]
    public ProtoHeader[]? Headers { get; set; }

    /// <summary>
    /// 有效载荷编码方式
    /// <para>指定 Payload 字段的编码格式</para>
    /// <para>常见值：utf-8、json、plain 等</para>
    /// <para>示例值："utf-8"、"json"</para>
    /// </summary>
    [ProtoMember(6, Name = "payload_encoding")]
    public string? PayloadEncoding { get; set; } = string.Empty;

    /// <summary>
    /// 有效载荷类型
    /// <para>指定 Payload 字段的数据类型</para>
    /// <para>用于客户端正确解析和反序列化数据</para>
    /// <para>示例值："application/json"、"text/plain"、"binary"</para>
    /// </summary>
    [ProtoMember(7, Name = "payload_type")]
    public string? PayloadType { get; set; } = string.Empty;

    /// <summary>
    /// 消息有效载荷
    /// <para>包含实际的消息数据内容</para>
    /// <para>可以是 JSON 字符串的字节数组或其他二进制数据</para>
    /// <para>长度和格式由 PayloadEncoding 和 PayloadType 决定</para>
    /// <para>示例值：[123, 34, 109, 101, 115, 115, 97, 103, 101, 34, 58, 34, 72, 101, 108, 108, 111, 34, 125]</para>
    /// </summary>
    [ProtoMember(8, Name = "payload")]
    public byte[]? Payload { get; set; }

    /// <summary>
    /// 日志ID（新版本）
    /// <para>用于消息追踪和日志关联</para>
    /// <para>替代旧版本的 LogID 属性，提供更好的兼容性</para>
    /// <para>示例值："log_20231201_001"</para>
    /// </summary>
    [ProtoMember(9)]
    public string? LogIDNew { get; set; } = string.Empty;

    /// <summary>
    /// 获取消息类型
    /// <para>根据 Headers 中的 type 信息判断消息类型</para>
    /// <para>优先检查 Headers 中 Key 为 "type" 的 Header 的 Value 值</para>
    /// <para>支持的类型：ping、pong、card、event</para>
    /// <para>如果 Headers 为空或不包含 type 信息，则返回 Unknown</para>
    /// </summary>
    public MessageType MessageType
    {
        get
        {
            if (Headers is null || Headers.Length == 0) return MessageType.Unknown;
            if (Headers.Any(p => p.Key == "type" && p.Value == "ping")) return MessageType.Ping;
            if (Headers.Any(p => p.Key == "type" && p.Value == "pong")) return MessageType.Pong;
            if (Headers.Any(p => p.Key == "type" && p.Value == "card")) return MessageType.Card;
            if (Headers.Any(p => p.Key == "type" && p.Value == "event")) return MessageType.Event;
            return MessageType.Unknown;
        }
    }
}

/// <summary>
/// WebSocket 消息类型枚举
/// <para>定义飞书 WebSocket 通信中支持的所有消息类型</para>
/// <para>用于消息路由和处理逻辑的分支判断</para>
/// </summary>
public enum MessageType
{
    /// <summary>
    /// Ping 消息
    /// <para>用于心跳检测的请求消息</para>
    /// <para>客户端或服务器发送，对方需要回复 Pong 消息</para>
    /// <para>Headers 中 type 值为 "ping"</para>
    /// </summary>
    Ping,

    /// <summary>
    /// Pong 消息
    /// <para>用于心跳检测的响应消息</para>
    /// <para>作为 Ping 消息的回复，表示连接正常</para>
    /// <para>Headers 中 type 值为 "pong"</para>
    /// </summary>
    Pong,

    /// <summary>
    /// 卡片消息
    /// <para>飞书卡片相关的消息</para>
    /// <para>包含卡片内容、更新、交互等信息</para>
    /// <para>Headers 中 type 值为 "card"</para>
    /// </summary>
    Card,

    /// <summary>
    /// 事件消息
    /// <para>飞书各类业务事件的消息</para>
    /// <para>如：消息通知、用户状态变更、群组变更等</para>
    /// <para>Headers 中 type 值为 "event"</para>
    /// </summary>
    Event,

    /// <summary>
    /// 未知消息类型
    /// <para>无法识别的消息类型或默认值</para>
    /// <para>当 Headers 中没有 type 信息或 type 值不在上述范围内时返回</para>
    /// <para>用于错误处理和兼容性保证</para>
    /// </summary>
    Unknown
}
