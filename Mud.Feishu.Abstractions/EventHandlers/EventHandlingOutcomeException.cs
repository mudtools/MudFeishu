// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.Abstractions.EventHandlers;

/// <summary>
/// 事件处理终态标记，用于向 AfterHandleAsync 传递除业务异常外的特殊终态。
/// </summary>
/// <remarks>
/// 接口契约中 <c>exception == null</c> 表示成功；本类型让拦截/取消/去重致命等
/// 非业务终态也能被后置拦截器区分，且不改变 <c>AfterHandleAsync</c> 方法签名。
/// 对既有拦截器透明：未识别本类型时按一般异常处理即可。
/// </remarks>
public class EventHandlingOutcomeException : InvalidOperationException
{
    /// <summary>
    /// 终态类别：<c>intercepted</c> / <c>canceled</c> / <c>dedup_fatal</c> / <c>timeout</c>。
    /// </summary>
    public string OutcomeKind { get; }

    /// <summary>
    /// 初始化终态标记异常。
    /// </summary>
    /// <param name="outcomeKind">终态类别</param>
    /// <param name="message">可读消息</param>
    public EventHandlingOutcomeException(string outcomeKind, string message)
        : base(message)
    {
        OutcomeKind = outcomeKind;
    }
}
