// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Mail;

/// <summary>
/// <para>多实体搜索请求体</para>
/// <para>接口详细文档请参见：<see href="https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/reference/mail-v1/multi_entity/search"/></para>
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Mail")]
public class SearchMultiEntityRequest
{
    /// <summary>
    /// <para>搜索关键词</para>
    /// <para>必填：是</para>
    /// <para>示例值：周会</para>
    /// <para>最大长度：50</para>
    /// <para>最小长度：1</para>
    /// </summary>
    [JsonPropertyName("query")]
    public string Query { get; set; } = string.Empty;

    /// <summary>
    /// <para>获取的数据条数</para>
    /// <para>必填：否</para>
    /// <para>示例值：20</para>
    /// <para>默认值：20</para>
    /// <para>最大值：20</para>
    /// <para>最小值：1</para>
    /// </summary>
    [JsonPropertyName("size")]
    public int? Size { get; set; }
}
