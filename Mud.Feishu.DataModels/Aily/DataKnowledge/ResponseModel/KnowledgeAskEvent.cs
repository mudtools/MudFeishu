// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Aily;

/// <summary>
/// <para>执行数据知识问答 SSE 事件数据（data 行的 JSON 负载）</para>
/// <para>接口以 Server-sent Events（SSE）方式推送，调用方需从响应流中解析 <c>data:</c> 行后使用本类型反序列化。</para>
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Aily")]
public class KnowledgeAskEvent
{
    /// <summary>
    /// <para>响应状态</para>
    /// <para>必填：否</para>
    /// <para>可选值：<list type="bullet">
    /// <item>processing：当前知识问答正在处理中</item>
    /// <item>finished：当前知识问答处理完成</item>
    /// </list></para>
    /// </summary>
    [JsonPropertyName("status")]
    public string? Status { get; set; }

    /// <summary>
    /// <para>结束类型</para>
    /// <para>必填：否</para>
    /// <para>可选值：<list type="bullet">
    /// <item>qa：执行数据知识问答</item>
    /// <item>faq：执行标准问答对</item>
    /// </list></para>
    /// </summary>
    [JsonPropertyName("finish_type")]
    public string? FinishType { get; set; }

    /// <summary>
    /// <para>响应消息</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("message")]
    public AskDataKnowledgeMessage? Message { get; set; }

    /// <summary>
    /// <para>知识问答运行过程结构化数据，status=finished 且 finish_type=qa 时返回</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("process_data")]
    public KnowledgeAskProcessData? ProcessData { get; set; }

    /// <summary>
    /// <para>匹配标准问答对结果，status=finished 且 finish_type=faq 时返回</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("faq_result")]
    public KnowledgeFaqResult? FaqResult { get; set; }

    /// <summary>
    /// <para>是否有结果，true 则代表 message 中的内容是通过配置知识而生成的</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("has_answer")]
    public bool HasAnswer { get; set; }
}
