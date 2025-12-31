// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Approval;

/// <summary>
/// <para>发起人自选审批人时，可选择的范围。</para>
/// </summary>
public class ApproverRange
{
    /// <summary>
    /// <para>审批人类型</para>
    /// <para>必填：否</para>
    /// <para>示例值：ALL</para>
    /// <para>可选值：<list type="bullet">
    /// <item>ALL：全企业</item>
    /// <item>PERSONAL：指定审批人</item>
    /// <item>ROLE：指定角色</item>
    /// </list></para>
    /// </summary>
    [JsonPropertyName("type")]
    public string? Type { get; set; }

    /// <summary>
    /// <para>ID 列表。</para>
    /// <para>- 当 type 取值 ALL 时，无需传值。</para>
    /// <para>- 当 type 取值 PERSONAL 时，传入用户 ID，ID 类型与 user_id_type 取值一致。</para>
    /// <para>- 当 type 取值 ROLE 时，传入角色 ID。获取方式：成功[创建角色]后，在返回结果中可获取角色 ID。</para>
    /// <para>必填：否</para>
    /// <para>示例值：f7cb567e</para>
    /// </summary>
    [JsonPropertyName("id_list")]
    public string[]? IdList { get; set; }
}