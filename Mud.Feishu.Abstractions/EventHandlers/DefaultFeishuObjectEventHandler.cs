// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Microsoft.Extensions.Logging;

namespace Mud.Feishu.Abstractions.EventHandlers;

/// <summary>
/// 飞书对象事件处理器基类，用于处理具有特定结果类型的飞书事件
/// </summary>
/// <typeparam name="T">事件结果对象的类型，必须是引用类型且具有无参构造函数</typeparam>
public abstract class DefaultFeishuObjectEventHandler<T> : DefaultFeishuEventHandler<ObjectEventResult<T>>
    where T : class, new()
{
    /// <summary>
    /// 初始化 DefaultFeishuObjectEventHandler 类的新实例
    /// </summary>
    /// <param name="logger">用于记录日志的 ILogger 实例</param>
    public DefaultFeishuObjectEventHandler(ILogger logger) : base(logger)
    {

    }
}