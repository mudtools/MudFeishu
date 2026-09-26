// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Lingo;

/// <summary>
/// 模糊搜索词条请求体
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Lingo")]
public class SearchEntityRequest
{
    /// <summary>
    /// <para>搜索关键词，与词条名、别名、释义等信息进行模糊匹配</para>
    /// <para>必填：否</para>
    /// <para>长度范围：1 ～ 100 字符</para>
    /// <para>示例值：飞书词典</para>
    /// </summary>
    [JsonPropertyName("query")]
    public string? Query { get; set; }

    /// <summary>
    /// <para>分类筛选</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("classification_filter")]
    public LingoClassificationFilter? ClassificationFilter { get; set; }

    /// <summary>
    /// <para>词条的创建来源，1 - 用户主动创建，2 - 批量导入，3 - 官方词，4 - OpenAPI 创建</para>
    /// <para>必填：否</para>
    /// <para>示例值：[1]</para>
    /// </summary>
    [JsonPropertyName("sources")]
    public int[]? Sources { get; set; }

    /// <summary>
    /// <para>创建者</para>
    /// <para>必填：否</para>
    /// <para>示例值：["ou_30b07b63089ea46518789914dac63d36"]</para>
    /// </summary>
    [JsonPropertyName("creators")]
    public string[]? Creators { get; set; }
}
