// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.EventCallback.Application;

/// <summary>
/// ApplicationAppVersionEvent
/// <para>当前线上版本信息</para>
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Application")]
public class ApplicationAppVersionEvent
{
    /// <summary>
    /// <para>应用 id</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("app_id")]
    public string? AppId { get; set; }

    /// <summary>
    /// <para>开发者填入的应用版本 ID</para>
    /// <para>**数据校验规则**：</para>
    /// <para>- 最小长度：`1` 字符</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("version")]
    public string? Version { get; set; }

    /// <summary>
    /// <para>唯一标识应用版本的 ID</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("version_id")]
    public string? VersionId { get; set; }

    /// <summary>
    /// <para>应用默认名称</para>
    /// <para>**数据校验规则**：</para>
    /// <para>- 最小长度：`1` 字符</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("app_name")]
    public string? AppName { get; set; }

    /// <summary>
    /// <para>应用头像 url</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("avatar_url")]
    public string? AvatarUrl { get; set; }

    /// <summary>
    /// <para>应用默认描述</para>
    /// <para>**数据校验规则**：</para>
    /// <para>- 最小长度：`1` 字符</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("description")]
    public string? Description { get; set; }

    /// <summary>
    /// <para>应用权限列表</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("scopes")]
    public ApplicationAppScope[]? Scopes { get; set; }

    /// <summary>
    /// <para>后台主页地址</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("back_home_url")]
    public string? BackHomeUrl { get; set; }

    /// <summary>
    /// <para>应用的国际化信息列表</para>
    /// <para>**数据校验规则**：</para>
    /// <para>- 最小长度：`1`</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("i18n")]
    public ApplicationAppI18nInfo[]? I18n { get; set; }

    /// <summary>
    /// <para>应用分类的国际化描述</para>
    /// <para>**数据校验规则**：</para>
    /// <para>- 最大长度：`3`</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("common_categories")]
    public string[]? CommonCategories { get; set; }

    /// <summary>
    /// <para>应用已订阅开放平台事件列表</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("events")]
    public string[]? Events { get; set; }

    /// <summary>
    /// <para>版本状态</para>
    /// <para>**可选值有**：</para>
    /// <para>- 0：未知状态</para>
    /// <para>- 1：审核通过</para>
    /// <para>- 2：审核拒绝</para>
    /// <para>- 3：审核中</para>
    /// <para>- 4：未提交审核</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("status")]
    public int? Status { get; set; }

    /// <summary>
    /// <para>版本创建时间（单位：s）</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("create_time")]
    public string? CreateTime { get; set; }

    /// <summary>
    /// <para>版本发布时间（单位：s）</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("publish_time")]
    public string? PublishTime { get; set; }

    /// <summary>
    /// <para>当前版本下应用开启的能力</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("ability")]
    public ApplicationAppAbility? Ability { get; set; }

    /// <summary>
    /// <para>跟随应用版本的信息</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("remark")]
    public ApplicationAppVersionRemarkEvent? Remark { get; set; }
}
