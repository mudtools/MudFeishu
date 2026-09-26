// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Lingo;

/// <summary>
/// 飞书词典草稿（draft）。草稿并非词条，而是通过 API 发起创建新词条或更新现有词条的申请；
/// 词典管理员审核通过后，草稿将变为新的词条或覆盖已有词条。
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Lingo")]
public class LingoDraft
{
    /// <summary>
    /// <para>草稿 ID</para>
    /// <para>必填：否（仅查询结果返回）</para>
    /// <para>示例值：7241543272228814852</para>
    /// </summary>
    [JsonPropertyName("draft_id")]
    public string? DraftId { get; set; }

    /// <summary>
    /// <para>草稿对应的词条内容</para>
    /// <para>必填：否（仅查询结果返回）</para>
    /// </summary>
    [JsonPropertyName("entity")]
    public LingoEntity? Entity { get; set; }
}
