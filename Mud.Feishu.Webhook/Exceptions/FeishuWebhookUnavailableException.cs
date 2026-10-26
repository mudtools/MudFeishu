// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.Webhook.Exceptions;

/// <summary>
/// 验签链路"无法完成判定"（密钥不可用 / 内部故障 / 配置构建失败）。
/// 语义 = <b>本请求未被消费</b>，请对端稍后重推（映射 HTTP 503 + <c>Retry-After</c>）。
/// </summary>
/// <remarks>
/// <para>
/// <b>与 <see cref="FeishuWebhookException"/> 家族的"签名不匹配"严格区分</b>：
/// 签名不匹配属"已判定为无效"，是终态（403，对端不再重推、事件被丢弃）；
/// 本异常属"无法判定"，是可重试的基础设施故障（503）。
/// </para>
/// <para>
/// 为什么用异常而不是三态返回值：同一模块内 <c>OperationCanceledException</c>（客户端断开/超时，
/// 要求"不写响应"）与 <c>FeishuRedisException</c>（携带 <c>FailureKind</c> 分类）必须以异常传播，
/// 再引入三态枚举会形成"枚举 + 异常"两套表达同一语义的机制。
/// </para>
/// <para>
/// 抛出点仅三处（叶子 wrap、外层 filter 放行，禁止双重包裹）：
/// <c>FeishuWebhookService.HandleEventAsync</c>、<c>CompositeFeishuEventValidator.ValidateHeaderSignatureAsync</c>、
/// <c>SignatureValidator.ValidateHeaderSignatureAsync</c>。
/// </para>
/// </remarks>
public class FeishuWebhookUnavailableException : FeishuWebhookException
{
    /// <summary>
    /// 初始化 <see cref="FeishuWebhookUnavailableException"/> 的新实例
    /// </summary>
    /// <param name="message">失败原因（仅服务端日志可见，不写入响应体）</param>
    public FeishuWebhookUnavailableException(string message)
        : base("ServiceUnavailable", message, requestId: null, isRetryable: true)
    {
    }

    /// <summary>
    /// 初始化 <see cref="FeishuWebhookUnavailableException"/> 的新实例
    /// </summary>
    /// <param name="message">失败原因（仅服务端日志可见，不写入响应体）</param>
    /// <param name="innerException">原始异常（保留堆栈以便归因）</param>
    public FeishuWebhookUnavailableException(string message, Exception innerException)
        : base("ServiceUnavailable", message, innerException, requestId: null, isRetryable: true)
    {
    }
}
