// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Lingo;

/// <summary>
/// 飞书词典词条（entity），各查询类接口返回的词条对象
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Lingo")]
public class LingoEntity
{
    /// <summary>
    /// <para>词条 ID</para>
    /// <para>必填：否（仅查询结果返回）</para>
    /// <para>示例值：enterprise_402***21</para>
    /// </summary>
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    /// <summary>
    /// <para>词条名，最多 1 个</para>
    /// <para>必填：否（仅查询结果返回）</para>
    /// </summary>
    [JsonPropertyName("main_keys")]
    public LingoTerm[]? MainKeys { get; set; }

    /// <summary>
    /// <para>别名，最多 10 个</para>
    /// <para>必填：否（仅查询结果返回）</para>
    /// </summary>
    [JsonPropertyName("aliases")]
    public LingoTerm[]? Aliases { get; set; }

    /// <summary>
    /// <para>纯文本格式词条释义。注：description 和 rich_text 至少有一个，否则会报错 1540001</para>
    /// <para>必填：否（仅查询结果返回）</para>
    /// <para>长度范围：1 ～ 5000 字符</para>
    /// </summary>
    [JsonPropertyName("description")]
    public string? Description { get; set; }

    /// <summary>
    /// <para>词条创建者</para>
    /// <para>必填：否（仅查询结果返回，需 contact:user.employee_id:readonly 字段权限才返回）</para>
    /// <para>示例值：ou_30b07b63089ea46518789914dac63d36</para>
    /// </summary>
    [JsonPropertyName("creator")]
    public string? Creator { get; set; }

    /// <summary>
    /// <para>词条创建时间</para>
    /// <para>必填：否（仅查询结果返回）</para>
    /// <para>示例值：1627540853</para>
    /// </summary>
    [JsonPropertyName("create_time")]
    public string? CreateTime { get; set; }

    /// <summary>
    /// <para>最近一次更新者</para>
    /// <para>必填：否（仅查询结果返回，需 contact:user.employee_id:readonly 字段权限才返回）</para>
    /// <para>示例值：ou_30b07b63089ea46518789914dac63d36</para>
    /// </summary>
    [JsonPropertyName("updater")]
    public string? Updater { get; set; }

    /// <summary>
    /// <para>词条最近更新时间（秒级时间戳）</para>
    /// <para>必填：否（仅查询结果返回）</para>
    /// <para>示例值：1627541853</para>
    /// </summary>
    [JsonPropertyName("update_time")]
    public string? UpdateTime { get; set; }

    /// <summary>
    /// <para>更多相关信息</para>
    /// <para>必填：否（仅查询结果返回）</para>
    /// </summary>
    [JsonPropertyName("related_meta")]
    public LingoRelatedMeta? RelatedMeta { get; set; }

    /// <summary>
    /// <para>当前词条收到的反馈数据</para>
    /// <para>必填：否（仅查询结果返回）</para>
    /// </summary>
    [JsonPropertyName("statistics")]
    public LingoStatistics? Statistics { get; set; }

    /// <summary>
    /// <para>外部系统关联数据</para>
    /// <para>必填：否（仅查询结果返回）</para>
    /// </summary>
    [JsonPropertyName("outer_info")]
    public LingoOuterInfo? OuterInfo { get; set; }

    /// <summary>
    /// <para>富文本格式（填写富文本后 description 失效可不填）</para>
    /// <para>必填：否（仅查询结果返回）</para>
    /// <para>长度范围：1 ～ 5000 字符</para>
    /// </summary>
    [JsonPropertyName("rich_text")]
    public string? RichText { get; set; }

    /// <summary>
    /// <para>词条的创建来源，1 - 用户主动创建，2 - 批量导入，3 - 官方词，4 - OpenAPI 创建</para>
    /// <para>必填：否（仅查询结果返回）</para>
    /// <para>示例值：4</para>
    /// </summary>
    [JsonPropertyName("source")]
    public int? Source { get; set; }

    /// <summary>
    /// <para>国际化的词条释义，最多 3 条</para>
    /// <para>必填：否（仅查询结果返回）</para>
    /// </summary>
    [JsonPropertyName("i18n_descs")]
    public LingoI18nEntryDesc[]? I18nDescs { get; set; }
}
