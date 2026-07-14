// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.HelpDesk;

/// <summary>
/// <para>客服信息</para>
/// </summary>
[HttpJsonSerializable(SerializerClassName = "HelpDesk")]
public class AgentUser
{
    /// <summary>
    /// <para>客服 id</para>
    /// <para>必填：否</para>
    /// <para>示例值：ou_ea651a5c09e2d01af8acd34059f5359b</para>
    /// </summary>
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    /// <summary>
    /// <para>avatar url</para>
    /// <para>必填：否</para>
    /// <para>示例值：https://avatar-url.com/test.png</para>
    /// </summary>
    [JsonPropertyName("avatar_url")]
    public string? AvatarUrl { get; set; }

    /// <summary>
    /// <para>客服名字</para>
    /// <para>必填：否</para>
    /// <para>示例值：test-user</para>
    /// </summary>
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    /// <summary>
    /// <para>email</para>
    /// <para>必填：否</para>
    /// <para>示例值：test@bytedance.com</para>
    /// </summary>
    [JsonPropertyName("email")]
    public string? Email { get; set; }

    /// <summary>
    /// <para>部门</para>
    /// <para>必填：否</para>
    /// <para>示例值：测试部门</para>
    /// </summary>
    [JsonPropertyName("department")]
    public string? Department { get; set; }

    /// <summary>
    /// <para>公司名</para>
    /// <para>必填：否</para>
    /// <para>示例值：test-company</para>
    /// </summary>
    [JsonPropertyName("company_name")]
    public string? CompanyName { get; set; }
}
