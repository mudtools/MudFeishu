// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using System.Text.RegularExpressions;

namespace Mud.Feishu.Abstractions.Utilities;

/// <summary>
/// 日志清洗工具（WHF-06）
/// 对来自外部的输入（nonce、eventId、IP、请求头等）在写入日志前做统一清洗：
/// 替换控制符/换行等不安全字符，防止日志注入与日志伪造；超长截断防止日志膨胀。
/// </summary>
public static class LogSanitizer
{
    /// <summary>
    /// 不安全字符模式：白名单之外的字符（控制符、换行、引号、花括号等）一律替换。
    /// 白名单：单词字符（字母/数字/下划线）、连字符、等号、点号。
    /// </summary>
    private static readonly Regex UnsafeChars = new(@"[^\w\-=.]", RegexOptions.Compiled);

    /// <summary>默认最大保留长度。</summary>
    private const int DefaultMaxLength = 64;

    /// <summary>
    /// 清洗外部输入用于日志：替换控制符/换行，超长截断。
    /// </summary>
    /// <param name="value">原始值（可为 null/空）。</param>
    /// <param name="maxLength">最大保留长度，超出部分截断并追加省略号。</param>
    /// <returns>清洗后的安全字符串；输入为空时返回空字符串。</returns>
    public static string Clean(string? value, int maxLength = DefaultMaxLength)
    {
        if (string.IsNullOrEmpty(value))
            return string.Empty;

        var cleaned = UnsafeChars.Replace(value, "?");
        if (cleaned.Length <= maxLength)
            return cleaned;

        return cleaned.Substring(0, maxLength) + "…";
    }
}
