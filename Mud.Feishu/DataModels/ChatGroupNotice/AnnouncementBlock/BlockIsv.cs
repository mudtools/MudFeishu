// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.ChatGroupNotice;

/// <summary>
/// <para>三方 Block</para>
/// </summary>
public class BlockIsv
{
    /// <summary>
    /// <para>团队互动应用唯一ID</para>
    /// <para>必填：否</para>
    /// <para>示例值：7056882725002051603</para>
    /// </summary>
    [JsonPropertyName("component_id")]
    public string? ComponentId { get; set; }

    /// <summary>
    /// <para>团队互动应用类型，比如信息收集"blk_5f992038c64240015d280958"</para>
    /// <para>必填：否</para>
    /// <para>示例值：blk_5f992038c64240015d280958</para>
    /// </summary>
    [JsonPropertyName("component_type_id")]
    public string? ComponentTypeId { get; set; }
}
