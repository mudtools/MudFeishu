// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.AI.Tools.Internal;

namespace Mud.Feishu.AI.Tools.Tests.Tools;

/// <summary>
/// 邮件 EML 头注入防护（R3-12）：<see cref="MailTools.BuildEml"/> 对<b>头字段</b>
/// （To/Cc/Bcc/Subject）拒绝任何 CR/LF，而<b>正文</b>仍允许换行。
/// </summary>
/// <remarks>
/// 为什么这里必须有一道独立的闸：全局入站净化（<c>ToolArgumentSanitizer.ValidateText</c>）
/// <b>有意</b>放行 CRLF/LF（正文、文档、消息文本都靠换行表达结构），
/// 因此 <c>Subject: "x\r\nBcc: attacker@evil"</c> 能穿过入站校验直达 EML 组装——
/// 若组装时不拒，则模型可凭空插入收件人（SMTP 头注入）。
/// </remarks>
public class MailToolsTests
{
    [Fact]
    public void BuildEml_ShouldRejectCrlf_InHeaderFields()
    {
        var subjectAct = () => MailTools.BuildEml(
            ["alice@example.com"], null, null, "会议纪要\r\nBcc: attacker@evil.com", "正文");
        subjectAct.Should().Throw<ArgumentException>(
            "Subject 中的 CRLF 可凭空插入 Bcc 头（SMTP 头注入）").WithMessage("*换行*");

        var toAct = () => MailTools.BuildEml(
            ["alice@example.com\r\nBcc: attacker@evil.com"], null, null, "会议纪要", "正文");
        toAct.Should().Throw<ArgumentException>("To 中的 CRLF 同样可注入头字段").WithMessage("*换行*");
    }

    [Fact]
    public void BuildEml_ShouldAllowMultilineBody_WhenHeadersAreClean()
    {
        var eml = MailTools.BuildEml(["alice@example.com"], null, null, "会议纪要", "第一行\n第二行");

        eml.Should().StartWith("Content-Type: text/plain; charset=\"utf-8\"\r\n");
        eml.Should().Contain("To: <alice@example.com>\r\n");
        eml.Should().Contain("Subject: 会议纪要\r\n");
        eml.Should().EndWith("\r\n第一行\n第二行", "正文仍允许 LF——换行禁忌只施加于头字段");
    }
}