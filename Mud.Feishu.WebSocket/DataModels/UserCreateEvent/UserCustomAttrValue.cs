// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.WebSocket.DataModels.UserCreateEvent;

/// <summary>
/// 用户自定义属性值类，用于表示飞书平台中用户自定义字段的各种可能值
/// </summary>
public class UserCustomAttrValue
{
    /// <summary>
    /// 文本类型的属性值
    /// </summary>
    [JsonPropertyName("text")]
    public string? Text { get; set; }

    /// <summary>
    /// URL链接类型的属性值，用于默认跳转链接
    /// </summary>
    [JsonPropertyName("url")]
    public string? Url { get; set; }

    /// <summary>
    /// PC端专用URL链接类型的属性值
    /// </summary>
    [JsonPropertyName("pc_url")]
    public string? PcUrl { get; set; }

    /// <summary>
    /// 枚举选项的ID值，用于枚举或图片枚举类型的属性
    /// </summary>
    [JsonPropertyName("option_id")]
    public string? OptionId { get; set; }

    /// <summary>
    /// 枚举选项的值，用于枚举或图片枚举类型的属性
    /// </summary>
    [JsonPropertyName("option_value")]
    public string? OptionValue { get; set; }

    /// <summary>
    /// 名称属性值
    /// </summary>
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    /// <summary>
    /// 图片URL链接，用于图片或图片枚举类型的属性
    /// </summary>
    [JsonPropertyName("picture_url")]
    public string? PictureUrl { get; set; }

    /// <summary>
    /// 通用用户引用对象，用于人员类型的属性值
    /// </summary>
    [JsonPropertyName("generic_user")]
    public GenericUser? GenericUser { get; set; }
}