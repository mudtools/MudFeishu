// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.ApprovalExternal;

/// <summary>
/// <para>托管预缓存策略。</para>
/// </summary>
public record ExternalTrusteeshipInstanceCacheConfig
{
    /// <summary>
    /// <para>托管预缓存策略。</para>
    /// <para>必填：否</para>
    /// <para>示例值：DISABLE</para>
    /// <para>可选值：<list type="bullet">
    /// <item>DISABLE：不启用，默认</item>
    /// <item>IMMUTABLE：表单不会随流程进行改变</item>
    /// <item>BY_NODE：跟随流程节点变更更新缓存</item>
    /// <item>BY_USER：对于每个待办任务存储一份</item>
    /// </list></para>
    /// </summary>
    [JsonPropertyName("form_policy")]
    public string? FormPolicy { get; set; }

    /// <summary>
    /// <para>表单是否随国际化改变。</para>
    /// <para>必填：否</para>
    /// <para>示例值：false</para>
    /// </summary>
    [JsonPropertyName("form_vary_with_locale")]
    public bool? FormVaryWithLocale { get; set; }

    /// <summary>
    /// <para>当前使用的表单版本号，保证表单改变后，版本号增加，实际值为 int64 整数。</para>
    /// <para>必填：否</para>
    /// <para>示例值：1</para>
    /// </summary>
    [JsonPropertyName("form_version")]
    public string? FormVersion { get; set; }
}