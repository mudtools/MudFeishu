// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Security;

/// <summary>
/// 行为审计日志操作对象的扩展字段（部分字段键名保留飞书原始大小写，如 third_party_appID / permission_external_access_Type）
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Security")]
public class SecurityAuditObjectDetail
{
    /// <summary>
    /// <para>克隆来源</para>
    /// </summary>
    [JsonPropertyName("clone_source")]
    public string? CloneSource { get; set; }

    /// <summary>
    /// <para>文本详情</para>
    /// </summary>
    [JsonPropertyName("text_detail")]
    public string? TextDetail { get; set; }

    /// <summary>
    /// <para>文件名</para>
    /// </summary>
    [JsonPropertyName("file_name")]
    public string? FileName { get; set; }

    /// <summary>
    /// <para>第三方应用 ID（飞书原始字段键名为 third_party_appID）</para>
    /// </summary>
    [JsonPropertyName("third_party_appID")]
    public string? ThirdPartyAppId { get; set; }

    /// <summary>
    /// <para>包含的文件数量</para>
    /// </summary>
    [JsonPropertyName("contain_file_num")]
    public int? ContainFileNum { get; set; }

    /// <summary>
    /// <para>权限设置类型</para>
    /// </summary>
    [JsonPropertyName("permission_setting_type")]
    public string? PermissionSettingType { get; set; }

    /// <summary>
    /// <para>是否开启外部访问（飞书原始字段键名为 permission_external_access_Type）</para>
    /// </summary>
    [JsonPropertyName("permission_external_access_Type")]
    public bool? PermissionExternalAccessType { get; set; }

    /// <summary>
    /// <para>权限分享类型</para>
    /// </summary>
    [JsonPropertyName("permission_share_type")]
    public string? PermissionShareType { get; set; }

    /// <summary>
    /// <para>文件服务来源</para>
    /// </summary>
    [JsonPropertyName("file_service_source")]
    public string? FileServiceSource { get; set; }

    /// <summary>
    /// <para>OKR 下载内容</para>
    /// </summary>
    [JsonPropertyName("okr_download_content")]
    public string? OkrDownloadContent { get; set; }

    /// <summary>
    /// <para>容器类型</para>
    /// </summary>
    [JsonPropertyName("container_type")]
    public string? ContainerType { get; set; }

    /// <summary>
    /// <para>容器 ID</para>
    /// </summary>
    [JsonPropertyName("container_id")]
    public string? ContainerId { get; set; }

    /// <summary>
    /// <para>当前页码</para>
    /// </summary>
    [JsonPropertyName("current_page")]
    public string? CurrentPage { get; set; }
}
