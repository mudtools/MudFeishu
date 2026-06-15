// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.AI;

/// <summary>
/// <para>识别的实体列表</para>
/// </summary>
public class IdEntity
{
    /// <summary>
    /// <para>识别的字段种类</para>
    /// <para>必填：否</para>
    /// <para>示例值：identity_name</para>
    /// <para>可选值：<list type="bullet">
    /// <item>identity_code：公民身份号码</item>
    /// <item>identity_name：姓名</item>
    /// <item>address：住址</item>
    /// <item>valid_date_start：有效期起始时间</item>
    /// <item>valid_date_end：有效期终止时间（“长期”识别为“长期”）</item>
    /// <item>gender：性别</item>
    /// <item>race：民族</item>
    /// <item>issued_by：签发机关</item>
    /// <item>birth：出生日期</item>
    /// </list></para>
    /// </summary>
    [JsonPropertyName("type")]
    public string? Type { get; set; }

    /// <summary>
    /// <para>识别出字段的文本信息</para>
    /// <para>必填：否</para>
    /// <para>示例值：张三</para>
    /// </summary>
    [JsonPropertyName("value")]
    public string? Value { get; set; }
}