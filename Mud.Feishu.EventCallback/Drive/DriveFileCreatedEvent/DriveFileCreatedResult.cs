// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.EventCallback.Drive;


/// <summary>
/// 文件夹下文件创建
/// <para>当用户订阅的文件夹下有新建文件时将触发此事件。</para>
/// <para>事件类型:drive.file.created_in_folder_v1</para>
/// <para>使用时请继承：<see cref="DriveFileCreatedEventHandler"/></para>
/// <para>文档地址：https://open.feishu.cn/document/docs/drive-v1/event/list/created_in_folder</para>
/// </summary>
[GenerateEventHandler(EventType = FeishuEventTypes.DriveFileCreated, HandlerNamespace = Consts.HandlerNamespace,
              InheritedFrom = Consts.InheritedFrom)]
public class DriveFileCreatedResult : IEventResult
{
    /// <summary>
    /// <para>文件类型，与文件的 file_token 相匹配</para>
    /// <para>**示例值**：docx</para>
    /// <para>**可选值有**：</para>
    /// <para>doc:旧版文档,docx:新版文档,sheet:电子表格,bitable:多维表格,slides:幻灯片,file:文件</para>
    /// <para>**数据校验规则**：</para>
    /// <para>- 长度范围：`1` ～ `50` 字符</para>
    /// <para>必填：否</para>
    /// <para>可选值：<list type="bullet">
    /// <item>doc：旧版文档</item>
    /// <item>docx：新版文档</item>
    /// <item>sheet：电子表格</item>
    /// <item>bitable：多维表格</item>
    /// <item>slides：幻灯片</item>
    /// <item>file：文件</item>
    /// </list></para>
    /// </summary>
    [JsonPropertyName("file_type")]
    public string? FileType { get; set; }

    /// <summary>
    /// <para>文件的 token，获取方式见 [如何获取云文档资源相关 token](https://open.feishu.cn/document/ukTMukTMukTM/uczNzUjL3czM14yN3MTN#08bb5df6)</para>
    /// <para>**示例值**：docxnBKgoMyY5OMbUG6FioTXuBe</para>
    /// <para>**数据校验规则**：</para>
    /// <para>- 长度范围：`22` ～ `27` 字符</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("file_token")]
    public string? FileToken { get; set; }

    /// <summary>
    /// <para>文件夹 Token。获取方式见[文件夹概述](https://open.feishu.cn/document/ukTMukTMukTM/ugTNzUjL4UzM14CO1MTN/folder-overview)</para>
    /// <para>**数据校验规则**：</para>
    /// <para>- 长度范围：`22` ～ `27` 字符</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("folder_token")]
    public string? FolderToken { get; set; }

    /// <summary>
    /// <para>操作者的用户 ID</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("operator_id")]
    public UserIdInfo? OperatorId { get; set; }

    /// <summary>
    /// <para>订阅者的用户 ID 列表</para>
    /// <para>**数据校验规则**：</para>
    /// <para>- 长度范围：`0` ～ `100`</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("subscriber_ids")]
    public UserIdInfo[]? SubscriberIds { get; set; }
}
