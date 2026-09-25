// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Spark;

/// <summary>
/// 妙搭应用信息
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Spark")]
public class App
{
    /// <summary>
    /// <para>应用唯一标识，系统自动生成，用于在接口中定位具体应用。可通过应用创建接口或开发者后台获取</para>
    /// <para>必填：否</para>
    /// <para>示例值：app_7d2f8a4b1c9e6035</para>
    /// </summary>
    [JsonPropertyName("app_id")]
    public string? AppId { get; set; }

    /// <summary>
    /// <para>应用类型</para>
    /// <para>必填：否</para>
    /// <para>示例值：HTML</para>
    /// </summary>
    [JsonPropertyName("app_type")]
    public string? AppType { get; set; }

    /// <summary>
    /// <para>应用名称，用于在管理后台和前端展示，支持中英文，长度不超过 64 字符</para>
    /// <para>必填：否</para>
    /// <para>示例值：智能客服助手</para>
    /// </summary>
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    /// <summary>
    /// <para>应用功能说明，用于向用户介绍应用核心能力，长度不超过 200 字符</para>
    /// <para>必填：否</para>
    /// <para>示例值：提供7×24小时智能对话服务，支持常见问题自动解答与工单流转</para>
    /// </summary>
    [JsonPropertyName("description")]
    public string? Description { get; set; }

    /// <summary>
    /// <para>应用图标访问地址，支持 PNG/JPG 格式，建议尺寸 128×128 像素</para>
    /// <para>必填：否</para>
    /// <para>示例值：https://example.com/app-icons/customer-service.png</para>
    /// </summary>
    [JsonPropertyName("icon_url")]
    public string? IconUrl { get; set; }

    /// <summary>
    /// <para>应用创建时间，遵循 ISO 8601 UTC 格式，由系统自动生成</para>
    /// <para>必填：否</para>
    /// <para>示例值：2026-05-18T10:00:00Z</para>
    /// </summary>
    [JsonPropertyName("created_at")]
    public string? CreatedAt { get; set; }

    /// <summary>
    /// <para>应用最后更新时间，遵循 ISO 8601 UTC 格式，应用信息变更时自动更新</para>
    /// <para>必填：否</para>
    /// <para>示例值：2026-06-20T14:30:00Z</para>
    /// </summary>
    [JsonPropertyName("updated_at")]
    public string? UpdatedAt { get; set; }

    /// <summary>
    /// <para>应用是否已发布</para>
    /// <para>必填：否</para>
    /// <para>示例值：true</para>
    /// </summary>
    [JsonPropertyName("is_published")]
    public bool? IsPublished { get; set; }

    /// <summary>
    /// <para>应用发布后的访问地址</para>
    /// <para>必填：否</para>
    /// <para>示例值：http://www.tos.dlxka.com</para>
    /// </summary>
    [JsonPropertyName("online_url")]
    public string? OnlineUrl { get; set; }

    /// <summary>
    /// <para>应用对应的文档 token</para>
    /// <para>必填：否</para>
    /// <para>示例值：wfjgyiyN2Fk17H8cgfFanWe</para>
    /// </summary>
    [JsonPropertyName("meta_token")]
    public string? MetaToken { get; set; }

    /// <summary>
    /// <para>应用启用状态</para>
    /// <para>必填：否</para>
    /// <para>示例值：enabled</para>
    /// <para>可选值：enabled（启用）、disabled（停用）</para>
    /// </summary>
    [JsonPropertyName("status")]
    public string? Status { get; set; }

    /// <summary>
    /// <para>应用所有者 UserID</para>
    /// <para>必填：否</para>
    /// <para>示例值：7615135553960283664</para>
    /// </summary>
    [JsonPropertyName("owner_id")]
    public string? OwnerId { get; set; }

    /// <summary>
    /// <para>应用创建者 UserID</para>
    /// <para>必填：否</para>
    /// <para>示例值：7615135553960283664</para>
    /// </summary>
    [JsonPropertyName("creator_id")]
    public string? CreatorId { get; set; }

    /// <summary>
    /// <para>应用管理员 UserID 列表</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("admin_ids")]
    public string[]? AdminIds { get; set; }

    /// <summary>
    /// <para>应用开发者 UserID 列表</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("developer_ids")]
    public string[]? DeveloperIds { get; set; }
}
