// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.Abstractions.DataModels.Organization.DepartmentCreatedEvent;

namespace Mud.Feishu.Abstractions.EventHandlers.Organization;

/// <summary>
/// 部门创建事件处理器
/// <para>当应用订阅该事件后，如果通讯录内有部门被创建，则会触发该事件。</para>
/// <para>事件类型:contact.department.created_v3</para>
/// <para>订阅该事件详细文档：<see href="https://open.feishu.cn/document/server-docs/contact-v3/user/events/created"/></para>
/// </summary>
public abstract class DepartmentCreatedEventHandler : DefaultFeishuObjectEventHandler<DepartmentCreatedEventResult>
{
    /// <summary>
    /// 默认构造函数
    /// </summary>
    /// <param name="logger"></param>
    public DepartmentCreatedEventHandler(ILogger logger)
        : base(logger)
    {
    }

    /// <summary>
    /// 支持的事件类型
    /// </summary>
    public override string SupportedEventType => FeishuEventTypes.DepartmentCreated;
}
