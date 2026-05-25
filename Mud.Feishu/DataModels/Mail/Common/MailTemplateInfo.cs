// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Mail;


/// <summary>
/// <para>模板详情</para>
/// </summary>
public class MailTemplateInfo : MailTemplate
{
    /// <summary>
    /// <para>模板 id</para>
    /// <para>必填：否</para>
    /// <para>示例值：7281187859195772947</para>
    /// </summary>
    [JsonPropertyName("template_id")]
    public string? TemplateId { get; set; }


    /// <summary>
    /// <para>模板创建时间（毫秒级时间戳字符串，避免 JS 弱类型侧 i64 精度丢失）</para>
    /// <para>必填：否</para>
    /// <para>示例值：1716279320000</para>
    /// </summary>
    [JsonPropertyName("create_time")]
    public string? CreateTime { get; set; }
}