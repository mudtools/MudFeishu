// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using System.Text.Json;

namespace Mud.Feishu.AI.FeishuTools.Tools;

/// <summary>
/// <b>R5 / B-2 + F-10</b>：字节型下载的"<b>错误体伪装成文件</b>"防线。
/// </summary>
/// <remarks>
/// <para>
/// <b>问题</b>：SDK 的 15 处字节型下载（<c>DownloadFileAsync → Task&lt;byte[]?&gt;</c> 等）取自
/// <c>HttpContent.ReadAsByteArrayAsync</c> —— <b>HTTP 200 + JSON 错误体会被当作文件内容返回</b>。
/// 该风险在 SDK 注释里被明确写出，但注释不能执行；而"落盘"这一步一旦做了，
/// 磁盘上就多了一个<b>内容是错误 JSON 的假文件</b>，且调用方通常要等到读它时才发现。
/// </para>
/// <para>
/// <b>为什么判定放在本类而不是留给宿主</b>：宿主实现 <c>IFeishuAttachmentStager</c> 时很容易
/// 只做"大小上限 + 域名白名单"而漏掉这一步（它甚至可能拿不到 <c>Content-Type</c>）。
/// 把判定做成<b>纯函数</b>放在工具层调用，意味着<b>字节在交给宿主之前就已经过关</b> ——
/// 防线不依赖宿主的实现质量，且可被单元测试锁死。
/// </para>
/// <para>
/// <b>⚠️ 豁免的取舍（B-2 原先"无豁免机制"的解法）</b>：单纯的"内容是 JSON 就拒绝"会误杀
/// <b>合法的 JSON 文件下载</b>（例如下载一份 <c>.json</c> 配置）。故判据不是"是不是 JSON"，
/// 而是"<b>是不是飞书的错误信封</b>"：
/// <list type="number">
/// <item>必须能按 JSON 解析（否则视为普通二进制，放过）；</item>
/// <item>必须是<b>对象</b>（顶层 <c>{…}</c>）；</item>
/// <item>必须含 <c>code</c> 字段且为<b>非 0 整数</b> —— 这是错误信封与"合法 JSON 文件"的分界。</item>
/// </list>
/// 三步全中才判为错误体，因此合法 JSON 文件里的 <c>code</c>（若存在）只要为 0 或不叫 <c>code</c> 就不会被误杀。
/// 调用方若确知自己在下载<b>错误信封形状</b>的 JSON（极少见），仍可经
/// <c>expected_content_type</c> 显式豁免 —— 但那是<b>声明</b>，会被审计，不是静默绕过。
/// </para>
/// <para>
/// <b>AOT 安全</b>：只用 <see cref="Utf8JsonReader"/>（栈上、零反射、零中间对象），
/// 不构建 <c>JsonDocument</c>，故对超大响应也不会放大内存。
/// </para>
/// </remarks>
internal static class DownloadedContentGuard
{
    /// <summary>
    /// 判定字节内容是否为"<b>被当作文件返回的 JSON 错误体</b>"。
    /// </summary>
    /// <param name="content">下载得到的字节。</param>
    /// <param name="errorCode">解析到的非 0 <c>code</c>；非错误体时为 0。</param>
    /// <param name="errorMessage">解析到的 <c>msg</c>（可空）。</param>
    /// <returns>是错误体时为 <see langword="true"/>。</returns>
    public static bool LooksLikeJsonErrorBody(ReadOnlySpan<byte> content, out int errorCode, out string? errorMessage)
    {
        errorCode = 0;
        errorMessage = null;

        var start = SkipBomAndWhitespace(content);
        if (start >= content.Length || content[start] != (byte)'{')
        {
            // 不是对象（二进制/数组/纯文本）⇒ 与"错误信封"无关，交给宿主按普通内容处理。
            return false;
        }

        try
        {
            // 只读顶层字段：进入嵌套结构一律 Skip，既省时也避免把子对象的 code 误当信封字段。
            var reader = new Utf8JsonReader(content[start..], isFinalBlock: true, state: default);
            var depth = 0;

            while (reader.Read())
            {
                switch (reader.TokenType)
                {
                    case JsonTokenType.StartObject:
                        depth++;
                        break;

                    case JsonTokenType.EndObject:
                        depth--;
                        break;

                    case JsonTokenType.PropertyName when depth == 1:
                        var name = reader.ValueSpan;
                        reader.Read();

                        if (name.SequenceEqual("code"u8))
                        {
                            errorCode = ReadErrorCode(ref reader);
                        }
                        else if (name.SequenceEqual("msg"u8) && reader.TokenType == JsonTokenType.String)
                        {
                            errorMessage = reader.GetString();
                        }
                        else if (reader.TokenType is JsonTokenType.StartObject or JsonTokenType.StartArray)
                        {
                            reader.Skip();
                        }

                        break;

                    default:
                        break;
                }
            }
        }
        catch (JsonException)
        {
            // 有意静默（守卫白名单）：本类是 **internal 纯判定函数**，无日志面可用；
            // 且此处异常**完全不表示"出错了"**——"内容不是合法 JSON" 正是本函数要回答的问题之一
            // （二进制/截断内容的正常路径）。当成异常上报会让每次普通文件下载都刷一条噪声。
            //
            // 后果亦 fail-open 且可解释：判不了 ⇒ 放过 ⇒ 由宿主的大小/域名/MIME 策略继续把关，
            // 因此静默不会让"错误体落盘"这条防线出现**无人负责**的缺口。
            errorCode = 0;
            errorMessage = null;
            return false;
        }

        return errorCode != 0;
    }

    /// <summary>
    /// <b>落盘前的放行判定</b>：把"是否错误体"与"调用方的显式豁免"合成一个结论。
    /// </summary>
    /// <param name="content">下载得到的字节。</param>
    /// <param name="expectedContentType">
    /// 调用方声明的期望内容类型（可空）。为 JSON 类型时表示"我知道这个文件本来就是 JSON"，据此豁免。
    /// </param>
    /// <param name="reason">拒绝原因（人类可读，供结构化错误回填）；放行时为 <see langword="null"/>。</param>
    /// <returns>应当<b>拒绝落盘</b>时为 <see langword="true"/>。</returns>
    /// <remarks>
    /// <b>为什么豁免只认 <c>expected_content_type</c> 而不是自动识别</b>：自动识别（例如"文件名以 .json 结尾"）
    /// 会让<b>服务端返回的错误体</b>与<b>用户想下载的 JSON</b> 无法区分 —— 而两者在"文件名"上可以完全一致。
    /// 唯一可靠的信息源是<b>调用方对这次下载的意图</b>，故只认显式声明。
    /// </remarks>
    public static bool ShouldRejectForJsonErrorBody(
        ReadOnlySpan<byte> content,
        string? expectedContentType,
        out string? reason)
    {
        reason = null;

        if (IsJsonContentType(expectedContentType))
        {
            // 调用方已声明"这次要的就是 JSON"：即便内容是错误信封形状，也按其意图放行。
            return false;
        }

        if (!LooksLikeJsonErrorBody(content, out var code, out var message))
        {
            return false;
        }

        reason =
            $"下载内容不是文件而是平台的 JSON 错误体（code={code}"
            + (string.IsNullOrWhiteSpace(message) ? string.Empty : $"，msg={message}")
            + "）。已拒绝落盘，避免磁盘上出现内容为错误信的假文件。"
            + "若你确实要下载一份 JSON 文件，请显式传入 expected_content_type=application/json。";
        return true;
    }

    /// <summary>
    /// 读取 <c>code</c> 字段为整数；非数值形态返回 0（不构成"错误信封"判据）。
    /// </summary>
    /// <remarks>
    /// <b>为什么也接受数值字符串</b>：部分接口的 <c>code</c> 以 <c>"1770001"</c> 形态返回。
    /// 本判据是<b>安全防线</b>：漏判 = 磁盘上多一个内容是错误信的假文件（静默、难发现），
    /// 误判 = 下载失败（响亮、可恢复）。因此倾向"宁可响亮失败"，
    /// 但仍以"顶层对象 + 非零数值 code"为界，保证合法 JSON 文件不被整体误杀。
    /// </remarks>
    private static int ReadErrorCode(ref Utf8JsonReader reader)
        => reader.TokenType switch
        {
            JsonTokenType.Number when reader.TryGetInt32(out var numeric) => numeric,
            JsonTokenType.String when int.TryParse(
                reader.GetString(), System.Globalization.NumberStyles.Integer,
                System.Globalization.CultureInfo.InvariantCulture, out var textual) => textual,
            _ => 0,
        };

    /// <summary>内容类型是否为 JSON 系（含 <c>+json</c> 后缀，如 <c>application/vnd.api+json</c>）。</summary>
    /// <remarks>
    /// 用 <see cref="string"/> 而非 <c>ReadOnlySpan&lt;char&gt;</c>：后者带
    /// <see cref="StringComparison"/> 的 <c>Equals</c>/<c>EndsWith</c> 重载在 <b>netstandard2.0</b>
    /// 上不存在（本工程多目标，net8/net10 能过而 ns2.0 编译失败）。
    /// Content-Type 是短头部字符串，这点分配可忽略。
    /// </remarks>
    private static bool IsJsonContentType(string? contentType)
    {
        if (string.IsNullOrWhiteSpace(contentType))
        {
            return false;
        }

        var value = contentType.Trim();

        // 去掉 "; charset=utf-8" 之类的参数。
        var separator = value.IndexOf(';');
        if (separator >= 0)
        {
            value = value.Substring(0, separator).TrimEnd();
        }

        return string.Equals(value, "application/json", StringComparison.OrdinalIgnoreCase)
               || string.Equals(value, "text/json", StringComparison.OrdinalIgnoreCase)
               || value.EndsWith("+json", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>跳过 UTF-8 BOM 与前导空白，返回首个有效字节的下标。</summary>
    private static int SkipBomAndWhitespace(ReadOnlySpan<byte> content)
    {
        var index = 0;

        if (content.Length >= 3 && content[0] == 0xEF && content[1] == 0xBB && content[2] == 0xBF)
        {
            index = 3;
        }

        while (index < content.Length && content[index] is (byte)' ' or (byte)'\t' or (byte)'\r' or (byte)'\n')
        {
            index++;
        }

        return index;
    }
}
