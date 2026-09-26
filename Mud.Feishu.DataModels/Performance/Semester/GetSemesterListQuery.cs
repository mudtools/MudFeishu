// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.HttpUtils;
using System.Globalization;

namespace Mud.Feishu.DataModels.Performance;

/// <summary>
/// <para>获取周期列表查询参数（查询对象模式，见 AGENTS.md API-2）</para>
/// </summary>
public class GetSemesterListQuery : IQueryParameter
{
    /// <summary>
    /// <para>周期开始时间最小值，毫秒时间戳，小于该时间开始的周期会被过滤掉</para>
    /// <para>必填：否</para>
    /// <para>示例值：1630425599999</para>
    /// </summary>
    public string? StartTime { get; set; }

    /// <summary>
    /// <para>周期结束时间最大值，毫秒时间戳，大于该时间结束的周期会被过滤掉</para>
    /// <para>必填：否</para>
    /// <para>示例值：1640425000000</para>
    /// </summary>
    public string? EndTime { get; set; }

    /// <summary>
    /// <para>周期年份，填写时按照周期年份筛选，取值范围 0 ～ 9999</para>
    /// <para>必填：否</para>
    /// <para>示例值：2024</para>
    /// </summary>
    public int? Year { get; set; }

    /// <summary>
    /// <para>周期类型分组：Annual（年）/ Semi-annual（半年）/ Quarter（季度）/ Bimonth（双月）/ Month（月）/ Non-standard（非标准周期）</para>
    /// <para>必填：否</para>
    /// <para>示例值：Annual</para>
    /// </summary>
    public string? TypeGroup { get; set; }

    /// <summary>
    /// <para>周期类型：Annual / H1 / H2 / Q1~Q4 / January-February / March-April / May-June / July-August / September-October / November-December / January~December / Custom</para>
    /// <para>必填：否</para>
    /// <para>示例值：April</para>
    /// </summary>
    public string? Type { get; set; }

    /// <summary>
    /// <para>用户 ID 类型：open_id/union_id/user_id，默认 open_id；取 user_id 时需字段权限 contact:user.employee_id:readonly</para>
    /// <para>必填：否</para>
    /// </summary>
    public string? UserIdType { get; set; }

    /// <summary>
    /// 将对象转换为查询参数键值对集合（仅输出已设置的参数）。
    /// </summary>
    /// <returns>包含查询参数的键值对集合。</returns>
    public IEnumerable<KeyValuePair<string, string?>> ToQueryParameters()
    {
        if (!string.IsNullOrEmpty(StartTime))
        {
            yield return new KeyValuePair<string, string?>("start_time", StartTime);
        }

        if (!string.IsNullOrEmpty(EndTime))
        {
            yield return new KeyValuePair<string, string?>("end_time", EndTime);
        }

        if (Year.HasValue)
        {
            yield return new KeyValuePair<string, string?>("year", Year.Value.ToString(CultureInfo.InvariantCulture));
        }

        if (!string.IsNullOrEmpty(TypeGroup))
        {
            yield return new KeyValuePair<string, string?>("type_group", TypeGroup);
        }

        if (!string.IsNullOrEmpty(Type))
        {
            yield return new KeyValuePair<string, string?>("type", Type);
        }

        if (!string.IsNullOrEmpty(UserIdType))
        {
            yield return new KeyValuePair<string, string?>("user_id_type", UserIdType);
        }
    }
}
