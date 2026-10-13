// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.EventCallback.Card;

/// <summary>
/// 卡片事件回调响应中 <c>card.type = template</c> 时的卡片数据。
/// </summary>
/// <remarks>
/// 对应通过飞书卡片构建工具搭建的卡片模板；模板 ID 与模板版本名均通过卡片构建工具获取。
/// </remarks>
[HttpJsonSerializable(SerializerClassName = "Card")]
public class CardCallbackTemplateData
{
    /// <summary>
    /// <para>卡片模板 ID（即卡片 ID）</para>
    /// <para>必填：是</para>
    /// </summary>
    [JsonPropertyName("template_id")]
    public string? TemplateId { get; set; }

    /// <summary>
    /// <para>卡片模板版本</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("template_version_name")]
    public string? TemplateVersionName { get; set; }

    /// <summary>
    /// <para>卡片模板变量，格式 <c>{key:value}</c></para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("template_variable")]
    public Dictionary<string, object?>? TemplateVariable { get; set; }
}
