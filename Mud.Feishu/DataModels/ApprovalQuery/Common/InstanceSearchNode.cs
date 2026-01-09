// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.DataModels.ApprovalExternal;

namespace Mud.Feishu.DataModels.ApprovalQuery;

/// <summary>
/// <para>审批实例信息</para>
/// </summary>
public class InstanceSearchNode
{
    /// <summary>
    /// <para>审批实例 Code</para>
    /// <para>必填：否</para>
    /// <para>示例值：EB828003-9FFE-4B3F-AA50-2E199E2ED943</para>
    /// </summary>
    [JsonPropertyName("code")]
    public string? Code { get; set; }

    /// <summary>
    /// <para>审批实例的第三方 ID</para>
    /// <para>必填：否</para>
    /// <para>示例值：0004_3ED52DC1-AA6C</para>
    /// </summary>
    [JsonPropertyName("external_id")]
    public string? ExternalId { get; set; }

    /// <summary>
    /// <para>审批实例发起人的 user_id</para>
    /// <para>必填：否</para>
    /// <para>示例值：lwiu098wj</para>
    /// </summary>
    [JsonPropertyName("user_id")]
    public string? UserId { get; set; }

    /// <summary>
    /// <para>审批实例开始时间，Unix 毫秒时间戳</para>
    /// <para>必填：否</para>
    /// <para>示例值：1547654251506</para>
    /// </summary>
    [JsonPropertyName("start_time")]
    public string? StartTime { get; set; }

    /// <summary>
    /// <para>审批实例结束时间，Unix 毫秒时间戳</para>
    /// <para>必填：否</para>
    /// <para>示例值：1547654251506</para>
    /// </summary>
    [JsonPropertyName("end_time")]
    public string? EndTime { get; set; }

    /// <summary>
    /// <para>审批实例状态</para>
    /// <para>必填：否</para>
    /// <para>示例值：pending</para>
    /// <para>可选值：<list type="bullet">
    /// <item>rejected：已拒绝</item>
    /// <item>pending：审批中</item>
    /// <item>canceled：已撤回</item>
    /// <item>deleted：已删除</item>
    /// <item>approved：已通过</item>
    /// </list></para>
    /// </summary>
    [JsonPropertyName("status")]
    public string? Status { get; set; }

    /// <summary>
    /// <para>审批实例名称（只有第三方审批有返回值）</para>
    /// <para>必填：否</para>
    /// <para>示例值：test</para>
    /// </summary>
    [JsonPropertyName("title")]
    public string? Title { get; set; }

    /// <summary>
    /// <para>审批实例扩展字段，字符串类型的 JSON 数据</para>
    /// <para>必填：否</para>
    /// <para>示例值：{}</para>
    /// </summary>
    [JsonPropertyName("extra")]
    public string? Extra { get; set; }

    /// <summary>
    /// <para>审批流水号</para>
    /// <para>必填：否</para>
    /// <para>示例值：201902020001</para>
    /// </summary>
    [JsonPropertyName("serial_id")]
    public string? SerialId { get; set; }

    /// <summary>
    /// <para>审批实例链接（只有第三方审批有返回值）</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("link")]
    public InstanceLink? Link { get; set; }

}