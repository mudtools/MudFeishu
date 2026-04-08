// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.Abstractions.Services;

namespace Mud.Feishu.Abstractions.EventHandlers;

/// <summary>
/// 支持 AppKey 上下文注入的处理器接口
/// </summary>
/// <remarks>
/// 此接口用于框架内部，在处理器执行前注入 AppKey 上下文访问器。
/// 实现此接口的处理器可以在多应用场景下正确获取当前 AppKey。
/// </remarks>
public interface ISetAppKeyAware
{
    /// <summary>
    /// 设置应用键上下文访问器
    /// </summary>
    /// <param name="appKeyAccessor">应用键上下文访问器</param>
    void SetAppKeyAccessor(IAppKeyAccessor appKeyAccessor);
}
