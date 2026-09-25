// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Acs;

/// <summary>
/// 智能门禁外部用户信息（权限组成员 / 访客）
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Acs")]
public class AcsExternalUser
{
    /// <summary>
    /// <para>用户类型</para>
    /// <para>必填：是（权限组成员/访客创建场景）</para>
    /// <para>可选值：<list type="bullet">
    /// <item>1：员工</item>
    /// <item>2：部门</item>
    /// <item>10：全体员工</item>
    /// <item>11：访客</item>
    /// </list></para>
    /// <para>示例值：11</para>
    /// </summary>
    [JsonPropertyName("user_type")]
    public int? UserType { get; set; }

    /// <summary>
    /// <para>用户 ID</para>
    /// <para>必填：否</para>
    /// <para>示例值：ou_7dab8a3d3cdcc9da365777c7ad535d62</para>
    /// </summary>
    [JsonPropertyName("user_id")]
    public string? UserId { get; set; }

    /// <summary>
    /// <para>用户名称</para>
    /// <para>必填：否</para>
    /// <para>示例值：张三</para>
    /// </summary>
    [JsonPropertyName("user_name")]
    public string? UserName { get; set; }

    /// <summary>
    /// <para>电话号码</para>
    /// <para>必填：否</para>
    /// <para>示例值：1357890001</para>
    /// </summary>
    [JsonPropertyName("phone_num")]
    public string? PhoneNum { get; set; }

    /// <summary>
    /// <para>部门 ID</para>
    /// <para>必填：否</para>
    /// <para>示例值：od-f7d44ab733f7602f5cc5194735fd9aaf</para>
    /// </summary>
    [JsonPropertyName("department_id")]
    public string? DepartmentId { get; set; }
}
