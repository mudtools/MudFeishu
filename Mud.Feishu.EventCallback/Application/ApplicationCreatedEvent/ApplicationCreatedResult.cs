// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.EventCallback.Application;

/// <summary>
/// 应用创建
/// <para>当企业内有新的自建应用被创建时推送此事件（创建就会产生此事件，不需要发版）</para>
/// <para>事件类型:application.application.created_v6</para>
/// <para>使用时请继承：<see cref="ApplicationCreatedEventHandler"/></para>
/// <para>文档地址：https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/application-v6/application/events/created</para>
/// </summary>
[GenerateEventHandler(EventType = FeishuEventTypes.ApplicationCreated, HandlerNamespace = Consts.HandlerNamespace,
              InheritedFrom = Consts.InheritedFrom, HeaderType = nameof(FeishuEventHeader))]
[HttpJsonSerializable(SerializerClassName = "Application")]
public class ApplicationCreatedResult : IEventResult
{
    /// <summary>
    /// <para>用户 ID</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("operator_id")]
    public UserIdInfo? OperatorId { get; set; }

    /// <summary>
    /// <para>应用 ID</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("app_id")]
    public string? AppId { get; set; }

    /// <summary>
    /// <para>应用名称</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    /// <summary>
    /// <para>应用描述</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("description")]
    public string? Description { get; set; }

    /// <summary>
    /// <para>应用图标链接</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("avatar")]
    public string? Avatar { get; set; }

    /// <summary>
    /// <para>应用类型，0: 自建应用，1: 应用商店应用</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("app_scene_type")]
    public int? AppSceneType { get; set; }

    /// <summary>
    /// <para>应用主语言</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("primary_language")]
    public string? PrimaryLanguage { get; set; }

    /// <summary>
    /// <para>应用创建来源</para>
    /// <para>**可选值有**：</para>
    /// <para>- developer_console：开发者后台</para>
    /// <para>- base：多维表格自动化流程创建的应用</para>
    /// <para>- app_engine：飞书应用引擎</para>
    /// <para>- bot_builder：机器人助手</para>
    /// <para>- aily：aily(智能伙伴搭建平台)</para>
    /// <para>- unknown：未知来源</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("create_source")]
    public string? CreateSource { get; set; }
}
