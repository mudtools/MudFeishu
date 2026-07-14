// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.VideoConferencing;

/// <summary>
/// <para>授权对象列表</para>
/// </summary>
[HttpJsonSerializable(SerializerClassName = "VideoConferencing")]
public class RecordingPermissionObject
{
    /// <summary>
    /// <para>授权对象ID</para>
    /// <para>必填：否</para>
    /// <para>示例值：ou_3ec3f6a28a0d08c45d895276e8e5e19b</para>
    /// </summary>
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    /// <summary>
    /// <para>授权对象类型</para>
    /// <para>必填：是</para>
    /// <para>示例值：1</para>
    /// <para>可选值：<list type="bullet">
    /// <item>1：用户授权（id字段填入用户ID）</item>
    /// <item>2：群组授权（id字段填入群组open_chat_id）</item>
    /// <item>3：租户内授权（id字段不填）</item>
    /// <item>4：公网授权（id字段不填）</item>
    /// </list></para>
    /// </summary>
    [JsonPropertyName("type")]
    public int Type { get; set; }

    /// <summary>
    /// <para>权限</para>
    /// <para>必填：是</para>
    /// <para>示例值：1</para>
    /// <para>可选值：<list type="bullet">
    /// <item>1：查看</item>
    /// </list></para>
    /// </summary>
    [JsonPropertyName("permission")]
    public int Permission { get; set; }
}
