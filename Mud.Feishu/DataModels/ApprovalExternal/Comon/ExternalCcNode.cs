// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.ApprovalExternal;

/// <summary>
/// <para>抄送列表</para>
/// </summary>
public record ExternalCcNode
{
    /// <summary>
    /// <para>审批实例内抄送唯一标识。</para>
    /// <para>必填：是</para>
    /// <para>示例值：123456</para>
    /// </summary>
    [JsonPropertyName("cc_id")]
    public string CcId { get; set; } = string.Empty;

    /// <summary>
    /// <para>抄送人的 user_id。获取方式参见[如何获取用户的 User ID](https://open.feishu.cn/document/uAjLw4CM/ugTN1YjL4UTN24CO1UjN/trouble-shooting/how-to-obtain-user-id)。</para>
    /// <para>**注意**：抄送人的 open_id 和 user_id 需至少传入一个。</para>
    /// <para>必填：否</para>
    /// <para>示例值：12345</para>
    /// </summary>
    [JsonPropertyName("user_id")]
    public string? UserId { get; set; }

    /// <summary>
    /// <para>抄送人的 open_id。获取方式参见[如何获取用户的 Open ID](https://open.feishu.cn/document/uAjLw4CM/ugTN1YjL4UTN24CO1UjN/trouble-shooting/how-to-obtain-openid)。</para>
    /// <para>**注意**：抄送人的 open_id 和 user_id 需至少传入一个。</para>
    /// <para>必填：否</para>
    /// <para>示例值：ou_be73cbc0ee35eb6ca54e9e7cc14998c1</para>
    /// </summary>
    [JsonPropertyName("open_id")]
    public string? OpenId { get; set; }

    /// <summary>
    /// <para>审批抄送跳转链接。设置的链接用于在审批中心 **抄送我** 列表内点击跳转，跳回三方审批系统查看审批抄送详情。</para>
    /// <para>必填：是</para>
    /// </summary>
    [JsonPropertyName("links")]
    public InstanceLink Links { get; set; } = new();

    /// <summary>
    /// <para>抄送人的阅读状态</para>
    /// <para>必填：是</para>
    /// <para>示例值：READ</para>
    /// <para>可选值：<list type="bullet">
    /// <item>READ：已读</item>
    /// <item>UNREAD：未读</item>
    /// </list></para>
    /// </summary>
    [JsonPropertyName("read_status")]
    public string ReadStatus { get; set; } = string.Empty;

    /// <summary>
    /// <para>扩展字段。JSON 格式，传值时需要压缩转义为字符串。</para>
    /// <para>必填：否</para>
    /// <para>示例值：{\"xxx\":\"xxx\"}</para>
    /// </summary>
    [JsonPropertyName("extra")]
    public string? Extra { get; set; }

    /// <summary>
    /// <para>抄送任务名称。</para>
    /// <para>必填：否</para>
    /// <para>示例值：xxx</para>
    /// </summary>
    [JsonPropertyName("title")]
    public string? Title { get; set; }

    /// <summary>
    /// <para>抄送发起时间，Unix 毫秒时间戳。</para>
    /// <para>必填：是</para>
    /// <para>示例值：1556468012678</para>
    /// </summary>
    [JsonPropertyName("create_time")]
    public string CreateTime { get; set; } = string.Empty;

    /// <summary>
    /// <para>抄送最近更新时间，Unix 毫秒时间戳，用于推送数据版本。如果 update_mode 值为 UPDATE，则仅当传过来的 update_time 有变化时（变大），才会更新审批中心中的审批实例信息。</para>
    /// <para>必填：是</para>
    /// <para>示例值：1556468012678</para>
    /// </summary>
    [JsonPropertyName("update_time")]
    public string UpdateTime { get; set; } = string.Empty;

    /// <summary>
    /// <para>列表页打开审批任务的方式。</para>
    /// <para>必填：否</para>
    /// <para>示例值：BROWSER</para>
    /// <para>可选值：<list type="bullet">
    /// <item>BROWSER：跳转系统默认浏览器打开</item>
    /// <item>SIDEBAR：飞书中侧边抽屉打开</item>
    /// <item>NORMAL：飞书内嵌页面打开</item>
    /// <item>TRUSTEESHIP：以托管模式打开</item>
    /// </list></para>
    /// </summary>
    [JsonPropertyName("display_method")]
    public string? DisplayMethod { get; set; }
}