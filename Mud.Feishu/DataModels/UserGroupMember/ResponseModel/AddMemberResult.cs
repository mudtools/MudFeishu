// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.UserGroupMember;

/// <summary>
/// 添加成员结果响应模型，包含用户组成员添加操作的结果信息。
/// 用于单个用户添加到用户组操作成功后返回的详细信息。
/// </summary>
public class AddMemberResult
{
    /// <summary>
    /// 成员ID。
    /// <para>表示成功添加到用户组的成员唯一标识符。</para>
    /// <para>可以是用户ID、部门ID或其他类型的成员标识。</para>
    /// <para>示例值："ou_7d8a6e9d3c2c1b882487c7398e9d8f7"</para>
    /// </summary>
    [JsonPropertyName("member_id")]
    public string MemberId { get; set; } = string.Empty;

    /// <summary>
    /// 操作结果代码。
    /// <para>表示添加成员操作的执行结果状态。</para>
    /// <para>0表示成功，非0值表示失败或警告。</para>
    /// <para>示例值：0（成功）</para>
    /// </summary>
    [JsonPropertyName("code")]
    public int Code { get; set; }
}
