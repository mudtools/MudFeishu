// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Calendar;

/// <summary>
/// 日历基础数据
/// </summary>
public class CalendarBaseData
{
    /// <summary>
    /// <para>日历标题。</para>
    /// <para>**默认值**：空</para>
    /// <para>必填：否</para>
    /// <para>示例值：测试日历</para>
    /// <para>最大长度：255</para>
    /// </summary>
    [JsonPropertyName("summary")]
    public string? Summary { get; set; }

    /// <summary>
    /// <para>日历描述。</para>
    /// <para>**默认值**：空</para>
    /// <para>必填：否</para>
    /// <para>示例值：使用开放接口创建日历</para>
    /// <para>最大长度：255</para>
    /// </summary>
    [JsonPropertyName("description")]
    public string? Description { get; set; }

    /// <summary>
    /// <para>日历公开范围。</para>
    /// <para>**默认值**：show_only_free_busy</para>
    /// <para>必填：否</para>
    /// <para>示例值：private</para>
    /// <para>可选值：<list type="bullet">
    /// <item>private：私密</item>
    /// <item>show_only_free_busy：仅展示忙闲信息</item>
    /// <item>public：公开，他人可查看日程详情</item>
    /// </list></para>
    /// </summary>
    [JsonPropertyName("permissions")]
    public string? Permissions { get; set; }

    /// <summary>
    /// <para>日历颜色，取值通过颜色 RGB 值的 int32 表示，其中，24 ~ 31 位为透明度，16 ~ 23 位为红，8 ~ 15 位为绿，0 ~ 7 位为蓝。例如，-11034625 表示 RGB 值 (87, 159, 255)。</para>
    /// <para>**注意**：</para>
    /// <para>- 取值范围为 -2^31 ~ 2^31-1</para>
    /// <para>- 日历颜色会映射到飞书客户端色板上最接近的一种颜色进行展示。</para>
    /// <para>- 该颜色仅对当前身份生效。</para>
    /// <para>**默认值**：-14513409</para>
    /// <para>必填：否</para>
    /// <para>示例值：-1</para>
    /// </summary>
    [JsonPropertyName("color")]
    public int? Color { get; set; }

    /// <summary>
    /// <para>日历备注名，设置该字段后（包括后续修改该字段）仅对当前身份生效。</para>
    /// <para>**默认值**：空</para>
    /// <para>必填：否</para>
    /// <para>示例值：日历备注名</para>
    /// <para>最大长度：255</para>
    /// </summary>
    [JsonPropertyName("summary_alias")]
    public string? SummaryAlias { get; set; }
}