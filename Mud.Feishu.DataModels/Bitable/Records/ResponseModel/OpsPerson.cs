// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Bitable;


/// <summary>
/// <para>该记录的创建人信息。本接口不返回该参数</para>
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Bitable")]
public class OpsPerson
{
    /// <summary>
    /// <para>创建人的用户 ID，ID 类型与 `user_id_type` 所指定的类型一致</para>
    /// <para>必填：否</para>
    /// <para>示例值：ou_9a971ded01b4ca66f4798549878abcef</para>
    /// </summary>
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    /// <summary>
    /// <para>用户的中文名称</para>
    /// <para>必填：否</para>
    /// <para>示例值：张敏</para>
    /// </summary>
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    /// <summary>
    /// <para>用户的英文名称</para>
    /// <para>必填：否</para>
    /// <para>示例值：Min Zhang</para>
    /// </summary>
    [JsonPropertyName("en_name")]
    public string? EnName { get; set; }

    /// <summary>
    /// <para>用户的邮箱</para>
    /// <para>必填：否</para>
    /// <para>示例值：zhangmin@feishu.com</para>
    /// </summary>
    [JsonPropertyName("email")]
    public string? Email { get; set; }

    /// <summary>
    /// <para>头像链接</para>
    /// <para>必填：否</para>
    /// <para>示例值：https://example.com/avatar</para>
    /// </summary>
    [JsonPropertyName("avatar_url")]
    public string? AvatarUrl { get; set; }
}
