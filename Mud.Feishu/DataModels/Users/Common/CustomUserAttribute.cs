// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Users;


/// <summary>
/// 自定义字段。
/// </summary>
public class CustomUserAttribute
{
    /// <summary>
    /// 自定义字段类型。可选值有：TEXT：文本 HREF：网页 ENUMERATION：枚举 PICTURE_ENUM：图片 GENERIC_USER：用户
    /// </summary>
    [JsonPropertyName("type")]
    public string? Type { get; set; }

    /// <summary>
    /// 自定义字段 ID。
    /// </summary>
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    /// <summary>
    /// 自定义字段取值。
    /// </summary>
    [JsonPropertyName("value")]
    public CustomUserAttributeValue? Value { get; set; }
}
