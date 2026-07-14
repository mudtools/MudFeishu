// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Bitable;

/// <summary>
/// <para>协作者列表</para>
/// </summary>
public class AppRoleMember
{
    /// <summary>
    /// <para>协作者的 open_id</para>
    /// <para>必填：否</para>
    /// <para>示例值：ou_7dab8a3d3cdcc9da365777c7ad5abcef</para>
    /// </summary>
    [JsonPropertyName("open_id")]
    public string? OpenId { get; set; }

    /// <summary>
    /// <para>协作者的 union_id</para>
    /// <para>必填：否</para>
    /// <para>示例值：on_7dab8a3d3cdcc9da365777c7ad5abcef</para>
    /// </summary>
    [JsonPropertyName("union_id")]
    public string? UnionId { get; set; }

    /// <summary>
    /// <para>协作者的 user_id</para>
    /// <para>必填：否</para>
    /// <para>示例值：13e4beac</para>
    /// </summary>
    [JsonPropertyName("user_id")]
    public string? UserId { get; set; }

    /// <summary>
    /// <para>协作者为一个群聊，群聊的 chat_id</para>
    /// <para>必填：否</para>
    /// <para>示例值：oc_a0553eda9014c201e6969b478895c230</para>
    /// </summary>
    [JsonPropertyName("chat_id")]
    public string? ChatId { get; set; }

    /// <summary>
    /// <para>协作者为一个部门，部门的 department_id</para>
    /// <para>必填：否</para>
    /// <para>示例值：h121921</para>
    /// </summary>
    [JsonPropertyName("department_id")]
    public string? DepartmentId { get; set; }

    /// <summary>
    /// <para>协作者为一个部门，部门的 open_department_id</para>
    /// <para>必填：否</para>
    /// <para>示例值：od-4e6ac4d14bcd5071a37a39de902c7141</para>
    /// </summary>
    [JsonPropertyName("open_department_id")]
    public string? OpenDepartmentId { get; set; }

    /// <summary>
    /// <para>协作者的名称</para>
    /// <para>必填：否</para>
    /// <para>示例值：张敏</para>
    /// </summary>
    [JsonPropertyName("member_name")]
    public string? MemberName { get; set; }

    /// <summary>
    /// <para>协作者的英文名称</para>
    /// <para>必填：否</para>
    /// <para>示例值：Min Zhang</para>
    /// </summary>
    [JsonPropertyName("member_en_name")]
    public string? MemberEnName { get; set; }

    /// <summary>
    /// <para>协作者的类型</para>
    /// <para>必填：否</para>
    /// <para>示例值：user</para>
    /// <para>可选值：<list type="bullet">
    /// <item>user：用户</item>
    /// <item>chat：群组</item>
    /// <item>department：部门</item>
    /// </list></para>
    /// </summary>
    [JsonPropertyName("member_type")]
    public string? MemberType { get; set; }
}