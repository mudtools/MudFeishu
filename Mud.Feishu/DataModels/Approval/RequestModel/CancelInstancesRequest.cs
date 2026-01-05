// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Approval;

/// <summary>
/// 撤回审批实例请求体
/// </summary>
public class CancelInstancesRequest
{
    /// <summary>
    /// <para>审批定义 Code。</para>
    /// <para>必填：是</para>
    /// <para>示例值：7C468A54-8745-2245-9675-08B7C63E7A85</para>
    /// </summary>
    [JsonPropertyName("approval_code")]
    public string ApprovalCode { get; set; } = string.Empty;

    /// <summary>
    /// <para>审批实例 Code。</para>
    /// <para>必填：是</para>
    /// <para>示例值：81D31358-93AF-92D6-7425-01A5D67C4E71</para>
    /// </summary>
    [JsonPropertyName("instance_code")]
    public string InstanceCode { get; set; } = string.Empty;

    /// <summary>
    /// <para>审批提交人的用户 ID，ID 类型与查询参数 user_id_type 的取值一致。</para>
    /// <para>必填：是</para>
    /// <para>示例值：f7cb567e</para>
    /// </summary>
    [JsonPropertyName("user_id")]
    public string UserId { get; set; } = string.Empty;
}
