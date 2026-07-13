// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.HelpDesk;

/// <summary>
/// 创建服务台对话请求体
/// </summary>
public class StartServiceTicketRequest
{
    /// <summary>
    /// <para>是否直接进入人工(若appointed_agents填写了，该值为必填)</para>
    /// <para>必填：否</para>
    /// <para>示例值：false</para>
    /// </summary>
    [JsonPropertyName("human_service")]
    public bool? HumanService { get; set; }

    /// <summary>
    /// <para>客服 open ids (获取方式参考[获取单个用户信息](https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/reference/contact-v3/user/get))，human_service需要为true</para>
    /// <para>必填：否</para>
    /// <para>示例值：[ou_7dab8a3d3cdcc9da365777c7ad535d62]</para>
    /// </summary>
    [JsonPropertyName("appointed_agents")]
    public string[]? AppointedAgents { get; set; }

    /// <summary>
    /// <para>用户 open id,(获取方式参考[获取单个用户信息](https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/reference/contact-v3/user/get))</para>
    /// <para>必填：是</para>
    /// <para>示例值：ou_7dab8a3d3cdcc9da365777c7ad535d62</para>
    /// </summary>
    [JsonPropertyName("open_id")]
    public string OpenId { get; set; } = string.Empty;

    /// <summary>
    /// <para>工单来源自定义信息，长度限制1024字符，如设置，[获取工单详情](https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/helpdesk-v1/ticket/get)会返回此信息</para>
    /// <para>必填：否</para>
    /// <para>示例值：测试自定义字段信息</para>
    /// </summary>
    [JsonPropertyName("customized_info")]
    public string? CustomizedInfo { get; set; }
}
