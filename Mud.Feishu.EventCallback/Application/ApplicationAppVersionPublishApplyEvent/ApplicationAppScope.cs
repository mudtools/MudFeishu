// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.EventCallback.Application;

/// <summary>
/// ApplicationAppScope
/// <para>应用权限列表</para>
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Application")]
public class ApplicationAppScope
{
    /// <summary>
    /// <para>应用权限</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("scope")]
    public string? Scope { get; set; }

    /// <summary>
    /// <para>应用权限的国际化描述</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("description")]
    public string? Description { get; set; }

    /// <summary>
    /// <para>权限等级描述</para>
    /// <para>**可选值有**：</para>
    /// <para>- 1：普通权限</para>
    /// <para>- 2：高级权限</para>
    /// <para>- 3：超敏感权限</para>
    /// <para>- 0：未知等级</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("level")]
    public int? Level { get; set; }

    /// <summary>
    /// <para>返回用户身份类型user、应用身份类型tenant。如果两种类型都支持，则同时返回两个。</para>
    /// <para>**可选值有**：</para>
    /// <para>- tenant：应用身份类型</para>
    /// <para>- user：用户身份类型</para>
    /// <para>**数据校验规则**：</para>
    /// <para>- 长度范围：`0` ～ `200`</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("token_types")]
    public string[]? TokenTypes { get; set; }
}
