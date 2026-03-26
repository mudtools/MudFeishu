// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Spreadsheets;

/// <summary>可编辑保护范围的用户 ID</summary>
public class SheetEditor
{
    /// <summary>
    /// <para>用户 ID 类型，可选值：</para>
    /// <para>- userId：标识一个用户在某个租户内的身份。同一个用户在租户 A 和租户 B 内的 User ID 是不同的。在同一个租户内，一个用户的 User ID 在所有应用（包括商店应用）中都保持一致。User ID 主要用于在不同的应用间打通用户数据。</para>
    /// <para>- openId：标识一个用户在某个应用中的身份。同一个用户在不同应用中的 Open ID 不同。</para>
    /// <para>- unionId：标识一个用户在某个应用开发商下的身份。同一用户在同一开发商下的应用中的 Union ID 是相同的，在不同开发商下的应用中的 Union ID 是不同的。通过 Union ID，应用开发商可以把同个用户在多个应用中的身份关联起来。</para>
    /// <para>必填：是</para>
    /// </summary>
    [JsonPropertyName("memberType")]
    public string MemberType { get; set; } = string.Empty;

    /// <summary>
    /// <para>用户 ID，类型由 `memberType` 决定。</para>
    /// <para>必填：是</para>
    /// </summary>
    [JsonPropertyName("memberId")]
    public string MemberId { get; set; } = string.Empty;
}