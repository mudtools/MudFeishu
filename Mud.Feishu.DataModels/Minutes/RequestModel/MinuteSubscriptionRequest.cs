// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Minutes;

/// <summary>
/// 妙记变更事件订阅请求体（订阅与取消订阅共用）
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Minutes")]
public class MinuteSubscriptionRequest
{
    /// <summary>
    /// <para>事件类型</para>
    /// <para>必填：否</para>
    /// <para>示例值：minutes.minute.generated_v1</para>
    /// <para>可选值：minutes.minute.generated_v1（妙记生成事件）</para>
    /// </summary>
    [JsonPropertyName("event_type")]
    public string? EventType { get; set; }
}
