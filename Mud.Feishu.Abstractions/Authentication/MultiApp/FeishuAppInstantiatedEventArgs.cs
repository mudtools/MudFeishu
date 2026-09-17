// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.Abstractions;

/// <summary>
/// TMA2-07 / P1-4：应用实例化事件参数。
/// </summary>
/// <remarks>
/// 当应用上下文首次实例化并注册到基类字典时触发。
/// <c>FeishuTokenRegistrationService</c> 订阅此事件做增量注册到后台刷新服务。
/// </remarks>
internal sealed class FeishuAppInstantiatedEventArgs : EventArgs
{
    /// <summary>
    /// 实例化的应用键。
    /// </summary>
    public string AppKey { get; }

    /// <summary>
    /// 实例化的应用上下文。
    /// </summary>
    public IFeishuAppContext Context { get; }

    /// <summary>
    /// 初始化 <see cref="FeishuAppInstantiatedEventArgs"/> 实例。
    /// </summary>
    /// <param name="appKey">应用唯一标识</param>
    /// <param name="context">应用上下文</param>
    public FeishuAppInstantiatedEventArgs(string appKey, IFeishuAppContext context)
    {
        AppKey = appKey;
        Context = context;
    }
}
