// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.HelpDesk;


/// <summary>
/// <para>工单创建用户</para>
/// </summary>
[HttpJsonSerializable(SerializerClassName = "HelpDesk")]
public class TicketUser
{
    /// <summary>
    /// <para>用户ID</para>
    /// <para>必填：否</para>
    /// <para>示例值：ou_37019b7c830210acd88fdce886e25c71</para>
    /// </summary>
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    /// <summary>
    /// <para>用户头像url</para>
    /// <para>必填：否</para>
    /// <para>示例值：https://xxxx</para>
    /// </summary>
    [JsonPropertyName("avatar_url")]
    public string? AvatarUrl { get; set; }

    /// <summary>
    /// <para>用户名</para>
    /// <para>必填：否</para>
    /// <para>示例值：abc</para>
    /// </summary>
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    /// <summary>
    /// <para>用户邮箱</para>
    /// <para>必填：否</para>
    /// <para>示例值：xxxx@abc.com</para>
    /// </summary>
    [JsonPropertyName("email")]
    public string? Email { get; set; }

    /// <summary>
    /// <para>所在部门名称</para>
    /// <para>必填：否</para>
    /// <para>示例值：用户部门名称(有权限才展示)</para>
    /// </summary>
    [JsonPropertyName("department")]
    public string? Department { get; set; }

    /// <summary>
    /// <para>城市</para>
    /// <para>必填：否</para>
    /// <para>示例值：城市</para>
    /// </summary>
    [JsonPropertyName("city")]
    public string? City { get; set; }

    /// <summary>
    /// <para>国家代号(CountryCode)，参考：http://www.mamicode.com/info-detail-2186501.html</para>
    /// <para>必填：否</para>
    /// <para>示例值：国家</para>
    /// </summary>
    [JsonPropertyName("country")]
    public string? Country { get; set; }
}
