// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Search;

/// <summary></summary>
[HttpJsonSerializable(SerializerClassName = "Search")]
public class DateItemAcl
{
    /// <summary>
    /// <para>权限类型，优先级：Deny &gt; Allow。</para>
    /// <para>**示例值**："allow"</para>
    /// <para>**可选值有**：</para>
    /// <para>allow:允许访问,deny:禁止访问</para>
    /// <para>必填：否</para>
    /// <para>可选值：<list type="bullet">
    /// <item>allow：允许访问</item>
    /// <item>deny：禁止访问</item>
    /// </list></para>
    /// </summary>
    [JsonPropertyName("access")]
    public string? Access { get; set; }

    /// <summary>
    /// <para>设置的权限值，例如 userID ，依赖 type 描述。</para>
    /// <para>**注**：在 type 为 user 且 access 为 allow 时，可填 "everyone" 来表示该数据项对全员可见；</para>
    /// <para>**示例值**："d35e3c23"</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("value")]
    public string? Value { get; set; }

    /// <summary>
    /// <para>权限值类型</para>
    /// <para>**示例值**："user"</para>
    /// <para>**可选值有**：</para>
    /// <para>user:访问权限控制中指定“用户”可以访问或拒绝访问该条数据,group:(已下线)访问权限控制中指定“用户组”可以访问或拒绝访问该条数据,open_id:用户的open_id</para>
    /// <para>必填：否</para>
    /// <para>可选值：<list type="bullet">
    /// <item>user：访问权限控制中指定“用户”可以访问或拒绝访问该条数据</item>
    /// <item>group：(已下线)访问权限控制中指定“用户组”可以访问或拒绝访问该条数据</item>
    /// <item>open_id：用户的open_id</item>
    /// </list></para>
    /// </summary>
    [JsonPropertyName("type")]
    public string? Type { get; set; }

}
