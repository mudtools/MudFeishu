// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.ChatGroupNotice;

/// <summary>
/// <para>Add-ons</para>
/// </summary>
public record BlockAddOns
{
    /// <summary>
    /// <para>团队互动应用唯一ID</para>
    /// <para>必填：否</para>
    /// <para>示例值：7056882725002051603</para>
    /// </summary>
    [JsonPropertyName("component_id")]
    public string? ComponentId { get; set; }

    /// <summary>
    /// <para>团队互动应用类型，比如问答互动"blk_636a0a6657db8001c8df5488"</para>
    /// <para>必填：是</para>
    /// <para>示例值：blk_636a0a6657db8001c8df5488</para>
    /// </summary>
    [JsonPropertyName("component_type_id")]
    public string ComponentTypeId { get; set; } = string.Empty;

    /// <summary>
    /// <para>文档小组件内容数据，JSON 字符串</para>
    /// <para>必填：否</para>
    /// <para>示例值："{}"</para>
    /// </summary>
    [JsonPropertyName("record")]
    public string? Record { get; set; }
}

