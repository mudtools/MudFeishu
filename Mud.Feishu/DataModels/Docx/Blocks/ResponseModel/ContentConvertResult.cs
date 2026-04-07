// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Docx;

/// <summary>
/// 内容转换响应结果
/// </summary>
public class ContentConvertResult
{
    /// <summary>
    /// 第一级块 ID 列表。文档内容被解析成一个个块（Block），每个块都有一个唯一的 block_id 作为标识符。块是文档中的最小构建单元，是内容的结构化组成元素，有着明确的含义。在一篇文档中，有多个不同类型的段落，这些段落被定义为块（Block）。块有多种形态，可以是一段文字、一张电子表格、一张图片或一个多维表格等。第一级块是指直接位于文档根节点下的块，这些块构成了文档内容的第一层结构。
    /// </summary>
    [JsonPropertyName("first_level_block_ids")]
    public string[] FirstLevelBlockIds { get; set; } = [];

    /// <summary>
    /// 块列表。文档内容被解析成一个个块（Block），每个块都有一个唯一的 block_id 作为标识符。块是文档中的最小构建单元，是内容的结构化组成元素，有着明确的含义。在一篇文档中，有多个不同类型的段落，这些段落被定义为块（Block）。块有多种形态，可以是一段文字、一张电子表格、一张图片或一个多维表格等。
    /// </summary>
    [JsonPropertyName("blocks")]
    public Block[] Blocks { get; set; } = [];

    /// <summary>
    /// 块图片 URL 映射关系。对于块类型为图片的块，block_id_to_image_urls 中会包含该块的 block_id 和对应的 image_url。通过 image_url，可以访问该图片资源。
    /// </summary>
    [JsonPropertyName("block_id_to_image_urls")]
    public BlockImageUrl[] BlockIdToImageUrls { get; set; } = [];
}

/// <summary>
/// 块图片 URL 映射关系
/// </summary>
public class BlockImageUrl
{
    /// <summary>
    /// 块的唯一标识。每个块都有一个唯一的 block_id，作为该块的标识符。通过 block_id，可以在文档中定位到具体的块，并对其进行操作，如修改、删除等。
    /// </summary>
    [JsonPropertyName("block_id")]
    public string BlockId { get; set; } = string.Empty;

    /// <summary>
    /// 图片 URL 地址。对于块类型为图片的块，block_id_to_image_urls 中会包含该块的 block_id 和对应的 image_url。通过 image_url，可以访问该图片资源。
    /// </summary>
    [JsonPropertyName("image_url")]
    public string? ImageUrl { get; set; }
}