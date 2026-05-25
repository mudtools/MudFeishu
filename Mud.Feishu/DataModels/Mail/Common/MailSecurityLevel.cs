// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Mail;

/// <summary>
/// <para>安全信息</para>
/// </summary>
public class MailSecurityLevel
{
    /// <summary>
    /// <para>是否风险邮件</para>
    /// <para>必填：否</para>
    /// <para>示例值：false</para>
    /// </summary>
    [JsonPropertyName("is_risk")]
    public bool? IsRisk { get; set; }

    /// <summary>
    /// <para>风险邮件等级</para>
    /// <para>必填：否</para>
    /// <para>示例值：WARNING</para>
    /// <para>可选值：<list type="bullet">
    /// <item>WARNING：警告</item>
    /// <item>DANGER：危险</item>
    /// <item>INFO：提示</item>
    /// </list></para>
    /// </summary>
    [JsonPropertyName("risk_banner_level")]
    public string? RiskBannerLevel { get; set; }

    /// <summary>
    /// <para>风险邮件原因</para>
    /// <para>必填：否</para>
    /// <para>示例值：IMPERSONATE_DOMAIN</para>
    /// <para>可选值：<list type="bullet">
    /// <item>NO_REASON：未知</item>
    /// <item>IMPERSONATE_DOMAIN：相似域名仿冒</item>
    /// <item>IMPERSONATE_KP_NAME：KP姓名仿冒</item>
    /// <item>UNAUTH_EXTERNAL：未认证外部域名</item>
    /// <item>MALICIOUS_URL：恶意链接</item>
    /// <item>MALICIOUS_ATTACHMENT：高危附件</item>
    /// <item>PHISHING：钓鱼邮件</item>
    /// <item>IMPERSONATE_PARTNER：仿冒合作伙伴</item>
    /// <item>EXTERNAL_ENCRYPTION_ATTACHMENT：外部邮件携带加密附件</item>
    /// </list></para>
    /// </summary>
    [JsonPropertyName("risk_banner_reason")]
    public string? RiskBannerReason { get; set; }

    /// <summary>
    /// <para>发件人是否外部邮件</para>
    /// <para>必填：否</para>
    /// <para>示例值：false</para>
    /// </summary>
    [JsonPropertyName("is_header_from_external")]
    public bool? IsHeaderFromExternal { get; set; }

    /// <summary>
    /// <para>代发或伪造邮件展示SPF或DKIM域名</para>
    /// <para>必填：否</para>
    /// <para>示例值：larksuite.com</para>
    /// </summary>
    [JsonPropertyName("via_domain")]
    public string? ViaDomain { get; set; }

    /// <summary>
    /// <para>垃圾邮件原因</para>
    /// <para>必填：否</para>
    /// <para>示例值：USER_REPORT</para>
    /// <para>可选值：<list type="bullet">
    /// <item>USER_REPORT：用户曾标记邮件是垃圾邮件</item>
    /// <item>USER_BLOCK：用户曾将发件人的邮件标记为垃圾邮件</item>
    /// <item>ANTI_SPAM：系统判为垃圾邮件</item>
    /// <item>USER_RULE：命中收信规则进入垃圾邮件</item>
    /// <item>BLOCK_DOMIN：用户已拦截来自该域名的邮件</item>
    /// <item>BLOCK_ADDRESS：用户已拦截来自该邮件地址的邮件</item>
    /// </list></para>
    /// </summary>
    [JsonPropertyName("spam_banner_type")]
    public string? SpamBannerType { get; set; }

    /// <summary>
    /// <para>命中的收信规则ID</para>
    /// <para>必填：否</para>
    /// <para>示例值：7618365627924925388</para>
    /// </summary>
    [JsonPropertyName("spam_user_rule_id")]
    public string? SpamUserRuleId { get; set; }

    /// <summary>
    /// <para>命中用户黑名单的地址或域名信息</para>
    /// <para>必填：否</para>
    /// <para>示例值：larksuite.com</para>
    /// </summary>
    [JsonPropertyName("spam_banner_info")]
    public string? SpamBannerInfo { get; set; }
}
