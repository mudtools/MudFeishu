// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Bitable;

/// <summary>
/// <para>字段编组的成员</para>
/// </summary>
public class FieldGroupChild
{
    /// <summary>
    /// <para>编组成员类型</para>
    /// <para>必填：是</para>
    /// <para>示例值：field</para>
    /// <para>可选值：<list type="bullet">
    /// <item>field：字段</item>
    /// </list></para>
    /// </summary>
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// <para>编组成员ID，必须与type的取值一致（如type为field时，id为字段的ID）；字段ID可以通过调用[获取字段列表]接口获取</para>
    /// <para>必填：是</para>
    /// <para>示例值：fldPTb0U2y</para>
    /// </summary>
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;
}