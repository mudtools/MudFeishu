// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Performance;

/// <summary>
/// 获取周期任务（全部用户）请求体
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Performance")]
public class FindStageTaskByPageRequest
{
    /// <summary>
    /// <para>周期 ID，可通过获取周期接口获取</para>
    /// <para>必填：是</para>
    /// <para>示例值：7235911950407352321</para>
    /// </summary>
    [JsonPropertyName("semester_id")]
    public string? SemesterId { get; set; }

    /// <summary>
    /// <para>任务分类，填写则获取指定分类的任务：1（待完成）/ 2（已完成）/ 3（已逾期，仅当租户设置不允许逾期提交时才有此分类），最大长度 3</para>
    /// <para>必填：否</para>
    /// <para>示例值：[1, 2, 3]</para>
    /// </summary>
    [JsonPropertyName("task_option_lists")]
    public int[]? TaskOptionLists { get; set; }

    /// <summary>
    /// <para>任务截止时间最小值，毫秒时间戳，填写则查询在此时间之后截止的任务</para>
    /// <para>必填：否</para>
    /// <para>示例值：1717142243000</para>
    /// </summary>
    [JsonPropertyName("after_time")]
    public string? AfterTime { get; set; }

    /// <summary>
    /// <para>任务截止时间最大值，毫秒时间戳，填写则查询在此时间之前截止的任务</para>
    /// <para>必填：否</para>
    /// <para>示例值：1717142245000</para>
    /// </summary>
    [JsonPropertyName("before_time")]
    public string? BeforeTime { get; set; }

    /// <summary>
    /// <para>分页标记，第一次请求不填，表示从头开始遍历；分页查询结果还有更多项时会同时返回新的 page_token，下次遍历可采用该 page_token 获取查询结果</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("page_token")]
    public string? PageToken { get; set; }

    /// <summary>
    /// <para>分页大小，默认 20，最大 50</para>
    /// <para>必填：否</para>
    /// <para>示例值：30</para>
    /// </summary>
    [JsonPropertyName("page_size")]
    public int? PageSize { get; set; }
}
