// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Drive.Files;

/// <summary>
/// <para>文件的元数据列表</para>
/// </summary>
public record FileMetaInfo
{
    /// <summary>
    /// <para>文件的 token</para>
    /// <para>必填：是</para>
    /// <para>示例值：doccnfYZzTlvXqZIGTdAHKabcef</para>
    /// </summary>
    [JsonPropertyName("doc_token")]
    public string DocToken { get; set; } = string.Empty;

    /// <summary>
    /// <para>文件的类型</para>
    /// <para>必填：是</para>
    /// <para>示例值：doc</para>
    /// </summary>
    [JsonPropertyName("doc_type")]
    public string DocType { get; set; } = string.Empty;

    /// <summary>
    /// <para>标题</para>
    /// <para>必填：是</para>
    /// <para>示例值：sampletitle</para>
    /// </summary>
    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// <para>文件的所有者</para>
    /// <para>必填：是</para>
    /// <para>示例值：ou_b13d41c02edc52ce66aaae67bf1abcef</para>
    /// </summary>
    [JsonPropertyName("owner_id")]
    public string OwnerId { get; set; } = string.Empty;

    /// <summary>
    /// <para>创建时间。UNIX 时间戳，单位为秒</para>
    /// <para>必填：是</para>
    /// <para>示例值：1652066345</para>
    /// </summary>
    [JsonPropertyName("create_time")]
    public string CreateTime { get; set; } = string.Empty;

    /// <summary>
    /// <para>最后编辑者</para>
    /// <para>必填：是</para>
    /// <para>示例值：ou_b13d41c02edc52ce66aaae67bf1abcef</para>
    /// </summary>
    [JsonPropertyName("latest_modify_user")]
    public string LatestModifyUser { get; set; } = string.Empty;

    /// <summary>
    /// <para>最后编辑时间。UNIX 时间戳，单位为秒</para>
    /// <para>必填：是</para>
    /// <para>示例值：1652066345</para>
    /// </summary>
    [JsonPropertyName("latest_modify_time")]
    public string LatestModifyTime { get; set; } = string.Empty;

    /// <summary>
    /// <para>文档访问链接</para>
    /// <para>必填：是</para>
    /// <para>示例值：https://sample.feishu.cn/docs/doccnfYZzTlvXqZIGTdAHKabcef</para>
    /// </summary>
    [JsonPropertyName("url")]
    public string Url { get; set; } = string.Empty;

    /// <summary>
    /// <para>文档密级标签名称</para>
    /// <para>必填：否</para>
    /// <para>示例值：L2-内部</para>
    /// </summary>
    [JsonPropertyName("sec_label_name")]
    public string? SecLabelName { get; set; }
}
