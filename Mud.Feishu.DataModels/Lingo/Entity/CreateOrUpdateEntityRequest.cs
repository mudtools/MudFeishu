// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Lingo;

/// <summary>
/// 创建/更新词条的请求体（创建免审词条、更新免审词条、创建草稿、更新草稿四个接口共用，字段与词条对象一致）。
/// <para>创建新词条时 <see cref="Id"/> 可不填；更新已有词条时需填入词条 ID。</para>
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Lingo")]
public class CreateOrUpdateEntityRequest
{
    /// <summary>
    /// <para>词条 ID（需要更新某个词条时填写，若是创建新词条可不填写）</para>
    /// <para>必填：否</para>
    /// <para>示例值：enterprise_40217521</para>
    /// </summary>
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    /// <summary>
    /// <para>词条名，最多 1 个</para>
    /// <para>必填：是</para>
    /// </summary>
    [JsonPropertyName("main_keys")]
    public LingoTerm[]? MainKeys { get; set; }

    /// <summary>
    /// <para>别名，最多 10 个</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("aliases")]
    public LingoTerm[]? Aliases { get; set; }

    /// <summary>
    /// <para>纯文本格式词条释义。注：description 和 rich_text 至少有一个，否则会报错 1540001</para>
    /// <para>必填：否</para>
    /// <para>长度范围：1 ～ 5000 字符</para>
    /// <para>示例值：词典是飞书提供的一款知识管理工具</para>
    /// </summary>
    [JsonPropertyName("description")]
    public string? Description { get; set; }

    /// <summary>
    /// <para>词条相关信息</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("related_meta")]
    public LingoRelatedMeta? RelatedMeta { get; set; }

    /// <summary>
    /// <para>富文本格式（填写富文本后 description 失效可不填），最多 5000 字符</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("rich_text")]
    public string? RichText { get; set; }

    /// <summary>
    /// <para>外部系统关联数据；使用外部系统关联时 provider、outer_id 均必填</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("outer_info")]
    public LingoOuterInfo? OuterInfo { get; set; }

    /// <summary>
    /// <para>国际化的词条释义，最多 3 条</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("i18n_descs")]
    public LingoI18nEntryDesc[]? I18nDescs { get; set; }
}
