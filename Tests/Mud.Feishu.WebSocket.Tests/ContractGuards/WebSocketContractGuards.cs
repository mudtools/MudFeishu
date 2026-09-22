// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using System.Text.RegularExpressions;
using FluentAssertions;

namespace Mud.Feishu.WebSocket.Tests.ContractGuards;

/// <summary>
/// WebSocket 模块架构不变量守卫（R2 / WS2-13）。
/// </summary>
/// <remarks>
/// 沿用 <c>Tests/Mud.Feishu.Abstractions.Tests/ContractGuards/ConfigSurfaceContractGuards.cs</c> 的范式：
/// 读源码文本 + 精确 token 断言。**目的是防"已修项复活"**——本轮 12 项修复里有多项是
/// "机制存在但语义未生效"（R1 WS-16 即此产物），单靠行为用例无法覆盖"将来有人把守卫逻辑改回去"。
/// <para>
/// <b>设计纪律</b>（对应方案 §8.2 的风险项"守卫因源码格式误报"）：
/// <list type="bullet">
/// <item>一律用「关键 token + 白名单」，不用整段正则；</item>
/// <item>方法体提取先剥离字符串字面量（避免字面量中的大括号破坏括号匹配）；</item>
/// <item>断言失败信息必须写明"违反了哪条不变量、为什么"。</item>
/// </list>
/// </para>
/// <para>
/// 注意：本测试工程 <c>GenerateDocumentationFile=false</c>，源码守卫类无需为每个成员补 XML 注释。
/// </para>
/// </remarks>
[Trait("Category", "ContractGuard")]
public class WebSocketContractGuards
{
    private const string ModuleDirectoryName = "Mud.Feishu.WebSocket";

    #region I13：连接终止路径穷尽占位

    /// <summary>
    /// I13：每条"使 socket 不再被读取"的路径都必须经过原子占位，且主动断开必须先占位后关闭。
    /// </summary>
    [Fact]
    public void ConnectionTerminationPaths_ShouldAllClaimDisconnected()
    {
        var source = ReadModuleSource("Core/WebSocketConnectionManager.cs");

        // ① 接收循环因取消退出（P0-1 的核心缺口，改造前只记日志）
        var receiveLoopBody = ExtractMethodBody(source, "StartReceivingAsync(Func<ArraySegment<byte>, WebSocketReceiveResult, Task> messageHandler");
        receiveLoopBody.Should().Contain("NotifyDisconnected",
            "I13：接收循环的取消分支（socket 仍 Open）必须补发断线声明，否则形成无读循环/无事件的僵尸连接");
        receiveLoopBody.Should().Contain("OperationCanceledException",
            "I13：取消分支必须显式存在——它是本轮 P0-1 的落点");

        // ② 服务端关闭帧
        ExtractMethodBody(source, "HandleCloseMessageAsync(WebSocketReceiveResult result")
            .Should().Contain("NotifyDisconnected", "I13：服务端关闭帧路径必须占位");

        // ③ 客户端主动断开：必须先占位、后关闭握手
        var disconnectBody = ExtractMethodBody(source, "DisconnectCoreAsync(CancellationToken cancellationToken)");
        var claimIndex = disconnectBody.IndexOf("TryClaimDisconnected", StringComparison.Ordinal);
        var closeIndex = disconnectBody.IndexOf("CloseAsync", StringComparison.Ordinal);

        claimIndex.Should().BeGreaterThanOrEqualTo(0, "I13：主动断开必须经过 TryClaimDisconnected");
        closeIndex.Should().BeGreaterThanOrEqualTo(0, "I13：主动断开必须执行关闭握手");
        claimIndex.Should().BeLessThan(closeIndex,
            "I13 顺序约束：主动断开必须先占位、后关闭握手。顺序颠倒会让接收循环在与关闭握手的竞态中" +
            "抢先占位（事件归属错误），或双方都判定对方已占位而导致事件一次都不派发");

        // ④⑤ 释放路径（同步/异步）
        ExtractMethodBody(source, "ValueTask DisposeAsync()", useLastOccurrence: true)
            .Should().Contain("TryClaimDisconnected", "I13：DisposeAsync 也是终止路径，必须参与占位（否则连接计数永不归零）");
        ExtractMethodBody(source, "void Dispose()", useLastOccurrence: true)
            .Should().Contain("TryClaimDisconnected", "I13：Dispose 同理");
    }

    #endregion

    #region I14：接收循环唯一性由原子占位保证

    /// <summary>
    /// I14：公开 <c>StartReceivingAsync</c> 必须用原子占位做唯一性判定，并把循环任务登记到 <c>_receiveTask</c>。
    /// </summary>
    [Fact]
    public void ReceiveLoopStart_ShouldUseAtomicPlaceholder()
    {
        var source = ReadModuleSource("FeishuWebSocketClient.cs");
        var body = ExtractMethodBody(source, "public async Task StartReceivingAsync(CancellationToken cancellationToken = default)");

        body.Should().Contain("Interlocked.CompareExchange",
            "I14：接收循环唯一性必须由原子占位（Interlocked.CompareExchange）保证——" +
            "改造前读 _receiveTask 的 check-then-use 在两类窄窗口下失效");
        body.Should().Contain("_receiveTask",
            "I14：本路径同样必须登记循环任务（停机等待与存活判定都依赖它）");
        body.Should().NotContain("_receiveTask is { IsCompleted: false }",
            "I14：不得回退为\"先检查后使用\"地读 _receiveTask 作为唯一判定依据");
    }

    /// <summary>
    /// WS2-02：弃用标记必须同时存在于接口与实现（只标实现类时，经接口调用的宿主看不到任何警告）。
    /// </summary>
    [Fact]
    public void StartReceivingAsync_ShouldBeMarkedObsolete_OnInterfaceAndImplementation()
    {
        ReadModuleSource("Interfaces/IFeishuWebSocketClient.cs").Should()
            .MatchRegex(@"\[Obsolete\([^\)]*\)\]\s*Task StartReceivingAsync",
                "WS2-02：接口上的 StartReceivingAsync 必须带 [Obsolete]，否则经 IFeishuWebSocketClient 调用的宿主收不到编译期警告");

        ReadModuleSource("FeishuWebSocketClient.cs").Should()
            .MatchRegex(@"\[Obsolete\([^\)]*\)\]\s*public async Task StartReceivingAsync",
                "WS2-02：实现类的 StartReceivingAsync 必须带 [Obsolete]");
    }

    #endregion

    #region I15：调用方令牌不构成连接生命周期

    /// <summary>
    /// I15：<c>_cancellationTokenSource</c> 不得由调用方令牌链接而来。
    /// </summary>
    [Fact]
    public void ConnectAsync_ShouldNotLinkCallerTokenToConnectionLifetime()
    {
        var source = ReadModuleSource("FeishuWebSocketClient.cs");
        var body = ExtractMethodBody(source, "public async Task ConnectAsync(WsEndpointResult endpoint, CancellationToken cancellationToken = default)");

        body.Should().NotContain("CreateLinkedTokenSource(cancellationToken)",
            "I15：连接生命周期令牌不得链接调用方令牌——链接会让短命令牌取消后连接静默停帧（socket 仍 Open、无断线通知）");
        body.Should().Contain("_cancellationTokenSource = new CancellationTokenSource()",
            "I15：连接生命周期必须使用客户端自持的 CancellationTokenSource");
    }

    #endregion

    #region I16：配置双向约束 + TimeSpan 钳制

    /// <summary>
    /// I16：配置/变量派生的 <see cref="TimeSpan"/> 不得直投 <c>CancellationTokenSource</c>。
    /// </summary>
    /// <remarks>
    /// <b>判定规则</b>：匹配 <c>new CancellationTokenSource(TimeSpan.FromXxx(&lt;arg&gt;))</c>，
    /// 仅当 <c>&lt;arg&gt;</c> 是**数字字面量**（编译期常量，如上例的 5/10 秒）时放行；
    /// 其他一切形态（变量、配置值、表达式）都要求改为
    /// <c>TimeSpanGuards.ClampToCancellationTokenRange(TimeSpan.FromXxx(...))</c>——
    /// 包裹后不再匹配本正则，即"钳制过 = 不报"。
    /// <para>
    /// 说明（守卫的边界）：本守卫不试图静态判定"某个 <see cref="TimeSpan"/> 变量是否已被钳制"——
    /// 那需要数据流分析，会引入误报（方案 §8.2 已登记该风险）。配置侧的**上界**由下一条守卫覆盖，
    /// 二者组合才构成 I16 的"双向约束"。
    /// </para>
    /// </remarks>
    [Fact]
    public void CancellationTokenSourceFromTimeSpan_ShouldBeClamped()
    {
        var offenders = new List<string>();

        foreach (var (path, source) in ReadAllModuleSources())
        {
            foreach (Match match in Regex.Matches(source, @"new CancellationTokenSource\(\s*TimeSpan\.From\w+\(\s*([^\)]*)\)"))
            {
                var argument = match.Groups[1].Value.Trim();

                // 数字字面量（含 C# 的 `_` 分隔符）= 编译期常量，不构成配置面风险
                if (Regex.IsMatch(argument, @"^[0-9_]+$"))
                {
                    continue;
                }

                offenders.Add($"{path}: {match.Value}");
            }
        }

        offenders.Should().BeEmpty(
            "I16：由配置/变量派生的 TimeSpan 必须先经 TimeSpanGuards.ClampToCancellationTokenRange 钳制" +
            "（CancellationTokenSource(TimeSpan) 在超过 int.MaxValue-1 毫秒时抛 ArgumentOutOfRangeException，" +
            "异常会被异步 catch 吞掉或表现为晦涩的启动失败）。" +
            $"违规点：{string.Join("、", offenders)}");

        // 重连窗口（配置派生值）必须显式钳制
        ExtractMethodBody(ReadModuleSource("FeishuWebSocketHostedService.cs"), "private void TryTriggerReconnect(string reason)")
            .Should().Contain("TimeSpanGuards.ClampToCancellationTokenRange",
                "I16：Reconnect.TotalBudget 是配置派生值，进入 CancellationTokenSource 前必须钳制");

        // 工具类自身不得使用 netstandard2.0 不可用的 Math.Clamp
        var guards = ReadModuleSource("Core/TimeSpanGuards.cs");
        guards.Should().Contain("ClampToCancellationTokenRange");
        guards.Should().Contain("ClampToTaskDelayRange");
        // 注意：断言的是**调用形态**（带左括号）——类级 remarks 中会出现 "Math.Clamp" 这一名词
        // （用于解释"为什么不能用它"），不带括号的纯文本断言会被自己的文档误伤。
        guards.Should().NotContain("Math.Clamp(",
            "REV-16：Math.Clamp 需要 netstandard2.1，本模块最低目标 netstandard2.0，必须用 Math.Min/Max");
    }

    /// <summary>
    /// I16：数值配置必须有**上界**校验（改造前只有下界）。
    /// </summary>
    [Fact]
    public void Validate_ShouldHaveUpperBounds()
    {
        var body = ExtractMethodBody(ReadModuleSource("Configuration/FeishuWebSocketOptions.cs"), "public void Validate()");

        // 每项都必须以"键名 > 上界"的形态出现（键名 + 比较运算符组合断言）
        var requiredUpperBounds = new[]
        {
            (Key: "Reconnect.TotalBudget", Hint: "超上界会超出 CancellationTokenSource 的计时器上界，使重连静默失效"),
            (Key: "Reconnect.BaseDelayMs", Hint: "超上界会在指数退避中溢出 TimeSpan"),
            (Key: "Reconnect.MaxDelayMs", Hint: "同上"),
            (Key: "MessageSizeLimits.MaxTextMessageSize", Hint: "超上界会使派生字节上限整型溢出"),
            (Key: "AuthTimeoutMs", Hint: "过长的单次等待属配置错误"),
            (Key: "AuthGateTimeoutMs", Hint: "同上"),
        };

        foreach (var (key, hint) in requiredUpperBounds)
        {
            var pattern = $@"{Regex.Escape(key)}\s*>";
            Regex.IsMatch(body, pattern).Should().BeTrue(
                $"I16：{key} 必须同时校验上界（'{key} > <上界>'）。{hint}");
        }
    }

    #endregion

    #region D5：入站报文不得原文入日志

    /// <summary>
    /// D5：入站报文的日志点必须有脱敏截断，且不得复活 P1-4 的全文直传形态。
    /// </summary>
    /// <remarks>
    /// 本守卫是**针对性回归守卫**（方案 §8.2：用关键 token + 白名单，不做通用代码规约检查）。
    /// 通用形态检查（"<c>{Message}</c> 后是否直传原始报文变量"）需要区分"异常消息（合法）"与
    /// "入站报文（禁止）"，纯文本规则无法可靠判定，故此处锁定本轮两个具体泄露点 + 正确范式存在性。
    /// </remarks>
    [Fact]
    public void InboundMessage_ShouldNotBeLoggedVerbatim()
    {
        var managerSource = ReadModuleSource("FeishuWebSocketManager.cs");

        managerSource.Should().Contain("LogSanitizer.CleanMessage(e.Message",
            "D5：入站消息日志必须走 LogSanitizer.CleanMessage（脱敏截断）");
        managerSource.Should().NotContain("{Message} (大小: {Size}字节",
            "D5 回归：FeishuWebSocketManager.OnClientMessageReceived 曾把入站报文全文（e.Message）写入 Debug 日志");

        var authSource = ReadModuleSource("Core/AuthenticationManager.cs");
        authSource.Should().Contain("LogSanitizer.CleanMessage(responseMessage",
            "D5：认证响应日志必须走 LogSanitizer.CleanMessage");
        authSource.Should().NotContain("解析认证响应失败: {Message}\", responseMessage",
            "D5 回归：AuthenticationManager 曾把认证响应全文（含令牌）写入日志");

        // F4：连接 URL 日志不得带 query
        ReadModuleSource("Core/WebSocketConnectionManager.cs")
            .Should().Contain("UriPartial.Path",
                "F4/D5：连接 URL 日志必须整体剥离 query（GetLeftPart(UriPartial.Path)），不得用按键名白名单脱敏");
    }

    #endregion

    #region P2-4：文档不得宣称 SeqID 去重具备隔离

    /// <summary>
    /// P2-4：三份权威语义文档不得复活"SeqID 去重含 scopeKey 隔离"的表述。
    /// </summary>
    [Fact]
    public void Documentation_ShouldNotClaimSeqIdScopeKey()
    {
        const string banned = "含 scopeKey 隔离";

        foreach (var fileName in new[] { "协议帧与ACK语义.md", "故障排查手册.md", "架构与并发模型.md" })
        {
            var path = Path.Combine(GetRepositoryRoot(), "documents", "WebSocket", fileName);
            File.Exists(path).Should().BeTrue($"权威语义文档必须存在：{fileName}");

            File.ReadAllText(path).Should().NotContain(banned,
                $"P2-4 回归：{fileName} 曾宣称 SeqID 去重具备 scopeKey 隔离——" +
                "实现为裸 SeqID 的进程内全局集合，无租户/应用维度隔离（真实隔离层是 event_id + AppKey）");
        }
    }

    #endregion

    #region F5：受控丢弃必须可计数且原因必须走常量

    /// <summary>
    /// F5：受控丢弃点必须调用计数 API，且原因参数**不得**使用裸字符串字面量。
    /// </summary>
    /// <remarks>
    /// 丢弃是"事件丢失"的前兆：只写日志无法形成可告警的时序信号（改造前正是只写日志）。
    /// <para>
    /// 第二条断言（禁止裸字符串）守护的是**告警规则的稳定性**：
    /// 告警/看板按 <c>reason</c> 的值聚合，若各调用点各写各的字面量，
    /// 改名或拼写漂移会静默改变指标序列，而单测无法发现（指标名不变、只是维度值变了）。
    /// </para>
    /// </remarks>
    [Fact]
    public void DiscardPoints_ShouldBeCounted_WithReasonConstants()
    {
        var expectedReasons = new[]
        {
            "FragmentSizeExceeded", "DrainBoundExceeded", "AuthGateTimeout", "ConcurrencyRejected"
        };

        var callSites = 0;

        foreach (var (path, source) in ReadAllModuleSources())
        {
            var matches = Regex.Matches(
                source,
                @"RecordWebSocketFramesDiscarded\s*\(\s*[^,]+,\s*(?<reason>[^,)]+)");

            foreach (Match match in matches)
            {
                callSites++;
                var reason = match.Groups["reason"].Value.Trim();

                reason.Should().StartWith("FeishuMetrics.DiscardReasons.",
                    $"F5：{path} 的丢弃计数必须使用 FeishuMetrics.DiscardReasons 常量（实际传入 '{reason}'），" +
                    "裸字符串会让告警规则的维度值随重构静默漂移");

                expectedReasons.Should().Contain(
                    reason.Substring("FeishuMetrics.DiscardReasons.".Length),
                    $"F5：{path} 使用了未登记的丢弃原因（未登记的值不会出现在告警规则与看板里）");
            }
        }

        callSites.Should().BeGreaterThanOrEqualTo(4,
            "F5：受控丢弃点（首帧超限 / 累积超限 / 排空上界 / 认证闸门 / 背压拒绝）至少要有 4 处计数调用，" +
            "否则说明有丢弃路径退化回了【只写日志】");
    }

    #endregion

    #region 源码读取与解析辅助

    private static string GetRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory != null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "Mud.Feishu.slnx")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException(
            $"未能从 {AppContext.BaseDirectory} 向上定位仓库根目录（Mud.Feishu.slnx）——守卫无法工作，宁可失败也不要假绿");
    }

    private static string ReadModuleSource(string relativePath)
    {
        var path = Path.Combine(GetRepositoryRoot(), ModuleDirectoryName, relativePath.Replace('/', Path.DirectorySeparatorChar));
        File.Exists(path).Should().BeTrue($"模块源码必须存在：{relativePath}");
        return File.ReadAllText(path);
    }

    private static IEnumerable<(string Path, string Source)> ReadAllModuleSources()
    {
        var root = Path.Combine(GetRepositoryRoot(), ModuleDirectoryName);
        var separator = Path.DirectorySeparatorChar;

        return Directory
            .EnumerateFiles(root, "*.cs", SearchOption.AllDirectories)
            .Where(path => !path.Contains($"{separator}obj{separator}", StringComparison.OrdinalIgnoreCase)
                        && !path.Contains($"{separator}bin{separator}", StringComparison.OrdinalIgnoreCase))
            .Select(path => (Path.GetRelativePath(root, path), File.ReadAllText(path)));
    }

    /// <summary>
    /// 从源码中提取指定方法/属性的方法体（第一个匹配签名之后的第一个大括号块）。
    /// </summary>
    /// <param name="source">源码文本</param>
    /// <param name="signatureFragment">签名片段（用于定位）</param>
    /// <param name="useLastOccurrence">同名重载存在时是否取最后一次出现（例如 Dispose/DisposeAsync）</param>
    /// <remarks>
    /// 提取前会剥离注释与字符串字面量：这既保证大括号匹配不被 `"{\"code\":0}"` 之类的字面量打断，
    /// 也保证**顺序类断言按代码而非注释判定**——例如"占位必须在 CloseAsync 之前"的检查，
    /// 若注释里出现 "改造前顺序是 Cancel → CloseAsync → 占位" 这样的说明文字，
    /// 不剥离注释就会得出相反的结论（本守卫首版即踩到该误报）。
    /// </remarks>
    private static string ExtractMethodBody(string source, string signatureFragment, bool useLastOccurrence = false)
    {
        var sanitized = StripCommentsAndStringLiterals(source);

        var index = useLastOccurrence
            ? sanitized.LastIndexOf(signatureFragment, StringComparison.Ordinal)
            : sanitized.IndexOf(signatureFragment, StringComparison.Ordinal);

        index.Should().BeGreaterThanOrEqualTo(0,
            $"守卫无法定位签名『{signatureFragment}』——源码结构已被改动，请同步更新守卫（宁可失败也不要假绿）");

        var openBrace = sanitized.IndexOf('{', index);
        openBrace.Should().BeGreaterThanOrEqualTo(0, $"签名『{signatureFragment}』之后未找到方法体起始大括号");

        var depth = 0;
        for (var i = openBrace; i < sanitized.Length; i++)
        {
            switch (sanitized[i])
            {
                case '{':
                    depth++;
                    break;
                case '}':
                    depth--;
                    if (depth == 0)
                    {
                        return sanitized.Substring(openBrace, i - openBrace + 1);
                    }

                    break;
            }
        }

        throw new InvalidOperationException($"方法体括号不匹配：{signatureFragment}");
    }

    /// <summary>
    /// 把注释与字符串/字符字面量的<b>内容</b>替换为等长空白（保留换行，索引与原文一致）。
    /// </summary>
    /// <remarks>
    /// 单遍状态机，识别：<c>//</c> 行注释、<c>/* */</c> 块注释、<c>"…"</c> 常规字符串（含 <c>\"</c> 转义）、
    /// <c>'…'</c> 字符字面量。插值字符串按常规字符串处理（其 <c>{expr}</c> 内的嵌套引号属已知边界，
    /// 本模块当前源码不触发）。目的有二：① 括号匹配不被字面量中的大括号打断；
    /// ② 顺序/存在性断言只反映代码，不受注释文字影响（守卫首版即被注释误伤过）。
    /// </remarks>
    private static string StripCommentsAndStringLiterals(string source)
    {
        var chars = source.ToCharArray();
        var i = 0;

        while (i < chars.Length)
        {
            var c = chars[i];
            var next = i + 1 < chars.Length ? chars[i + 1] : '\0';

            // 行注释
            if (c == '/' && next == '/')
            {
                while (i < chars.Length && chars[i] != '\n')
                {
                    chars[i] = ' ';
                    i++;
                }

                continue;
            }

            // 块注释
            if (c == '/' && next == '*')
            {
                chars[i] = ' ';
                chars[i + 1] = ' ';
                i += 2;
                while (i < chars.Length && !(chars[i] == '*' && i + 1 < chars.Length && chars[i + 1] == '/'))
                {
                    if (chars[i] != '\n' && chars[i] != '\r')
                    {
                        chars[i] = ' ';
                    }

                    i++;
                }

                if (i < chars.Length) { chars[i] = ' '; i++; }
                if (i < chars.Length) { chars[i] = ' '; i++; }
                continue;
            }

            // 常规字符串 / 字符字面量
            if (c == '"' || c == '\'')
            {
                var quote = c;
                i++;
                while (i < chars.Length)
                {
                    if (chars[i] == '\\' && i + 1 < chars.Length)
                    {
                        chars[i] = ' ';
                        if (chars[i + 1] != '\n' && chars[i + 1] != '\r') { chars[i + 1] = ' '; }
                        i += 2;
                        continue;
                    }

                    if (chars[i] == quote)
                    {
                        i++;
                        break;
                    }

                    if (chars[i] == '\n' || chars[i] == '\r')
                    {
                        // 未闭合（跨行）：保守结束，避免吞掉后续代码
                        break;
                    }

                    chars[i] = ' ';
                    i++;
                }

                continue;
            }

            i++;
        }

        return new string(chars);
    }

    #endregion
}
