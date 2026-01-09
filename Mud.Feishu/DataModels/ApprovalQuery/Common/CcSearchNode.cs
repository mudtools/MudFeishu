// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.DataModels.ApprovalExternal;

namespace Mud.Feishu.DataModels.ApprovalQuery;


/// <summary>
/// <para>审批抄送信息</para>
/// </summary>
public class CcSearchNode
{
    /// <summary>
    /// <para>审批抄送发起人的 user_id</para>
    /// <para>必填：否</para>
    /// <para>示例值：lwiu098wj</para>
    /// </summary>
    [JsonPropertyName("user_id")]
    public string? UserId { get; set; }

    /// <summary>
    /// <para>审批抄送开始时间，Unix 毫秒时间戳</para>
    /// <para>必填：否</para>
    /// <para>示例值：1547654251506</para>
    /// </summary>
    [JsonPropertyName("create_time")]
    public string? CreateTime { get; set; }

    /// <summary>
    /// <para>审批抄送状态</para>
    /// <para>必填：否</para>
    /// <para>示例值：read</para>
    /// <para>可选值：<list type="bullet">
    /// <item>read：已读</item>
    /// <item>unread：未读</item>
    /// </list></para>
    /// </summary>
    [JsonPropertyName("read_status")]
    public string? ReadStatus { get; set; }

    /// <summary>
    /// <para>审批抄送名称（只有第三方审批有返回值）</para>
    /// <para>必填：否</para>
    /// <para>示例值：test</para>
    /// </summary>
    [JsonPropertyName("title")]
    public string? Title { get; set; }

    /// <summary>
    /// <para>审批抄送扩展字段，字符串类型的 JSON 数据</para>
    /// <para>必填：否</para>
    /// <para>示例值：{}</para>
    /// </summary>
    [JsonPropertyName("extra")]
    public string? Extra { get; set; }

    /// <summary>
    /// <para>审批抄送链接（只有第三方审批有返回值）</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("link")]
    public InstanceLink? Link { get; set; }

}