// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Bitable;

/// <summary>
/// 升级表单请求体
/// </summary>
public class UpgradeFormRequest
{
    /// <summary>
    /// <para>升级后的表单名称</para>
    /// <para>必填：是</para>
    /// <para>示例值：文档问题反馈</para>
    /// <para>最大长度：100</para>
    /// <para>最小长度：1</para>
    /// </summary>
    [JsonPropertyName("form_name")]
    public string FormName { get; set; } = string.Empty;

    /// <summary>
    /// <para>表单布局模式。</para>
    /// <para>必填：是</para>
    /// <para>示例值：one_question_per_page</para>
    /// <para>可选值：<list type="bullet">
    /// <item>traditional：传统布局</item>
    /// <item>one_question_per_page：一页一题布局</item>
    /// </list></para>
    /// </summary>
    [JsonPropertyName("display_mode")]
    public string DisplayMode { get; set; } = string.Empty;
}