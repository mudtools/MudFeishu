// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Application;

/// <summary>
/// 应用所有者信息。
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Application")]
public class ApplicationOwner
{
    /// <summary>
    /// <para>所有者类型。1：个人；2：客服（服务台）</para>
    /// <para>必填：是</para>
    /// <para>示例值：1</para>
    /// </summary>
    [JsonPropertyName("type")]
    public int? Type { get; set; }

    /// <summary>
    /// <para>所有者的用户 ID</para>
    /// <para>必填：否</para>
    /// <para>示例值：ou_d317f090b7258ad0372aa53963cda70d</para>
    /// </summary>
    [JsonPropertyName("owner_id")]
    public string? OwnerId { get; set; }

    /// <summary>
    /// <para>所有者名称</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    /// <summary>
    /// <para>所有者为客服时对应的客服 ID</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("help_desk")]
    public string? HelpDesk { get; set; }

    /// <summary>
    /// <para>所有者邮箱</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("email")]
    public string? Email { get; set; }

    /// <summary>
    /// <para>所有者手机号</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("phone")]
    public string? Phone { get; set; }

    /// <summary>
    /// <para>所有者为客服账号时对应的客服账号名</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("customer_service_account")]
    public string? CustomerServiceAccount { get; set; }
}
