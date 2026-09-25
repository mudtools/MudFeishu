// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.TrustParty;

/// <summary>
/// 关联组织（directory/v1 体系：管理员视角，名称为 i18n_text 结构）
/// </summary>
[HttpJsonSerializable(SerializerClassName = "TrustParty")]
public class DirectoryCollaborationTenant
{
    /// <summary>
    /// 关联组织的 tenant key
    /// </summary>
    [JsonPropertyName("tenant_key")]
    public string? TenantKey { get; set; }

    /// <summary>
    /// 建联时间（秒级时间戳）
    /// </summary>
    [JsonPropertyName("connect_time")]
    public int? ConnectTime { get; set; }

    /// <summary>
    /// 组织头像信息
    /// </summary>
    [JsonPropertyName("avatar")]
    public AvatarInfo? Avatar { get; set; }

    /// <summary>
    /// 组织品牌（如 feishu）
    /// </summary>
    [JsonPropertyName("brand")]
    public string? Brand { get; set; }

    /// <summary>
    /// 组织名称（国际化文本）
    /// </summary>
    [JsonPropertyName("name")]
    public I18nContent? Name { get; set; }

    /// <summary>
    /// 组织简称（国际化文本）
    /// </summary>
    [JsonPropertyName("short_name")]
    public I18nContent? ShortName { get; set; }
}
