// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Search;

/// <summary>
/// <para>文档搜索元信息</para>
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Search")]
public class DocMeta
{
    /// <summary>
    /// <para>文档类型</para>
    /// <para>必填：否</para>
    /// <para>示例值：SHORTCUT</para>
    /// <para>可选值：<list type="bullet">
    /// <item>DOC：文档</item>
    /// <item>SHEET：表格</item>
    /// <item>BITABLE：多维表格</item>
    /// <item>MINDNOTE：思维导图</item>
    /// <item>FILE：文件</item>
    /// <item>WIKI：维基</item>
    /// <item>DOCX：新版文档</item>
    /// <item>FOLDER：space文件夹</item>
    /// <item>CATALOG：wiki2.0文件夹</item>
    /// <item>SLIDES：新版本幻灯片</item>
    /// <item>SHORTCUT：快捷方式</item>
    /// </list></para>
    /// </summary>
    [JsonPropertyName("doc_types")]
    public string? DocTypes { get; set; }

    /// <summary>
    /// <para>更新时间戳（秒）</para>
    /// <para>必填：否</para>
    /// <para>示例值：1766567446</para>
    /// </summary>
    [JsonPropertyName("update_time")]
    public long? UpdateTime { get; set; }

    /// <summary>
    /// <para>文档链接</para>
    /// <para>必填：否</para>
    /// <para>示例值：https://www.feishu.cn/docs/dox-1234567890abcdef</para>
    /// </summary>
    [JsonPropertyName("url")]
    public string? Url { get; set; }

    /// <summary>
    /// <para>所有者名称</para>
    /// <para>必填：否</para>
    /// <para>示例值：张三</para>
    /// </summary>
    [JsonPropertyName("owner_name")]
    public string? OwnerName { get; set; }

    /// <summary>
    /// <para>所有者OpenID</para>
    /// <para>必填：否</para>
    /// <para>示例值：ou-7890123456abcdef</para>
    /// </summary>
    [JsonPropertyName("owner_id")]
    public string? OwnerId { get; set; }

    /// <summary>
    /// <para>是否跨租户</para>
    /// <para>必填：否</para>
    /// <para>示例值：false</para>
    /// </summary>
    [JsonPropertyName("is_cross_tenant")]
    public bool? IsCrossTenant { get; set; }

    /// <summary>
    /// <para>文档创建时间戳（秒）</para>
    /// <para>必填：否</para>
    /// <para>示例值：1766567446</para>
    /// <para>最大值：9223372036854776</para>
    /// <para>最小值：0</para>
    /// </summary>
    [JsonPropertyName("create_time")]
    public long? CreateTime { get; set; }

    /// <summary>
    /// <para>上次打开时间戳（秒）</para>
    /// <para>必填：否</para>
    /// <para>示例值：1766567446</para>
    /// <para>最大值：9223372036854776</para>
    /// <para>最小值：0</para>
    /// </summary>
    [JsonPropertyName("last_open_time")]
    public int? LastOpenTime { get; set; }

    /// <summary>
    /// <para>最后一次编辑用户OpenID</para>
    /// <para>必填：否</para>
    /// <para>示例值：ou-1122334455aabbcc</para>
    /// </summary>
    [JsonPropertyName("edit_user_id")]
    public string? EditUserId { get; set; }

    /// <summary>
    /// <para>最后一次编辑用户名称</para>
    /// <para>必填：否</para>
    /// <para>示例值：李四</para>
    /// </summary>
    [JsonPropertyName("edit_user_name")]
    public string? EditUserName { get; set; }

    /// <summary>
    /// <para>文档token</para>
    /// <para>必填：否</para>
    /// <para>示例值：dox_9876543210fedcba</para>
    /// </summary>
    [JsonPropertyName("token")]
    public string? Token { get; set; }

    /// <summary>
    /// <para>文件类型</para>
    /// <para>必填：否</para>
    /// <para>示例值：pdf</para>
    /// </summary>
    [JsonPropertyName("file_type")]
    public string? FileType { get; set; }

    /// <summary>
    /// <para>文档icon</para>
    /// <para>必填：否</para>
    /// <para>示例值：{\"type\":0,\"key\":\"\",\"obj_type\":22,\"file_type\":null,\"token\":\"FM78ddvYPo11I1xN7gjcSo1Ynuh\",\"version\":10191}</para>
    /// </summary>
    [JsonPropertyName("icon_info")]
    public string? IconInfo { get; set; }
}
