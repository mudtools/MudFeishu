// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels;


/// <summary>
/// <para>邮箱地址以及其对应的类型和状态</para>
/// </summary>
public class UserMailboxStatus
{
    /// <summary>
    /// <para>邮箱地址</para>
    /// <para>必填：否</para>
    /// <para>示例值：aaa@lark.com</para>
    /// </summary>
    [JsonPropertyName("email")]
    public string? Email { get; set; }

    /// <summary>
    /// <para>邮箱地址状态</para>
    /// <para>必填：否</para>
    /// <para>示例值：4</para>
    /// <para>可选值：<list type="bullet">
    /// <item>1：邮箱地址格式错误</item>
    /// <item>2：邮箱地址域名不存在</item>
    /// <item>3：邮箱地址不存在</item>
    /// <item>4：启用</item>
    /// <item>5：已删除（邮箱回收站中）</item>
    /// <item>6：禁用</item>
    /// </list></para>
    /// </summary>
    [JsonPropertyName("status")]
    public int? Status { get; set; }

    /// <summary>
    /// <para>邮箱地址类型</para>
    /// <para>必填：否</para>
    /// <para>示例值：1</para>
    /// <para>可选值：<list type="bullet">
    /// <item>1：成员邮箱</item>
    /// <item>2：成员邮箱别名</item>
    /// <item>3：公共邮箱</item>
    /// <item>4：公共邮箱别名</item>
    /// <item>5：邮件组</item>
    /// <item>6：邮件组别名</item>
    /// </list></para>
    /// </summary>
    [JsonPropertyName("type")]
    public int? Type { get; set; }
}