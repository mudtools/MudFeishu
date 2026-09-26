// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Lingo;

/// <summary>
/// 词条的外部系统关联数据（outer_info），用于与其他平台的内容进行绑定
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Lingo")]
public class LingoOuterInfo
{
    /// <summary>
    /// <para>外部系统（不能包含中横线 "-"）</para>
    /// <para>必填：是</para>
    /// <para>长度范围：2 ～ 32 字符</para>
    /// <para>示例值：星云</para>
    /// </summary>
    [JsonPropertyName("provider")]
    public string? Provider { get; set; }

    /// <summary>
    /// <para>词条在外部系统中对应的唯一 ID（不能包含中横线 "-"），需保证与词典词条唯一对应</para>
    /// <para>必填：是</para>
    /// <para>长度范围：1 ～ 64 字符</para>
    /// <para>示例值：12345abc</para>
    /// </summary>
    [JsonPropertyName("outer_id")]
    public string? OuterId { get; set; }
}
