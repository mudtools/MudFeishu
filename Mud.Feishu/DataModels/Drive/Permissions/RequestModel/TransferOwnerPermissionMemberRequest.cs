// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Drive;

/// <summary>
/// 转移云文档所有者请求体
/// </summary>
public class TransferOwnerPermissionMemberRequest
{
    /// <summary>
    /// <para>文件所有者的 ID 类型</para>
    /// <para>必填：是</para>
    /// <para>示例值：openid</para>
    /// <para>可选值：<list type="bullet">
    /// <item>email：飞书邮箱</item>
    /// <item>openid：开放平台 Open ID。参考[如何获取不同的用户 ID](https://open.feishu.cn/document/home/user-identity-introduction/open-id)</item>
    /// <item>userid：用户 ID。获取方式参考[如何获取不同的用户 ID](https://open.feishu.cn/document/home/user-identity-introduction/open-id)</item>
    /// </list></para>
    /// </summary>
    [JsonPropertyName("member_type")]
    public string MemberType { get; set; } = string.Empty;

    /// <summary>
    /// <para>文件所有者的 ID，与文件所有者的 ID 类型需要对应。</para>
    /// <para>必填：是</para>
    /// <para>示例值：ou_67e5ecb64ce1c0bd94612c17999abcef</para>
    /// </summary>
    [JsonPropertyName("member_id")]
    public string MemberId { get; set; } = string.Empty;
}