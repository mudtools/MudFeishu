// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.HelpDesk;

/// <summary>
/// 更新工单详情请求体
/// </summary>
[HttpJsonSerializable(SerializerClassName = "HelpDesk")]
public class UpdateTicketRequest
{
    /// <summary>
    /// <para>工单新status，status对应具体的含义如下：1: 待响应, 2: 处理中, 3: 排队中, 4: 待定, 5: 待用户响应, 50: 机器人关闭工单, 51: 人工关闭工单</para>
    /// <para>必填：否</para>
    /// <para>示例值：1</para>
    /// </summary>
    [JsonPropertyName("status")]
    public int? Status { get; set; }

    /// <summary>
    /// <para>新标签名</para>
    /// <para>必填：否</para>
    /// <para>示例值：abc</para>
    /// </summary>
    [JsonPropertyName("tag_names")]
    public string[]? TagNames { get; set; }

    /// <summary>
    /// <para>新评论</para>
    /// <para>必填：否</para>
    /// <para>示例值：good</para>
    /// </summary>
    [JsonPropertyName("comment")]
    public string? Comment { get; set; }

    /// <summary>
    /// <para>自定义字段</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("customized_fields")]
    public CustomizedFieldPutDisplayItem[]? CustomizedFields { get; set; }


    /// <summary>
    /// <para>ticket stage</para>
    /// <para>必填：否</para>
    /// <para>示例值：1</para>
    /// </summary>
    [JsonPropertyName("ticket_type")]
    public int? TicketType { get; set; }

    /// <summary>
    /// <para>工单是否解决，1: 未解决, 2: 已解决</para>
    /// <para>必填：否</para>
    /// <para>示例值：1</para>
    /// </summary>
    [JsonPropertyName("solved")]
    public int? Solved { get; set; }

    /// <summary>
    /// <para>工单来源渠道ID</para>
    /// <para>必填：否</para>
    /// <para>示例值：1</para>
    /// </summary>
    [JsonPropertyName("channel")]
    public int? Channel { get; set; }
}
