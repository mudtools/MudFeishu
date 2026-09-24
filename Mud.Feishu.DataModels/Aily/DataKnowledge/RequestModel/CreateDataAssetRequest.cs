// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Aily;

/// <summary>
/// 创建数据知识（Data Asset）请求体
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Aily")]
public class CreateDataAssetRequest
{
    /// <summary>
    /// <para>连接类型</para>
    /// <para>必填：是</para>
    /// <para>示例值：direct</para>
    /// <para>可选值：<list type="bullet">
    /// <item>import：导入模式</item>
    /// <item>direct：直连模式</item>
    /// </list></para>
    /// <para>最大长度：255</para>
    /// </summary>
    [JsonPropertyName("connect_type")]
    public string ConnectType { get; set; } = string.Empty;

    /// <summary>
    /// <para>数据源类型</para>
    /// <para>必填：是</para>
    /// <para>示例值：file</para>
    /// <para>可选值：<list type="bullet">
    /// <item>file：文件，只支持导入模式</item>
    /// <item>lark_wiki_space：飞书知识空间，只支持直连模式</item>
    /// <item>lark_doc：飞书云文档，导入模式只支持 docx</item>
    /// <item>lark_helpdesk：飞书服务台，只支持直连模式</item>
    /// </list></para>
    /// <para>最大长度：255</para>
    /// </summary>
    [JsonPropertyName("source_type")]
    public string SourceType { get; set; } = string.Empty;

    /// <summary>
    /// <para>知识导入配置</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("import_knowledge_setting")]
    public DataAssetImportKnowledgeSetting? ImportKnowledgeSetting { get; set; }

    /// <summary>
    /// <para>数据知识描述信息</para>
    /// <para>必填：否</para>
    /// <para>示例值：{"zh_cn":"描述"}</para>
    /// </summary>
    [JsonPropertyName("description")]
    public Dictionary<string, string>? Description { get; set; }
}
