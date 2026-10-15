// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.EventCallback.Application;

/// <summary>
/// 员工免审安装应用
/// <para>以下场景会触发该事件，事件体内包含对应用户的 ID 信息。 员工免审安装商店应用。功能介绍参见员工免审安装应用。 员工免审加入商店应用可用范围。当员工免审安装商店应用后，企业内其他员工在该应用详情页内获取应用时，员工会加入应用可用范围内，并触发该事件。</para>
/// <para>事件类型:application.application.visibility.added_v6</para>
/// <para>使用时请继承：<see cref="ApplicationVisibilityAddedEventHandler"/></para>
/// <para>文档地址：https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/application-v6/event/app-availability-scope-extended</para>
/// </summary>
[GenerateEventHandler(EventType = FeishuEventTypes.ApplicationVisibilityAdded, HandlerNamespace = Consts.HandlerNamespace,
              InheritedFrom = Consts.InheritedFrom, HeaderType = nameof(FeishuEventHeader))]
[HttpJsonSerializable(SerializerClassName = "Application")]
public class ApplicationVisibilityAddedResult : IEventResult
{
    /// <summary>
    /// <para>该字段暂无实际含义，可忽略</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("source")]
    public int? Source { get; set; }

    /// <summary>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("users")]
    public ApplicationVisibilityAddedUsers[]? Users { get; set; }
}
