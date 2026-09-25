// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Spark;

/// <summary>
/// 获取妙搭应用可用范围响应体
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Spark")]
public class GetAppVisibilityResult
{
    /// <summary>
    /// <para>当前可见范围类型</para>
    /// <para>必填：否</para>
    /// <para>示例值：Range</para>
    /// <para>可选值：All、Tenant、Range</para>
    /// </summary>
    [JsonPropertyName("scope")]
    public string? Scope { get; set; }

    /// <summary>
    /// <para>授权用户 open_id 列表，仅 Scope=Range 时返回</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("users")]
    public string[]? Users { get; set; }

    /// <summary>
    /// <para>授权部门 department_id 列表，仅 Scope=Range 时返回</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("departments")]
    public string[]? Departments { get; set; }

    /// <summary>
    /// <para>授权群聊 chat_id 列表，仅 Scope=Range 时返回</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("chats")]
    public string[]? Chats { get; set; }

    /// <summary>
    /// <para>当前申请访问配置（含审批人）；未启用申请时为空</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("apply_config")]
    public ApplyConfig? ApplyConfig { get; set; }

    /// <summary>
    /// <para>访问 Share URL 是否需要登录</para>
    /// <para>必填：否</para>
    /// <para>示例值：true</para>
    /// </summary>
    [JsonPropertyName("require_login")]
    public bool? RequireLogin { get; set; }
}
