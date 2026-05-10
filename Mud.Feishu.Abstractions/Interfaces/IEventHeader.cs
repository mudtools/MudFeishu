// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.Abstractions;

/// <summary>
/// 事件 Header 标记接口
/// <para>用于标识不同类型的事件 Header 数据模型，提供统一的 Header 字段访问</para>
/// </summary>
public interface IEventHeader
{
    /// <summary>
    /// 事件版本标识
    /// <para>v2.0 事件为 "2.0"，v1.0 事件为 null</para>
    /// </summary>
    string? Schema { get; }

    /// <summary>
    /// 事件的唯一标识
    /// </summary>
    string EventId { get; }

    /// <summary>
    /// 事件类型
    /// </summary>
    string EventType { get; }

    /// <summary>
    /// 租户 Key（企业标识）
    /// </summary>
    string TenantKey { get; }

    /// <summary>
    /// 应用 ID
    /// </summary>
    string AppId { get; }
}
