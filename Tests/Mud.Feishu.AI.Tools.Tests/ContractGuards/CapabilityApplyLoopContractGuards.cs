// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.AI.Tools.Tests.ContractGuards;

/// <summary>
/// 自省—申请闭环守卫：<see cref="CapabilityApplyLoop"/> 的候选策展名投影与申请通道文案的机械防线
/// （决策见 <c>documents/AIAgent/decisions/ADR-0006-capability-apply-loop.md</c>）。
/// </summary>
/// <remarks>
/// <para>
/// <b>拦截的失败形态</b>（都是静默的：产出仍是合法 JSON，只有语义错了）：
/// <list type="number">
/// <item><b>模块漂移</b>——投影绕过 <see cref="CapabilityModuleAliases"/>，跨命名分组（ChatGroup*、
/// Video*、Message…）的建议名会落进一个不存在的域，宿主据此去策展一个没有接口族的名字；
/// 这是 BUG-3 那类失真在「申请」侧的复现形态；</item>
/// <item><b>形态失控</b>——建议名里带 <c>Async</c>/大写/多个点，模型会把它读成 SDK 方法名
/// 或 HTTP 目标，进而臆造调用（与 R-12 的直接张力）；</item>
/// <item><b>与可调用面混淆</b>——建议名恰好等于既有工具名，模型会把「建议宿主策展」读成「已有可用工具」；</item>
/// <item><b>把全名一致率当契约</b>——策展命名是人类的语义行为（<c>GetApplicationsPageListAsync</c>
/// 实际是 <c>application.list_apps</c>），实测全名一致率 23.1%（342 条），断言它必然把规则锁死在巧合上；
/// 故本守卫只锁<b>模块前缀一致率下限</b>与结构形态。</item>
/// </list>
/// </para>
/// </remarks>
public class CapabilityApplyLoopContractGuards
{
    /// <summary>
    /// 模块前缀一致率下限：对既有策展面逐条比对「建议名的域前缀」与「实际工具名的域前缀」。
    /// 实测 95.6%（342 条策展条目、15 条不一致），留 5 点余量——别名表缺一条就会跌破
    /// （一条别名影响整族方法的建议前缀）。
    /// </summary>
    private const double MinModuleAgreement = 0.90;

    /// <summary>参与比对的策展条目下限（样本过小则本守卫恒绿，属假绿形态）。</summary>
    private const int MinSampleSize = 200;

    /// <summary>
    /// 判据 ①：投影的分词/动词归一/尾缀剥除逐项符合预期（解析器自证，一条一例）。
    /// </summary>
    [Theory]
    [InlineData("AcsAccessRecord", "GetAccessRecordListAsync", "acs.list_access_record")]
    [InlineData("ChatGroupMenu", "GetMenuByIdAsync", "chatgroup.get_menu")]
    [InlineData("ChatGroupAnnouncement", "DeleteTopNoticeAsync", "chatgroup.delete_top_notice")]
    [InlineData("Message", "DeleteMessageReactionAsync", "im.delete_message_reaction")]
    [InlineData("VideoConferencingMeeting", "GetMeetingInfoAsync", "vc.get_meeting_info")]
    [InlineData("DriveFiles", "DownloadFileAsync", "drive.download_file")]
    [InlineData("BitableRecord", "AddRecordsAsync", "bitable.create_records")]
    [InlineData("OkrIndicator", "UpdateIndicatorAsync", "okr.update_indicator")]
    [InlineData("MinutesMinute", "SubscribeMinuteAsync", "minutes.subscribe_minute")]
    [InlineData("Application", "GetApplicationAsync", "application.get_application")]
    [InlineData("CalendarEvent", "CreateCalendarEventAsync", "calendar.create_calendar_event")]
    [InlineData("OkrPeriod", "ListPeriodsAsync", "okr.list_periods")]
    [InlineData("Application", "GetApplicationsPageListAsync", "application.list_applications")]
    [InlineData("ChatGroupMember", "GetMemberPageListByIdAsync", "chatgroup.list_member")]
    public void SuggestToolName_ShouldProjectMethodName(string module, string method, string expected)
    {
        CapabilityApplyLoop.SuggestToolName(Entry(module, method)).Should().Be(expected);
    }

    /// <summary>判据 ②：非 SDK 接口派生的分组（<c>Unparsed</c>）无候选——不得给一个凭空造的域。</summary>
    [Fact]
    public void SuggestToolName_ShouldReturnNull_ForUnparsedGroup()
    {
        CapabilityApplyLoop.SuggestToolName(Entry("Unparsed", "RegisterAsync")).Should().BeNull();
    }

    /// <summary>
    /// 判据 ③：全目录投影只产出 <c>{module}.{action}</c> 形态（小写、单个点、不含 <c>Async</c>）。
    /// </summary>
    [Fact]
    public void SuggestToolName_ShouldOnlyProduceToolNameShape()
    {
        var suggestions = FeishuToolMethodCatalog.ByQualifiedName.Values
            .Select(CapabilityApplyLoop.SuggestToolName)
            .Where(static name => name is not null)
            .Select(static name => name!)
            .ToArray();

        suggestions.Should().NotBeEmpty("方法目录为空即假绿");

        var malformed = suggestions
            .Where(static name => !IsToolNameShape(name))
            .Take(10)
            .ToArray();

        malformed.Should().BeEmpty(
            "建议名必须与工具名同形态（小写 + 单个点 + 无 Async），否则模型会把它读成方法名或 HTTP 目标：{0}",
            string.Join(", ", malformed));
    }

    /// <summary>
    /// 判据 ④：投影必须复用别名表——跨命名分组的建议前缀落在真实模块上（BUG-3 的申请侧形态）。
    /// </summary>
    [Fact]
    public void SuggestToolName_ShouldReuseModuleAliases_ForCrossNamedGroups()
    {
        var aliased = CapabilityModuleAliases.ByGroupSegment
            .SelectMany(static pair => pair.Value.Select(module => (Segment: pair.Key, Module: module)))
            .GroupBy(static t => t.Module, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(static g => g.Key, g => g.Select(t => t.Segment).ToArray(), StringComparer.OrdinalIgnoreCase);

        var offenders = new List<string>();
        foreach (var pair in aliased)
        {
            var segment = pair.Value.FirstOrDefault();
            if (segment is null)
            {
                continue;
            }

            // 别名键若在生产代码里被改名，方法目录就不再有对应分组——跳过而不是硬失败（与本表既有守卫同口径）
            if (!FeishuToolMethodCatalog.ByQualifiedName.Values.Any(e => LeadingSegment(e.Module) == segment))
            {
                continue;
            }

            var suggestion = CapabilityApplyLoop.SuggestToolName(Entry(segment + "Resource", "GetThingAsync"));
            if (suggestion is null || !suggestion.StartsWith(pair.Key + ".", StringComparison.Ordinal))
            {
                offenders.Add($"{segment} → {suggestion}（期望前缀 {pair.Key}.）");
            }
        }

        offenders.Should().BeEmpty("候选策展名的域前缀必须经别名表解析：{0}", string.Join(", ", offenders));
    }

    /// <summary>
    /// 判据 ⑤：对既有策展面，建议名的<b>域前缀</b>与实际工具名的域前缀一致率不得低于下限。
    /// 一致率跌破 ⇒ 投影规则或别名表漂移，宿主会收到一批指向错误域的申请建议。
    /// </summary>
    [Fact]
    public void SuggestToolName_ModulePrefix_ShouldAgreeWithCuratedNames()
    {
        var pairs = FeishuToolMethodCatalog.ByQualifiedName.Values
            .Where(static e => e.Curated && e.CuratedTool is not null)
            .Select(static e => (Entry: e, Actual: e.CuratedTool!))
            .ToList();

        pairs.Should().HaveCountGreaterThanOrEqualTo(MinSampleSize,
            "参与比对的策展条目过少会让本守卫恒绿（假绿）");

        var disagreements = pairs
            .Where(static p => ModuleOf(CapabilityApplyLoop.SuggestToolName(p.Entry)) != ModuleOf(p.Actual))
            .Take(15)
            .Select(static p => $"{p.Entry.Module}.{p.Entry.Method} → {p.Actual}")
            .ToArray();

        var agreement = 1d - (double)pairs.Count(static p =>
            ModuleOf(CapabilityApplyLoop.SuggestToolName(p.Entry)) != ModuleOf(p.Actual)) / pairs.Count;

        agreement.Should().BeGreaterThanOrEqualTo(MinModuleAgreement,
            "域前缀一致率跌破下限说明别名表或投影规则漂移（样例：{0}）",
            string.Join(", ", disagreements));
    }

    /// <summary>判据 ⑥：同一入参两次投影必须同值（编译期常量目录 ⇒ 无随机、无状态）。</summary>
    [Fact]
    public void SuggestToolName_ShouldBeDeterministic()
    {
        var entries = FeishuToolMethodCatalog.ByQualifiedName.Values.Take(200).ToArray();

        foreach (var entry in entries)
        {
            CapabilityApplyLoop.SuggestToolName(entry).Should().Be(CapabilityApplyLoop.SuggestToolName(entry));
        }
    }

    /// <summary>
    /// 判据 ⑦：申请通道文案只有一个来源（两个元工具共用同一常量，不得各写一份）。
    /// </summary>
    [Fact]
    public void ApplyChannel_ShouldBeSourcedFromTheSingleConstant()
    {
        var root = FindRepositoryRoot();
        foreach (var file in new[] { "CapabilityLookupTools.cs", "SchemaReadTools.cs" })
        {
            var source = File.ReadAllText(Path.Combine(root, "Mud.Feishu.AI.Tools", "Internal", file));
            source.Should().Contain("CapabilityApplyLoop.ApplyChannel",
                $"{file} 的申请通道文案必须引用共享常量——两处各写一份必然漂移");
        }

        CapabilityApplyLoop.ApplyChannel.Should().Contain("[ToolHandler]",
            "申请通道必须写清策展动作（声明 + 执行器 + 登记），否则模型只会把建议名当成又一个调用通道");
        CapabilityApplyLoop.ApplyChannel.Should().Contain("不得把建议名当作可调用工具",
            "申请通道必须显式否定可调用性（R-12：不得形成第二条调用路径）");
    }

    private static FeishuToolMethodCatalog.Entry Entry(string module, string method)
        => new("IFeishuTenantV1Sample", method, "GET", "/open-apis/sample", "tenant", module,
            [], [], null, 0, null, false, null);

    private static bool IsToolNameShape(string name)
    {
        if (name.Contains("Async", StringComparison.Ordinal))
        {
            return false;
        }

        var dot = name.IndexOf('.');
        return dot > 0
            && dot == name.LastIndexOf('.')
            && name[..dot].All(static c => char.IsAscii(c) && (char.IsLower(c) || char.IsDigit(c) || c == '_'))
            && name[(dot + 1)..].All(static c => char.IsAscii(c) && (char.IsLower(c) || char.IsDigit(c) || c == '_'));
    }

    private static string? ModuleOf(string? name)
    {
        if (name is null)
        {
            return null;
        }

        var dot = name.IndexOf('.');
        return dot > 0 ? name.Substring(0, dot) : name;
    }

    private static string LeadingSegment(string groupKey)
    {
        var end = 1;
        while (end < groupKey.Length && char.IsLower(groupKey[end]))
        {
            end++;
        }

        return groupKey.Substring(0, end);
    }

    private static string FindRepositoryRoot()
    {
        var directory = AppContext.BaseDirectory;
        while (!string.IsNullOrEmpty(directory) && !File.Exists(Path.Combine(directory, "Mud.Feishu.slnx")))
        {
            directory = Path.GetDirectoryName(directory);
        }

        directory.Should().NotBeNullOrEmpty("测试必须能定位仓库根目录（以 Mud.Feishu.slnx 为锚）");
        return directory!;
    }
}
