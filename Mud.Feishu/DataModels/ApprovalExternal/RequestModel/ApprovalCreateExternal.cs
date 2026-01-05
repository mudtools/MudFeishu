// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.ApprovalExternal;

/// <summary>
/// <para>三方审批相关信息。</para>
/// </summary>
public class ApprovalCreateExternal
{
    /// <summary>
    /// <para>列表中用于提示审批来自哪个三方系统。</para>
    /// <para>**注意**：</para>
    /// <para>- 这里传入的是国际化文案 Key（即 i18n_resources.texts 参数中的 Key），还需要在 i18n_resources.texts 参数中以 Key:Value 格式进行赋值。</para>
    /// <para>- 该参数需要以 @i18n@ 开头。</para>
    /// <para>- 在 i18n_resources 中为该参数赋值时，无需设置 **来自** 前缀，审批中心默认会拼接 **来自** 前缀。</para>
    /// <para>必填：否</para>
    /// <para>示例值：@i18n@3</para>
    /// </summary>
    [JsonPropertyName("biz_name")]
    public string? BizName { get; set; }

    /// <summary>
    /// <para>审批定义业务类别，自定义设置。</para>
    /// <para>必填：否</para>
    /// <para>示例值：permission</para>
    /// </summary>
    [JsonPropertyName("biz_type")]
    public string? BizType { get; set; }

    /// <summary>
    /// <para>移动端发起三方审批的链接。</para>
    /// <para>- 如果设置了该链接，则在移动端发起审批时，会跳转到该链接对应的三方审批发起页。</para>
    /// <para>- 如果不设置该链接，则在移动端不显示该审批。</para>
    /// <para>必填：否</para>
    /// <para>示例值：https://applink.feishu.cn/client/mini_program/open?appId=cli_9c90fc38e07a9101&amp;path=pages/approval-form/index?id=9999</para>
    /// </summary>
    [JsonPropertyName("create_link_mobile")]
    public string? CreateLinkMobile { get; set; }

    /// <summary>
    /// <para>PC端发起三方审批的链接。</para>
    /// <para>- 如果设置了该链接，则在 PC 端发起审批时，会跳转到该链接对应的三方审批发起页。</para>
    /// <para>- 如果不设置该链接，则在 PC 端不显示该审批。</para>
    /// <para>必填：否</para>
    /// <para>示例值：https://applink.feishu.cn/client/mini_program/open?mode=appCenter&amp;appId=cli_9c90fc38e07a9101&amp;path=pc/pages/create-form/index?id=9999</para>
    /// </summary>
    [JsonPropertyName("create_link_pc")]
    public string? CreateLinkPc { get; set; }

    /// <summary>
    /// <para>审批定义是否要在 PC 端的发起审批页面展示，如果为 true 则展示，否则不展示，默认为false。</para>
    /// <para>**注意**：support_pc 和 support_mobile 不可都为 false。</para>
    /// <para>必填：否</para>
    /// <para>示例值：true</para>
    /// </summary>
    [JsonPropertyName("support_pc")]
    public bool? SupportPc { get; set; }

    /// <summary>
    /// <para>审批定义是否要在移动端的发起审批页面展示，如果为 true 则展示，否则不展示，默认为false。</para>
    /// <para>**注意**：support_pc 和 support_mobile 不可都为 false。</para>
    /// <para>必填：否</para>
    /// <para>示例值：true</para>
    /// </summary>
    [JsonPropertyName("support_mobile")]
    public bool? SupportMobile { get; set; }

    /// <summary>
    /// <para>是否支持批量已读，默认为false</para>
    /// <para>必填：否</para>
    /// <para>示例值：true</para>
    /// </summary>
    [JsonPropertyName("support_batch_read")]
    public bool? SupportBatchRead { get; set; }

    /// <summary>
    /// <para>是否支持标注可读</para>
    /// <para>**注意**：该字段无效，暂不支持使用。</para>
    /// <para>必填：否</para>
    /// <para>示例值：true</para>
    /// </summary>
    [JsonPropertyName("enable_mark_readed")]
    public bool? EnableMarkReaded { get; set; }

    /// <summary>
    /// <para>是否支持快速操作</para>
    /// <para>**注意**：该字段无效，暂不支持使用。</para>
    /// <para>必填：否</para>
    /// <para>示例值：true</para>
    /// </summary>
    [JsonPropertyName("enable_quick_operate")]
    public bool? EnableQuickOperate { get; set; }

    /// <summary>
    /// <para>三方系统的操作回调 URL，**待审批** 实例的任务审批人点击同意或拒绝操作后，审批中心调用该 URL 通知三方系统，回调地址相关信息可参见[三方审批快捷审批回调](https://open.feishu.cn/document/ukTMukTMukTM/ukjNyYjL5YjM24SO2IjN/quick-approval-callback)。</para>
    /// <para>必填：否</para>
    /// <para>示例值：http://www.feishu.cn/approval/openapi/instanceOperate</para>
    /// </summary>
    [JsonPropertyName("action_callback_url")]
    public string? ActionCallbackUrl { get; set; }

    /// <summary>
    /// <para>回调时带的 token，用于业务系统验证请求来自审批中心。</para>
    /// <para>必填：否</para>
    /// <para>示例值：sdjkljkx9lsadf110</para>
    /// </summary>
    [JsonPropertyName("action_callback_token")]
    public string? ActionCallbackToken { get; set; }

    /// <summary>
    /// <para>请求参数加密密钥。如果配置了该参数，则会对请求参数进行加密，接收请求后需要对请求进行解密。加解密算法参考[关联外部选项说明](https://open.feishu.cn/document/ukTMukTMukTM/uADM4QjLwADO04CMwgDN)。</para>
    /// <para>必填：否</para>
    /// <para>示例值：gfdqedvsadfgfsd</para>
    /// </summary>
    [JsonPropertyName("action_callback_key")]
    public string? ActionCallbackKey { get; set; }

    /// <summary>
    /// <para>是否支持批量审批。取值为 true 时，审批人在处理该定义下的审批任务时可以批量处理多个任务， 默认为false。</para>
    /// <para>必填：否</para>
    /// <para>示例值：true</para>
    /// </summary>
    [JsonPropertyName("allow_batch_operate")]
    public bool? AllowBatchOperate { get; set; }

    /// <summary>
    /// <para>审批流程数据是否不纳入效率统计，默认为false</para>
    /// <para>必填：否</para>
    /// <para>示例值：true</para>
    /// </summary>
    [JsonPropertyName("exclude_efficiency_statistics")]
    public bool? ExcludeEfficiencyStatistics { get; set; }
}
