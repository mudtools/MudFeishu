// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Docx;


/// <summary>
/// 创建嵌套块响应体
/// </summary>
public class CreateDescendantBlockResult
{
    /// <summary>
    /// <para>所添加的孩子的 Block 信息</para>
    /// <para>必填：否</para>
    /// <para>示例值：111</para>
    /// </summary>
    [JsonPropertyName("children")]
    public Block[]? Childrens { get; set; }

    /// <summary>
    /// <para>当前提交的 Block 创建成功后文档的版本号</para>
    /// <para>必填：否</para>
    /// <para>示例值：1</para>
    /// </summary>
    [JsonPropertyName("document_revision_id")]
    public int? DocumentRevisionId { get; set; }

    /// <summary>
    /// <para>操作的唯一标识，更新请求中使用此值表示幂等的进行此次更新</para>
    /// <para>必填：否</para>
    /// <para>示例值：80bf5b2a-4dea-4c02-8a84-a0e682de463d</para>
    /// </summary>
    [JsonPropertyName("client_token")]
    public string? ClientToken { get; set; }

    /// <summary>
    /// <para>传入的临时 BlockID 与真实 BlockID 映射关系</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("block_id_relations")]
    public BlockIdRelation[]? BlockIdRelations { get; set; }


}
