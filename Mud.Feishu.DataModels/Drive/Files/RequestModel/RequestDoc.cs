// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Drive.Files;

/// <summary>
/// <para>请求的文件的 token 和类型。一次请求中不可超过 200 个</para>
/// </summary>
public class RequestDoc
{
    /// <summary>
    /// <para>文件的 token</para>
    /// <para>必填：是</para>
    /// <para>示例值：doccnfYZzTlvXqZIGTdAHKabcef</para>
    /// </summary>
    [JsonPropertyName("doc_token")]
    public string DocToken { get; set; } = string.Empty;

    /// <summary>
    /// <para>文件的类型</para>
    /// <para>必填：是</para>
    /// <para>示例值：doc</para>
    /// <para>可选值：<list type="bullet">
    /// <item>doc：飞书文档</item>
    /// <item>sheet：飞书电子表格</item>
    /// <item>bitable：飞书多维表格</item>
    /// <item>mindnote：飞书思维笔记</item>
    /// <item>file：飞书文件</item>
    /// <item>wiki：飞书知识库</item>
    /// <item>docx：飞书新版文档</item>
    /// <item>folder：飞书文件夹</item>
    /// <item>synced_block：文档同步块（灰度中）</item>
    /// </list></para>
    /// </summary>
    [JsonPropertyName("doc_type")]
    public string DocType { get; set; } = string.Empty;
}
