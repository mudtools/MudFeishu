// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Aily;

/// <summary>
/// <para>数据知识（Data Asset）信息</para>
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Aily")]
public class DataAsset
{
    /// <summary>
    /// <para>数据知识 ID</para>
    /// <para>必填：否</para>
    /// <para>示例值：data_asset_dafefadsaf1</para>
    /// <para>最大长度：255</para>
    /// </summary>
    [JsonPropertyName("data_asset_id")]
    public string? DataAssetId { get; set; }

    /// <summary>
    /// <para>数据知识标题</para>
    /// <para>必填：否</para>
    /// <para>示例值：{"zh_cn":"标题"}</para>
    /// </summary>
    [JsonPropertyName("label")]
    public Dictionary<string, string>? Label { get; set; }

    /// <summary>
    /// <para>数据知识描述</para>
    /// <para>必填：否</para>
    /// <para>示例值：{"zh_cn":"描述"}</para>
    /// </summary>
    [JsonPropertyName("description")]
    public Dictionary<string, string>? Description { get; set; }

    /// <summary>
    /// <para>数据资源类型</para>
    /// <para>必填：否</para>
    /// <para>可选值包括：excel、pdf、pptx、txt、docx、mysql、postgresql、飞书多维表格、飞书服务台、飞书 Wiki、飞书云文档、数据表等</para>
    /// </summary>
    [JsonPropertyName("data_source_type")]
    public string? DataSourceType { get; set; }

    /// <summary>
    /// <para>数据连接状态</para>
    /// <para>必填：否</para>
    /// <para>可选值：<list type="bullet">
    /// <item>waiting：等待连接</item>
    /// <item>connecting：连接中</item>
    /// <item>connected：连接成功</item>
    /// <item>incremental_syncing：增量同步中</item>
    /// <item>partial_success：部分成功</item>
    /// <item>failed：连接失败</item>
    /// </list></para>
    /// </summary>
    [JsonPropertyName("connect_status")]
    public string? ConnectStatus { get; set; }

    /// <summary>
    /// <para>数据知识分类列表</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("tags")]
    public DataAssetTag[]? Tags { get; set; }

    /// <summary>
    /// <para>数据知识项列表</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("items")]
    public DataAssetItem[]? Items { get; set; }

    /// <summary>
    /// <para>连接状态失败信息</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("connect_failed_reason")]
    public string? ConnectFailedReason { get; set; }

    /// <summary>
    /// <para>数据连接类型</para>
    /// <para>必填：否</para>
    /// <para>可选值：<list type="bullet">
    /// <item>import：导入</item>
    /// <item>direct：直连</item>
    /// </list></para>
    /// </summary>
    [JsonPropertyName("connect_type")]
    public string? ConnectType { get; set; }

    /// <summary>
    /// <para>创建时间，毫秒时间戳</para>
    /// <para>必填：否</para>
    /// <para>示例值：1616920429000</para>
    /// </summary>
    [JsonPropertyName("created_time")]
    public string? CreatedTime { get; set; }

    /// <summary>
    /// <para>更新时间，毫秒时间戳</para>
    /// <para>必填：否</para>
    /// <para>示例值：1616920429000</para>
    /// </summary>
    [JsonPropertyName("updated_time")]
    public string? UpdatedTime { get; set; }
}
