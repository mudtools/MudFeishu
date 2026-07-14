// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Drive.Files;

/// <summary>
/// 复制文件请求体
/// </summary>
public class CopyFileRequest
{
    /// <summary>
    /// <para>复制的新文件的名称</para>
    /// <para>**数据校验规则**：最大长度为 `256` 字节</para>
    /// <para>必填：是</para>
    /// <para>示例值：Demo copy</para>
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// <para>被复制的源文件的类型。必须与 `file_token` 对应的源文件实际类型一致。</para>
    /// <para>**注意**：该参数为必填，请忽略左侧必填列的“否”。若该参数值为空或与实际文件类型不匹配，接口将返回失败。</para>
    /// <para>必填：否</para>
    /// <para>示例值：docx</para>
    /// <para>可选值：<list type="bullet">
    /// <item>file：文件类型</item>
    /// <item>doc：旧版文档。了解更多，参考[新旧版本文档说明](https://open.feishu.cn/document/ukTMukTMukTM/uUDN04SN0QjL1QDN/docs/upgraded-docs-access-guide/upgraded-docs-openapi-access-guide)。</item>
    /// <item>sheet：电子表格类型</item>
    /// <item>bitable：多维表格类型</item>
    /// <item>docx：新版文档类型</item>
    /// <item>mindnote：思维笔记类型</item>
    /// <item>slides：幻灯片类型</item>
    /// </list></para>
    /// </summary>
    [JsonPropertyName("type")]
    public string? Type { get; set; }

    /// <summary>
    /// <para>目标文件夹的 token。若传入根文件夹 token，表示复制的新文件将被创建在云空间根目录。了解如何获取文件夹 token，参考[文件夹概述](https://open.feishu.cn/document/ukTMukTMukTM/ugTNzUjL4UzM14CO1MTN/folder-overview)。</para>
    /// <para>必填：是</para>
    /// <para>示例值：fldbcO1UuPz8VwnpPx5a92abcef</para>
    /// </summary>
    [JsonPropertyName("folder_token")]
    public string FolderToken { get; set; } = string.Empty;

    /// <summary>
    /// <para>自定义请求附加参数，用于实现特殊的复制语义</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("extra")]
    public Property[]? Extras { get; set; }

    /// <summary>
    /// <para>自定义请求附加参数，用于实现特殊的复制语义</para>
    /// </summary>
    public class Property
    {
        /// <summary>
        /// <para>自定义属性键对象</para>
        /// <para>必填：是</para>
        /// <para>示例值：target_type</para>
        /// </summary>
        [JsonPropertyName("key")]
        public string Key { get; set; } = string.Empty;

        /// <summary>
        /// <para>自定义属性值对象</para>
        /// <para>必填：是</para>
        /// <para>示例值：docx</para>
        /// </summary>
        [JsonPropertyName("value")]
        public string Value { get; set; } = string.Empty;
    }
}
