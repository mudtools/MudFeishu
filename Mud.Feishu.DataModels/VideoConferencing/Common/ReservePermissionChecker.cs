// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.VideoConferencing;


/// <summary>
/// <para>权限检查器列表，权限检查器之间为"逻辑或"的关系（即 有一个为true则拥有该权限）</para>
/// </summary>
[HttpJsonSerializable(SerializerClassName = "VideoConferencing")]
public class ReservePermissionChecker
{
    /// <summary>
    /// <para>检查字段类型</para>
    /// <para>必填：是</para>
    /// <para>示例值：1</para>
    /// <para>可选值：<list type="bullet">
    /// <item>1：用户ID（check_list填入用户ID）</item>
    /// <item>2：用户类型（check_list可选值有 "1"：飞书用户、 "2"：rooms用户、 "6"：pstn用户、 "7"：sip用户）</item>
    /// <item>3：租户ID（check_list填入租户tenant_key）</item>
    /// </list></para>
    /// </summary>
    [JsonPropertyName("check_field")]
    public int CheckField { get; set; }

    /// <summary>
    /// <para>检查方式</para>
    /// <para>必填：是</para>
    /// <para>示例值：1</para>
    /// <para>可选值：<list type="bullet">
    /// <item>1：在check_list中为有权限（白名单）</item>
    /// <item>2：不在check_list中为有权限（黑名单）</item>
    /// </list></para>
    /// </summary>
    [JsonPropertyName("check_mode")]
    public int CheckMode { get; set; }

    /// <summary>
    /// <para>检查字段列表（根据check_field的类型填入对应内容）</para>
    /// <para>必填：是</para>
    /// <para>示例值："ou_3ec3f6a28a0d08c45d895276e8e5e19b"</para>
    /// </summary>
    [JsonPropertyName("check_list")]
    public string[] CheckList { get; set; } = [];
}
