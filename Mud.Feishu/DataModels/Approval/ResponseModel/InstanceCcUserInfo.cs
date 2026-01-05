// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Approval;

/// <summary>
/// 审批实体抄送人列表
/// </summary>
public class InstanceCcUserInfo
{
    /// <summary>
    /// <para>抄送人的 user_id</para>
    /// <para>必填：否</para>
    /// <para>示例值：eea5gefe</para>
    /// </summary>
    [JsonPropertyName("user_id")]
    public string? UserId { get; set; }

    /// <summary>
    /// <para>审批实例内抄送唯一标识</para>
    /// <para>必填：否</para>
    /// <para>示例值：123445</para>
    /// </summary>
    [JsonPropertyName("cc_id")]
    public string? CcId { get; set; }

    /// <summary>
    /// <para>抄送人的 open_id</para>
    /// <para>必填：否</para>
    /// <para>示例值：ou_12345</para>
    /// </summary>
    [JsonPropertyName("open_id")]
    public string? OpenId { get; set; }
}
