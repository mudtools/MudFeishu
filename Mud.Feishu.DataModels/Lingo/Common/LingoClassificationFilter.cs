// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Lingo;

/// <summary>
/// 搜索词条时的分类筛选条件（classification_filter）
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Lingo")]
public class LingoClassificationFilter
{
    /// <summary>
    /// <para>需要获取的分类 ID 列表</para>
    /// <para>必填：否</para>
    /// <para>示例值：["7195482254012055580"]</para>
    /// </summary>
    [JsonPropertyName("include")]
    public string[]? Include { get; set; }

    /// <summary>
    /// <para>需要排除的分类 ID 列表</para>
    /// <para>必填：否</para>
    /// <para>示例值：["7195482254012055581"]</para>
    /// </summary>
    [JsonPropertyName("exclude")]
    public string[]? Exclude { get; set; }
}
