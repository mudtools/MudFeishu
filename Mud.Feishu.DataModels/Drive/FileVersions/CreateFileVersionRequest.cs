// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Drive.FileVersions;

/// <summary>
/// 创建文档版本请求体
/// <para>创建文档版本。文档支持在线文档或电子表格。该接口为异步接口。</para>
/// </summary>
public class CreateFileVersionRequest
{
    /// <summary>
    /// <para>创建的版本文档的标题。</para>
    /// <para>最大长度 1024 个 Unicode 码点。通常情况下，一个英文或中文字符对应一个码点，但是某些特殊符号可能会对应多个码点。例如，家庭组合「👨‍👩‍👧」这个表情符号对应 5 个码点。</para>
    /// <para>**注意**：该参数必填，请忽略左侧必填列显示的“否”。</para>
    /// <para>必填：是</para>
    /// <para>示例值：项目文档 第 1 版</para>
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// <para>源文档的类型</para>
    /// <para>**注意**：该参数必填，请忽略左侧必填列显示的“否”。</para>
    /// <para>必填：是</para>
    /// <para>示例值：docx</para>
    /// <para>可选值：<list type="bullet">
    /// <item>docx：新版文档</item>
    /// <item>sheet：电子表格</item>
    /// </list></para>
    /// </summary>
    [JsonPropertyName("obj_type")]
    public string ObjType { get; set; } = string.Empty;
}
