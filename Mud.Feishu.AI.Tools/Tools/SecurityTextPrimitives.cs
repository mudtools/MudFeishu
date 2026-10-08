// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using System.Text.RegularExpressions;

namespace Mud.Feishu.AI.Tools.Tools;

/// <summary>
/// R3-05：安全文本基线单源——敏感键 / 控制字符判定 / 正则超时统一来源。
/// </summary>
/// <remarks>
/// <para><b>判据隔离</b>（防误伤）：</para>
/// <list type="bullet">
///   <item><b>出站脱敏键</b>（<see cref="CredentialKeys"/>）：JSON 键名精确匹配 + 非 JSON 形态（key=value）。
///   <b>不含</b> <c>open_id</c>/<c>chat_id</c>/<c>app_token</c>/<c>page_token</c> 等标识类字段——
///   这些是飞书 API 响应的合法结构字段，掩码会让模型无法读取正常的分页/标识信息。</item>
///   <item><b>入站审计键</b>（<see cref="ToolArgsDigester.SensitiveKeys"/>）：子串匹配（宁可多掩），
///   属独立口径——本轮<b>不合并</b>，仅加交叉引用注释。</item>
/// </list>
/// <para><b>控制字符判定</b>：出站剥离（<see cref="ToolResultSanitizer"/>）与入站拒绝
/// （<see cref="ToolArgumentSanitizer"/>）共用同一判定表达式，但处置方向不同（有意保留差异）。</para>
/// </remarks>
internal static class SecurityTextPrimitives
{
    /// <summary>正则匹配超时（统一止损阀；对齐 BitableFilterParser 既有取值）。</summary>
    public static readonly TimeSpan MatchTimeout = TimeSpan.FromSeconds(2);

    /// <summary>
    /// 控制字符判定<b>单源</b>（出站剥离 / 入站拒绝共用同一事实）。
    /// </summary>
    /// <remarks>
    /// 剥离 C0/C1 控制字符（保留 \n / \r / \t）。控制字符对模型无信息价值，
    /// 却可被用来伪造工具输出的视觉结构（提示注入面的常见载体）。
    /// </remarks>
    public static bool IsControl(char ch)
        => (ch < ' ' && ch is not ('\n' or '\r' or '\t')) || ch == '\u007F' || (ch >= '\u0080' && ch <= '\u009F');

    /// <summary>
    /// 出站脱敏键集（<b>并集</b>语义）：JSON 键名精确匹配 + 非 JSON 形态（key=value / key: value）。
    /// </summary>
    /// <remarks>
    /// <para>在既有 13 项基础上新增 <c>api_key</c>/<c>apikey</c>/<c>credential</c>/<c>client_id</c>
    /// ——这些在入站审计 <see cref="ToolArgsDigester.SensitiveKeys"/> 中已覆盖，但出站侧此前漏网。</para>
    /// <para><b>不含</b> <c>open_id</c>/<c>chat_id</c>/<c>app_token</c>/<c>page_token</c>——
    /// 误伤会让模型无法读取正常的分页/标识信息。</para>
    /// </remarks>
    public static readonly string[] CredentialKeys =
    [
        "app_secret", "client_secret", "secret", "password", "passwd",
        "access_token", "refresh_token", "tenant_access_token", "user_access_token",
        "app_access_token", "authorization", "private_key", "encrypt_key", "verification_token",
        // R3-05 新增：补齐与入站审计键的差集。
        "api_key", "apikey", "credential", "client_id",
    ];

    /// <summary>
    /// 凭据键 → 值脱敏正则（JSON 形态：<c>"app_secret":"…"</c>）。
    /// </summary>
    public static readonly Regex SecretValuesJson = new(
        "\\\"(?<key>" + string.Join("|", CredentialKeys) + ")\\\"\\s*:\\s*\\\"[^\\\"]*\\\"",
        RegexOptions.Compiled | RegexOptions.IgnoreCase,
        MatchTimeout);

    /// <summary>
    /// 凭据键 → 值脱敏正则（非 JSON 形态：<c>app_secret=xxx</c> / <c>app_secret: xxx</c>）。
    /// </summary>
    public static readonly Regex SecretValuesFlat = new(
        @"\b(?<key>" + string.Join("|", CredentialKeys) + @")\s*[=:]\s*[^\s""&,]+",
        RegexOptions.Compiled | RegexOptions.IgnoreCase,
        MatchTimeout);
}
