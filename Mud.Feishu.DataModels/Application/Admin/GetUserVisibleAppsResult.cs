// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Application;

/// <summary>
/// 获取用户可用的应用响应体数据。
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Application")]
public class GetUserVisibleAppsResult
{
    /// <summary>
    /// <para>下一页分页起始位置</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("page_token")]
    public string? PageToken { get; set; }

    /// <summary>
    /// <para>本次请求实际返回的页大小</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("page_size")]
    public int? PageSize { get; set; }

    /// <summary>
    /// <para>可用应用总数</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("total_count")]
    public int? TotalCount { get; set; }

    /// <summary>
    /// <para>是否还有更多应用。1：是；0：否</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("has_more")]
    public int? HasMore { get; set; }

    /// <summary>
    /// <para>当前应用信息展示的语言版本</para>
    /// <para>必填：否</para>
    /// <para>示例值：zh_cn</para>
    /// </summary>
    [JsonPropertyName("lang")]
    public string? Lang { get; set; }

    /// <summary>
    /// <para>用户可用的应用列表</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("app_list")]
    public List<UserVisibleApp>? AppList { get; set; }
}

/// <summary>
/// 用户可用的应用信息。
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Application")]
public class UserVisibleApp
{
    /// <summary>
    /// <para>应用 App ID</para>
    /// <para>必填：是</para>
    /// <para>示例值：cli_9cb844403dbb9108</para>
    /// </summary>
    [JsonPropertyName("app_id")]
    public string? AppId { get; set; }

    /// <summary>
    /// <para>应用主语言</para>
    /// <para>必填：否</para>
    /// <para>示例值：zh_cn</para>
    /// </summary>
    [JsonPropertyName("primary_language")]
    public string? PrimaryLanguage { get; set; }

    /// <summary>
    /// <para>应用名称</para>
    /// <para>必填：否</para>
    /// <para>示例值：审批</para>
    /// </summary>
    [JsonPropertyName("app_name")]
    public string? AppName { get; set; }

    /// <summary>
    /// <para>应用描述</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("description")]
    public string? Description { get; set; }

    /// <summary>
    /// <para>应用图标链接</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("avatar_url")]
    public string? AvatarUrl { get; set; }

    /// <summary>
    /// <para>应用类型。0：自建应用；1：商店应用</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("app_scene_type")]
    public int? AppSceneType { get; set; }

    /// <summary>
    /// <para>应用状态。0：未启用；1：已启用</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("status")]
    public int? Status { get; set; }

    /// <summary>
    /// <para>移动端默认应用能力。0：未开启；1：小程序；2：H5；8：机器人</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("mobile_default_ability")]
    public int? MobileDefaultAbility { get; set; }

    /// <summary>
    /// <para>桌面端默认应用能力。0：未开启；1：小程序；2：H5；8：机器人</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("pc_default_ability")]
    public int? PcDefaultAbility { get; set; }

    /// <summary>
    /// <para>应用创建来源。可选值：developer_console（开发者后台）、base（Base）、app_engine（飞书应用引擎）、bot_builder（机器人助手）、aily（Aily）、unknown（未知来源）</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("create_source")]
    public string? CreateSource { get; set; }
}
