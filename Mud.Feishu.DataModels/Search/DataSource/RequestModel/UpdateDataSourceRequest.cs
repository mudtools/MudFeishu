// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Search;

/// <summary>
/// <para>更新数据源请求体</para>
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Search")]
public class UpdateDataSourceRequest
{
    /// <summary>
    /// <para>数据源的展示名称</para>
    /// <para>**示例值**："客服工单"</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    /// <summary>
    /// <para>数据源状态，0-已上线，1-未上线</para>
    /// <para>**示例值**：0</para>
    /// <para>**可选值有**：</para>
    /// <para>0:已上线,1:未上线</para>
    /// <para>必填：否</para>
    /// <para>可选值：<list type="bullet">
    /// <item>0：已上线</item>
    /// <item>1：未上线</item>
    /// </list></para>
    /// </summary>
    [JsonPropertyName("state")]
    public int? State { get; set; }

    /// <summary>
    /// <para>对于数据源的描述</para>
    /// <para>**示例值**："搜索客服工单"</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("description")]
    public string? Description { get; set; }

    /// <summary>
    /// <para>数据源在 search tab 上的展示图标路径</para>
    /// <para>**示例值**："https://www.xxx.com/open.jpg"</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("icon_url")]
    public string? IconUrl { get; set; }

    /// <summary>
    /// <para>数据源名称多语言配置，json格式，key为语言locale，value为对应文案，例如{"zh_cn":"测试数据源", "en_us":"Test DataSource"}</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("i18n_name")]
    public I18nName? I18nName { get; set; }

    /// <summary>
    /// <para>数据源描述多语言配置，json格式，key为语言locale，value为对应文案，例如{"zh_cn":"搜索测试数据源相关数据", "en_us":"Search data from Test DataSource"}</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("i18n_description")]
    public I18nName? I18nDescription { get; set; }
}
