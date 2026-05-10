// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.Abstractions;

/// <summary>
/// 飞书事件处理拦截器接口
/// 允许在事件处理前后执行自定义逻辑，如日志记录、指标收集、权限验证等
/// </summary>
/// <remarks>
/// 该接口用于在事件处理的各个生命周期点插入自定义逻辑，支持以下场景：
/// - 日志记录：记录事件处理的开始、成功、失败
/// - 指标收集：统计事件处理时间、成功率等
/// - 安全审计：记录敏感事件的处理情况
/// - 权限控制：根据事件类型或内容决定是否处理
/// </remarks>
public interface IFeishuEventInterceptor
{
    /// <summary>
    /// 事件处理前拦截
    /// </summary>
    /// <param name="eventType">事件类型</param>
    /// <param name="eventData">事件数据</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>如果返回 false，将中断事件处理流程</returns>
    Task<bool> BeforeHandleAsync(string eventType, EventData eventData, CancellationToken cancellationToken = default);

    /// <summary>
    /// 事件处理后拦截
    /// </summary>
    /// <param name="eventType">事件类型</param>
    /// <param name="eventData">事件数据</param>
    /// <param name="exception">处理过程中的异常，如果为 null 表示处理成功</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns></returns>
    Task AfterHandleAsync(string eventType, EventData eventData, Exception? exception, CancellationToken cancellationToken = default);
}
