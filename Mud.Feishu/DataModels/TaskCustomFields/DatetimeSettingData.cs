// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.TaskCustomFields;

/// <summary>
/// <para>时间日期类型的字段设置</para>
/// </summary>
public class DatetimeSettingData
{
    /// <summary>
    /// <para>日期时间格式，支持</para>
    /// <para>默认为"yyyy-mm-dd"。</para>
    /// <para>注意本设置仅影响App中的时间日期类型字段的字段值的显示格式，并不会影响openapi输入/输出的字段值的格式。</para>
    /// <para>必填：否</para>
    /// <para>示例值：yyyy/mm/dd</para>
    /// <para>可选值：<list type="bullet">
    /// <item>yyyy-mm-dd：以短横分隔的年月日，例如2023-08-24</item>
    /// <item>yyyy/mm/dd：以斜杠分隔的年月日，例如2023/08/04</item>
    /// <item>mm/dd/yyyy：以斜杠分隔的月日年，例如08/24/2023</item>
    /// <item>dd/mm/yyyy：以斜杠分隔的日月年，例如24/08/2023</item>
    /// </list></para>
    /// </summary>
    [JsonPropertyName("format")]
    public string? Format { get; set; }
}
