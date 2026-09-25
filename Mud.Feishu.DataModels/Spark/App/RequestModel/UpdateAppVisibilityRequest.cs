// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Spark;

/// <summary>
/// 更新妙搭应用可用范围请求体
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Spark")]
public class UpdateAppVisibilityRequest
{
    /// <summary>
    /// <para>可见范围类型</para>
    /// <para>必填：是</para>
    /// <para>示例值：Range</para>
    /// <para>可选值：<list type="bullet">
    /// <item>Public：互联网公开可见</item>
    /// <item>Tenant：组织内可见</item>
    /// <item>Range：部分人员/部门/租户可见</item>
    /// </list></para>
    /// </summary>
    [JsonPropertyName("scope")]
    public string Scope { get; set; } = string.Empty;

    /// <summary>
    /// <para>授权用户 open_id 列表，仅 Scope = Range 时生效</para>
    /// <para>必填：否</para>
    /// <para>示例值：["ou_1234567890abcdef1234567890abcdef"]</para>
    /// </summary>
    [JsonPropertyName("users")]
    public string[]? Users { get; set; }

    /// <summary>
    /// <para>授权部门 department_id 列表，仅 Scope = Range 时生效，长度范围 1～200</para>
    /// <para>必填：否</para>
    /// <para>示例值：["od_1234567890abcdef1234567890abcdef"]</para>
    /// </summary>
    [JsonPropertyName("departments")]
    public string[]? Departments { get; set; }

    /// <summary>
    /// <para>授权群聊 chat_id 列表，仅 Scope = Range 时生效</para>
    /// <para>必填：否</para>
    /// <para>示例值：["oc_1234567890abcdef1234567890abcdef"]</para>
    /// </summary>
    [JsonPropertyName("chats")]
    public string[]? Chats { get; set; }

    /// <summary>
    /// <para>申请访问配置（含审批人，仅支持单个用户 open_id）</para>
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
