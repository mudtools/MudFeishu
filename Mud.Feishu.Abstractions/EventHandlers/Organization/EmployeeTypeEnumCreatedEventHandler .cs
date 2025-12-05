// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.Abstractions.DataModels.Organization;

namespace Mud.Feishu.Abstractions.EventHandlers;

/// <summary>
/// 新建人员类型事件
/// <para>当应用订阅该事件后，如果新增了人员类型中的选项，则会触发该事件。</para>
/// <para>事件类型:contact.employee_type_enum.created_v3</para>
/// <para>文档地址：<see href="https://open.feishu.cn/document/server-docs/contact-v3/employee_type_enum/events/created"/> </para>
/// </summary>
public abstract class EmployeeTypeEnumCreatedEventHandler : DefaultFeishuEventHandler<EmployeeTypeEnumCreatedResult>
{
    /// <summary>
    /// 默认构造函数
    /// </summary>
    /// <param name="logger"></param>
    public EmployeeTypeEnumCreatedEventHandler(ILogger logger)
        : base(logger)
    {
    }

    /// <summary>
    /// 支持的事件类型
    /// </summary>
    public override string SupportedEventType => FeishuEventTypes.EmployeeTypeEnumCreated;
}
