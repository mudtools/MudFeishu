// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Drive.FileVersions;

/// <summary>
/// 版本文档列表
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Drive")]
public class FileVersionInfo
{
    /// <summary>
    /// <para>版本文档的标题</para>
    /// <para>必填：否</para>
    /// <para>示例值：项目文档 第1版</para>
    /// </summary>
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    /// <summary>
    /// <para>版本文档的版本标识</para>
    /// <para>必填：否</para>
    /// <para>示例值：fnJfyX</para>
    /// </summary>
    [JsonPropertyName("version")]
    public string? VersionSuffix { get; set; }

    /// <summary>
    /// <para>当前版本对应的源文档的 token</para>
    /// <para>必填：否</para>
    /// <para>示例值：doxbcyvqZlSc9WlHvQMlSJabcef</para>
    /// </summary>
    [JsonPropertyName("parent_token")]
    public string? ParentToken { get; set; }

    /// <summary>
    /// <para>版本文档的所有者的 ID</para>
    /// <para>必填：否</para>
    /// <para>示例值：694699009591869450</para>
    /// </summary>
    [JsonPropertyName("owner_id")]
    public string? OwnerId { get; set; }

    /// <summary>
    /// <para>版本文档的创建者的 ID</para>
    /// <para>必填：否</para>
    /// <para>示例值：694699009591869451</para>
    /// </summary>
    [JsonPropertyName("creator_id")]
    public string? CreatorId { get; set; }

    /// <summary>
    /// <para>版本文档的创建时间，Unix 时间戳，单位为秒</para>
    /// <para>必填：否</para>
    /// <para>示例值：1660708537</para>
    /// </summary>
    [JsonPropertyName("create_time")]
    public string? CreateTime { get; set; }

    /// <summary>
    /// <para>版本文档的更新时间</para>
    /// <para>必填：否</para>
    /// <para>示例值：1660708537</para>
    /// </summary>
    [JsonPropertyName("update_time")]
    public string? UpdateTime { get; set; }

    /// <summary>
    /// <para>版本文档的状态</para>
    /// <para>必填：否</para>
    /// <para>示例值：0</para>
    /// <para>可选值：<list type="bullet">
    /// <item>0：正常状态</item>
    /// <item>1：删除状态</item>
    /// <item>2：回收站状态</item>
    /// </list></para>
    /// </summary>
    [JsonPropertyName("status")]
    public string? Status { get; set; }

    /// <summary>
    /// <para>版本文档的类型</para>
    /// <para>必填：否</para>
    /// <para>示例值：docx</para>
    /// <para>可选值：<list type="bullet">
    /// <item>docx：新版文档</item>
    /// <item>sheet：电子表格</item>
    /// </list></para>
    /// </summary>
    [JsonPropertyName("obj_type")]
    public string? ObjType { get; set; }

    /// <summary>
    /// <para>源文档的类型</para>
    /// <para>必填：否</para>
    /// <para>示例值：docx</para>
    /// <para>可选值：<list type="bullet">
    /// <item>docx：新版文档</item>
    /// <item>sheet：电子表格</item>
    /// </list></para>
    /// </summary>
    [JsonPropertyName("parent_type")]
    public string? ParentType { get; set; }
}
