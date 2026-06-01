// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Mail;

/// <summary>
/// <para>匹配规则列表</para>
/// </summary>
public class RuleConditionItem
{
    /// <summary>
    /// <para>匹配条件左值</para>
    /// <para>必填：是</para>
    /// <para>示例值：1</para>
    /// <para>最大值：16</para>
    /// <para>最小值：1</para>
    /// <para>可选值：<list type="bullet">
    /// <item>1：发件人地址</item>
    /// <item>2：收件人地址</item>
    /// <item>3：抄送地址</item>
    /// <item>4：收件人或抄送地址</item>
    /// <item>6：主题</item>
    /// <item>7：正文</item>
    /// <item>8：附件名字</item>
    /// <item>9：附件类型</item>
    /// <item>10：任意地址</item>
    /// <item>12：所有邮件</item>
    /// <item>13：是外部邮件</item>
    /// <item>14：是垃圾邮件</item>
    /// <item>15：不是垃圾邮件</item>
    /// <item>16：有附件</item>
    /// </list></para>
    /// </summary>
    [JsonPropertyName("type")]
    public int Type { get; set; }

    /// <summary>
    /// <para>匹配条件操作符</para>
    /// <para>必填：否</para>
    /// <para>示例值：1</para>
    /// <para>最大值：10</para>
    /// <para>最小值：1</para>
    /// <para>可选值：<list type="bullet">
    /// <item>1：包含</item>
    /// <item>2：不包含</item>
    /// <item>3：开头是</item>
    /// <item>4：结尾是</item>
    /// <item>5：是</item>
    /// <item>6：不是</item>
    /// <item>7：包含自己</item>
    /// <item>10：为空</item>
    /// </list></para>
    /// </summary>
    [JsonPropertyName("operator")]
    public int? Operator { get; set; }

    /// <summary>
    /// <para>匹配条件右值</para>
    /// <para>必填：否</para>
    /// <para>示例值：hello@world.com</para>
    /// </summary>
    [JsonPropertyName("input")]
    public string? Input { get; set; }
}
