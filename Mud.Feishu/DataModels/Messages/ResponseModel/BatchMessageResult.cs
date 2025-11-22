// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Messages;

/// <summary>
/// 批量消息发送结果类，用于封装批量发送消息接口的响应数据
/// </summary>
public class BatchMessageResult
{
    /// <summary>
    /// 无效的部门ID列表
    /// <para>这些部门ID在消息发送时被认为是无效的，可能原因包括部门不存在或无权访问</para>
    /// </summary>
    [JsonPropertyName("invalid_department_ids")]
    public List<string>? InvalidDepartmentIds { get; set; }

    /// <summary>
    /// 无效的OpenID列表
    /// <para>这些用户OpenID在消息发送时被认为是无效的，可能原因包括用户不存在或无权发送给该用户</para>
    /// </summary>
    [JsonPropertyName("invalid_open_ids")]
    public List<string>? InvalidOpenIds { get; set; }

    /// <summary>
    /// 无效的用户ID列表
    /// <para>这些用户ID在消息发送时被认为是无效的，可能原因包括用户不存在或无权发送给该用户</para>
    /// </summary>
    [JsonPropertyName("invalid_user_ids")]
    public List<string>? InvalidUserIds { get; set; }

    /// <summary>
    /// 无效的UnionID列表
    /// <para>这些UnionID在消息发送时被认为是无效的，可能原因包括用户不存在或无权发送给该用户</para>
    /// </summary>
    [JsonPropertyName("invalid_union_ids")]
    public List<string>? InvalidUnionIds { get; set; }

    /// <summary>
    /// 消息ID
    /// <para>成功发送消息后返回的唯一标识符，可用于后续的消息撤回或查询操作</para>
    /// </summary>
    [JsonPropertyName("message_id")]
    public string? MessageId { get; set; }
}