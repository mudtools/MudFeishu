// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.HelpDesk;

/// <summary>
/// 发送工单消息请求体
/// </summary>
[HttpJsonSerializable(SerializerClassName = "HelpDesk")]
public class CreateTicketMessageRequest
{
    /// <summary>
    /// <para>消息类型；text：纯文本；post：富文本</para>
    /// <para>必填：是</para>
    /// <para>示例值：post</para>
    /// </summary>
    [JsonPropertyName("msg_type")]
    public string MsgType { get; set; } = string.Empty;

    /// <summary>
    /// <para>- 纯文本，参考[发送文本消息](https://open.feishu.cn/document/ukTMukTMukTM/uUjNz4SN2MjL1YzM)中的content；</para>
    /// <para>- 富文本，参考[发送富文本消息](https://open.feishu.cn/document/ukTMukTMukTM/uMDMxEjLzATMx4yMwETM)中的content</para>
    /// <para>必填：是</para>
    /// <para>示例值：{ "msg_type": "post", "content": { "post": { "zh_cn": { "title": "this is title", "content": [ [ { "tag": "text", "un_escape": true, "text": "第一行 :" }, { "tag": "a", "text": "超链接", "href": "http://www.feishu.cn" } ], [ { "tag": "text", "text": "第二行 :" }, { "tag": "text", "text": "文本测试" } ] ] } } } }</para>
    /// </summary>
    [JsonPropertyName("content")]
    public string? Content { get; set; }
}
