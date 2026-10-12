// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.EventCallback.Organization;

/// <summary>
/// OrganizationUserCustomAttr
/// <para>自定义字段。如果企业管理员已在管理后台 &gt; 组织架构 &gt; 成员字段管理 &gt; 自定义字段管理 &gt; 全局设置中开启了 **允许开放平台 API 调用**，则该字段生效。</para>
/// <para>更多信息可参见[用户接口相关问题](https://open.feishu.cn/document/ugTN1YjL4UTN24CO1UjN/uQzN1YjL0cTN24CN3UjN#77061525)。</para>
/// <para>**字段权限要求（满足任一）**：</para>
/// <para>以应用身份读取通讯录(contact:contact:readonly_as_app)</para>
/// <para>获取用户受雇信息(contact:user.employee:readonly)</para>
/// <para>以应用身份访问通讯录(contact:contact:access_as_app)</para>
/// <para>读取通讯录(contact:contact:readonly)</para>
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Organization")]
public class OrganizationUserCustomAttr
{
    /// <summary>
    /// <para>自定义字段类型。</para>
    /// <para>**可能值有：**</para>
    /// <para>- `TEXT`：文本</para>
    /// <para>- `HREF`：网页</para>
    /// <para>- `ENUMERATION`：枚举</para>
    /// <para>- `PICTURE_ENUM`：图片</para>
    /// <para>- `GENERIC_USER`：用户</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("type")]
    public string? Type { get; set; }

    /// <summary>
    /// <para>自定义字段 ID。</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    /// <summary>
    /// <para>自定义字段取值。</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("value")]
    public OrganizationUserCustomAttrValue? Value { get; set; }
}
