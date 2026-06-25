// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Search;

/// <summary></summary>
public class SchemaTagOptions
{
    /// <summary>
    /// <para>tag 对应的枚举值名称</para>
    /// <para>**示例值**："status"</para>
    /// <para>**数据校验规则**：</para>
    /// <para>- 最大长度：`20` 字符</para>
    /// <para>必填：是</para>
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// <para>标签对应的颜色</para>
    /// <para>**示例值**："blue"</para>
    /// <para>**可选值有**：</para>
    /// <para>red:含警示性、敏感性的提示信息,green:表示成功、完成、完毕的提示信息,blue:组件架构、职能等中性信息,grey:中立系统提示信息（慎重使用）,yellow:焦点信息、推广性信息</para>
    /// <para>必填：是</para>
    /// <para>可选值：<list type="bullet">
    /// <item>red：含警示性、敏感性的提示信息</item>
    /// <item>green：表示成功、完成、完毕的提示信息</item>
    /// <item>blue：组件架构、职能等中性信息</item>
    /// <item>grey：中立系统提示信息（慎重使用）</item>
    /// <item>yellow：焦点信息、推广性信息</item>
    /// </list></para>
    /// </summary>
    [JsonPropertyName("color")]
    public string Color { get; set; } = string.Empty;

    /// <summary>
    /// <para>标签中展示的文本</para>
    /// <para>**示例值**："PASS"</para>
    /// <para>**数据校验规则**：</para>
    /// <para>- 最大长度：`8` 字符</para>
    /// <para>必填：是</para>
    /// </summary>
    [JsonPropertyName("text")]
    public string Text { get; set; } = string.Empty;
}