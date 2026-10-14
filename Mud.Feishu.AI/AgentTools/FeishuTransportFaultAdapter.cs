// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.AI.Abstractions.Tools.Errors;

namespace Mud.Feishu.AI.AgentTools;

/// <summary>
/// 把飞书传输层异常（<c>Mud.HttpUtils.ApiException</c>）适配为产品中立端口
/// <see cref="ITransportFault"/> 的防腐层。
/// </summary>
/// <remarks>
/// <para>
/// <b>为什么由本层承担（而不是让中立层判定类型）</b>：错误分类器住在产品中立的
/// <c>Mud.AI.Agent</c>，独立产品线纪律要求它<b>不得认识任何具体 HTTP 组件的异常类型</b>
/// （v2.3 P1-D 曾直接判定 <c>ApiException</c>，已下线）。"哪个异常携带状态码"是产品事实，
/// 归属产品层；中立层只接收中立事实（<see cref="ITransportFault.StatusCode"/>）。
/// </para>
/// <para>
/// 本类型是 <c>internal</c>：它是实现细节，宿主看不到；执行链
/// （<c>Mud.Feishu.AI.Tools.FeishuToolBinding</c>）在分类前调用
/// <see cref="Adapt"/> 把异常桥到端口。
/// </para>
/// <para>
/// <b>漏接的后果</b>：传输异常会落到中立分类器的 <see cref="System.Net.Http.HttpRequestException"/>
/// 兜底分支（<c>retryable/transient</c>）——403 会被误判为"可重试"。故该接线由消费方的
/// 端到端用例锁定（见飞书侧 <c>ToolErrorContractGuards</c> 的传输异常分类断言）。
/// </para>
/// </remarks>
internal static class FeishuTransportFaultAdapter
{
    /// <summary>
    /// 把异常适配为中立传输故障端口。
    /// </summary>
    /// <param name="exception">被分类的异常。</param>
    /// <returns>
    /// 该异常携带传输层语义时返回端口实现；否则返回 <see langword="null"/>
    /// （表示"不携带传输层事实"，交由中立分类器按 BCL 异常类型判定）。
    /// </returns>
    internal static ITransportFault? Adapt(Exception exception)
        => exception is Mud.HttpUtils.ApiException apiException
            ? new ApiExceptionFault(apiException)
            : null;

    /// <summary>
    /// <c>Mud.HttpUtils.ApiException</c> 的端口投影：只透出状态码，
    /// 不把组件的其余面（内容 / 请求 URI）带进中立契约。
    /// </summary>
    private sealed class ApiExceptionFault : ITransportFault
    {
        private readonly Mud.HttpUtils.ApiException _apiException;

        internal ApiExceptionFault(Mud.HttpUtils.ApiException apiException)
            => _apiException = apiException;

        /// <inheritdoc />
        public int? StatusCode => (int?)_apiException.StatusCode;
    }
}
