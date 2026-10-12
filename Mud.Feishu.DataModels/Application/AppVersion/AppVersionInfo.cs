// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Application;

/// <summary>
/// 应用版本信息。
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Application")]
public class AppVersionInfo
{
    /// <summary>
    /// <para>应用 App ID</para>
    /// <para>必填：是</para>
    /// <para>示例值：cli_9f3ca975326b501b</para>
    /// </summary>
    [JsonPropertyName("app_id")]
    public string? AppId { get; set; }

    /// <summary>
    /// <para>应用版本号</para>
    /// <para>必填：否</para>
    /// <para>示例值：1.0.0</para>
    /// </summary>
    [JsonPropertyName("version")]
    public string? Version { get; set; }

    /// <summary>
    /// <para>应用版本 ID</para>
    /// <para>必填：是</para>
    /// <para>示例值：oav_d317f090b7258ad0372aa53963cda70d</para>
    /// </summary>
    [JsonPropertyName("version_id")]
    public string? VersionId { get; set; }

    /// <summary>
    /// <para>应用名称</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("app_name")]
    public string? AppName { get; set; }

    /// <summary>
    /// <para>应用头像图片链接</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("avatar_url")]
    public string? AvatarUrl { get; set; }

    /// <summary>
    /// <para>应用描述</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("description")]
    public string? Description { get; set; }

    /// <summary>
    /// <para>该版本申请的权限列表</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("scopes")]
    public List<AppScope>? Scopes { get; set; }

    /// <summary>
    /// <para>应用管理后台链接</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("back_home_url")]
    public string? BackHomeUrl { get; set; }

    /// <summary>
    /// <para>应用多语言名称与描述列表</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("i18n")]
    public List<AppI18nInfo>? I18n { get; set; }

    /// <summary>
    /// <para>应用管理后台的应用分类信息</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("common_categories")]
    public List<string>? CommonCategories { get; set; }

    /// <summary>
    /// <para>该版本订阅的事件列表</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("events")]
    public List<string>? Events { get; set; }

    /// <summary>
    /// <para>版本状态。2：已上架；3：审核中；4：待审核；5：审核未通过；6：已下架；7：已取消</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("status")]
    public int? Status { get; set; }

    /// <summary>
    /// <para>版本创建时间（毫秒级时间戳）</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("create_time")]
    public string? CreateTime { get; set; }

    /// <summary>
    /// <para>版本发布时间（毫秒级时间戳）</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("publish_time")]
    public string? PublishTime { get; set; }

    /// <summary>
    /// <para>版本的应用能力配置</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("ability")]
    public AppVersionAbility? Ability { get; set; }

    /// <summary>
    /// <para>版本的发布备注（更新说明与可见范围）</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("remark")]
    public AppVersionRemark? Remark { get; set; }

    /// <summary>
    /// <para>版本订阅的事件详情列表</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("event_infos")]
    public List<AppVersionEventInfo>? EventInfos { get; set; }
}
