// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.EventCallback.Application;

/// <summary>
/// ApplicationCloudDoc
/// <para>云文档应用</para>
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Application")]
public class ApplicationCloudDoc
{
    /// <summary>
    /// <para>云空间重定向 url</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("space_url")]
    public string? SpaceUrl { get; set; }

    /// <summary>
    /// <para>国际化信息</para>
    /// <para>**数据校验规则**：</para>
    /// <para>- 最小长度：`1`</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("i18n")]
    public ApplicationCloudDocI18nInfo[]? I18n { get; set; }

    /// <summary>
    /// <para>图标链接</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("icon_url")]
    public string? IconUrl { get; set; }

    /// <summary>
    /// <para>云文档支持模式</para>
    /// <para>**可选值有**：</para>
    /// <para>- 0：未知</para>
    /// <para>- 1：移动端</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("mode")]
    public int? Mode { get; set; }
}
