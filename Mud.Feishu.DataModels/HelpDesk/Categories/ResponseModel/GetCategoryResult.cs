// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.HelpDesk;

/// <summary>
/// 获取知识库分类响应体
/// </summary>
[HttpJsonSerializable(SerializerClassName = "HelpDesk")]
public class GetCategoryResult
{
    /// <summary>
    /// <para>知识库分类 ID</para>
    /// <para>必填：否</para>
    /// <para>示例值："6948728206392295444"</para>
    /// </summary>
    [JsonPropertyName("category_id")]
    public string? CategoryId { get; set; }

    /// <summary>
    /// <para>知识库分类 ID（旧版本字段，建议使用 category_id）</para>
    /// <para>必填：否</para>
    /// <para>示例值："6948728206392295444"</para>
    /// </summary>
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    /// <summary>
    /// <para>名称</para>
    /// <para>必填：否</para>
    /// <para>示例值："Create a team and invite members"</para>
    /// </summary>
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    /// <summary>
    /// <para>服务台 ID</para>
    /// <para>必填：否</para>
    /// <para>示例值："6939771743531696147"</para>
    /// </summary>
    [JsonPropertyName("helpdesk_id")]
    public string? HelpdeskId { get; set; }

    /// <summary>
    /// <para>语言</para>
    /// <para>必填：否</para>
    /// <para>示例值："zh_cn"</para>
    /// </summary>
    [JsonPropertyName("language")]
    public string? Language { get; set; }
}
