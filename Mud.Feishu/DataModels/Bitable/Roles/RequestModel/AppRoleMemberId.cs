// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Bitable;

/// <summary>
/// <para>协作者列表</para>
/// </summary>
public class AppRoleMemberId
{
    /// <summary>
    /// <para>协作者 ID 的类型</para>
    /// <para>必填：否</para>
    /// <para>示例值：open_id</para>
    /// <para>可选值：<list type="bullet">
    /// <item>open_id：以 open_id 来识别协作者。获取方式参考[如何获取不同的用户 ID](https://open.feishu.cn/document/home/user-identity-introduction/open-id)。</item>
    /// <item>union_id：以 union_id 来识别协作者。获取方式参考[如何获取不同的用户 ID](https://open.feishu.cn/document/home/user-identity-introduction/open-id)</item>
    /// <item>user_id：以 user_id 来识别协作者。获取方式参考[如何获取不同的用户 ID](https://open.feishu.cn/document/home/user-identity-introduction/open-id)</item>
    /// <item>chat_id：以 chat_id 来识别协作者。获取方式参考[群 ID 说明](https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/reference/im-v1/chat-id-description)</item>
    /// <item>department_id：以 department_id 来识别协作者。调用前，请确保应用有部门的可见性，参考[配置应用可用范围](https://open.feishu.cn/document/home/introduction-to-scope-and-authorization/availability)。获取 department_id 方式参考[部门资源介绍](https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/reference/contact-v3/department/field-overview)</item>
    /// <item>open_department_id：以 open_department_id 来识别协作者。调用前，请确保应用有部门的可见性，参考[配置应用可用范围](https://open.feishu.cn/document/home/introduction-to-scope-and-authorization/availability)。获取 open_department_id 方式参考[部门资源介绍](https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/reference/contact-v3/department/field-overview)</item>
    /// </list></para>
    /// <para>默认值：open_id</para>
    /// </summary>
    [JsonPropertyName("type")]
    public string? Type { get; set; }

    /// <summary>
    /// <para>协作者的 ID，需与 type 的类型需一致。获取 ID 方式参考 type 参数描述。</para>
    /// <para>必填：是</para>
    /// <para>示例值：ou_35990a9d9052051a2fae9b2f1afabcef</para>
    /// </summary>
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;
}