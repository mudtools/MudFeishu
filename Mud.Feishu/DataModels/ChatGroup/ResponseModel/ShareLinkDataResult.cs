// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.ChatGroup;

/// <summary>
/// 分享链接数据结果
/// <para>表示分享链接的基本信息，包含分享链接、过期时间等属性</para>
/// </summary>
public class ShareLinkDataResult
{
    /// <summary>
    /// 分享链接
    /// <para>用于邀请用户加入群组的分享链接URL</para>
    /// <para>用户可以通过此链接直接加入对应的群组</para>
    /// </summary>
    [JsonPropertyName("share_link")]
    public string? ShareLink { get; set; }

    /// <summary>
    /// 过期时间
    /// <para>分享链接的过期时间戳或日期字符串</para>
    /// <para>超过此时间后，链接将失效，用户无法通过该链接加入群组</para>
    /// </summary>
    [JsonPropertyName("expire_time")]
    public string? ExpireTime { get; set; }

    /// <summary>
    /// 是否为永久链接
    /// <para>标识分享链接是否永久有效</para>
    /// <para>true：链接永久有效，不会过期</para>
    /// <para>false：链接有时效性，会在指定时间后过期</para>
    /// </summary>
    [JsonPropertyName("is_permanent")]
    public bool IsPermanent { get; set; }
}
