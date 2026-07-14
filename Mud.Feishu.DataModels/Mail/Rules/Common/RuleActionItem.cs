// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Mail;

/// <summary>
/// <para>匹配命中规则后的操作列表</para>
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Mail")]
public class RuleActionItem
{
    /// <summary>
    /// <para>操作类型</para>
    /// <para>必填：是</para>
    /// <para>示例值：1</para>
    /// <para>最大值：13</para>
    /// <para>最小值：1</para>
    /// <para>可选值：<list type="bullet">
    /// <item>1：归档</item>
    /// <item>2：删除邮件</item>
    /// <item>3：标记为已读</item>
    /// <item>4：移至垃圾邮件</item>
    /// <item>5：不移至垃圾邮件</item>
    /// <item>8：添加用户标签（暂不支持）</item>
    /// <item>9：添加旗标</item>
    /// <item>10：不弹出通知</item>
    /// <item>11：移至用户文件夹</item>
    /// <item>12：自动转发（暂不支持）</item>
    /// <item>13：分享到会话（暂不支持）</item>
    /// </list></para>
    /// </summary>
    [JsonPropertyName("type")]
    public int Type { get; set; }

    /// <summary>
    /// <para>当 type 为移动到文件夹时，该字段填文件夹的 id</para>
    /// <para>必填：否</para>
    /// <para>示例值：283412371233</para>
    /// </summary>
    [JsonPropertyName("input")]
    public string? Input { get; set; }
}
