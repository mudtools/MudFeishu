// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.CardMessageStream;


/// <summary>
/// <para>状态标签（非必填字段，如未选择该字段，则默认展示卡片触达时间）</para>
/// </summary>
public class OpenMessageStatusLabel
{
    /// <summary>
    /// <para>标签文字</para>
    /// <para>必填：是</para>
    /// <para>示例值：标签文字</para>
    /// </summary>
    [JsonPropertyName("text")]
    public string Text { get; set; } = string.Empty;

    /// <summary>
    /// <para>标签类型</para>
    /// <para>必填：是</para>
    /// <para>示例值：primary</para>
    /// <para>可选值：<list type="bullet">
    /// <item>primary：主类型</item>
    /// <item>secondary：次要类型</item>
    /// <item>success：成功类型</item>
    /// <item>danger：危险类型</item>
    /// </list></para>
    /// <para>默认值：primary</para>
    /// </summary>
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;
}