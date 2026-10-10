// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using System.Text.Json;

using Mud.Feishu.AI.Tools.Generated;
using Mud.Feishu.AI.Tools.Tests.Tools;

namespace Mud.Feishu.AI.Tools.Tests.ContractGuards;

/// <summary>
/// <b>PII 工具纪律（R7 / DP-A5-1 档位 ①③⑥）</b>：把"哪些工具触碰员工个人信息、哪些默认不启用"
/// 从文档描述升级为<b>可机械判定的门禁</b>。
/// </summary>
/// <remarks>
/// <para>
/// <b>要解决的缺陷形态</b>：PII 出口（他人考勤 / 通讯录 / 群聊历史 / 邮件 / 会议逐字稿）一旦
/// 被无意启用，数据就随每一轮对话进入第三方 LLM 并持久化到会话历史——<b>不可召回</b>。
/// 而在本守卫之前，"哪些工具是 PII 工具"只存在于评审者的记忆里：新增工具不会触发任何信号。
/// </para>
/// <para>
/// <b>唯一真相源</b>：<c>documents/AIAgent/scope-authority.json</c> 的 <c>piiTools</c> 数组
/// （与 scope 清单同一份权威文件——两者都是"人从控制台/合规口径核对后回填"的登记）。
/// 本守卫锁三件事：
/// <list type="number">
/// <item>登记集合 == 用例内声明集合（新增/删除 PII 工具必须同批更新，防静默膨胀）；</item>
/// <item>登记的每条都必须是<b>真实契约工具</b>（防僵尸条目）；</item>
/// <item><c>defaultDisabled=true</c> 的工具必须<b>真的默认不启用</b>且描述含后果句。</item>
/// </list>
/// </para>
/// </remarks>
public class PiiToolDisciplineContractGuards
{
    private const string AuthorityFilePath = "documents/AIAgent/scope-authority.json";

    /// <summary>
    /// PII 工具集合基线（2026-10-10 实测）。
    /// </summary>
    /// <remarks>
    /// 新增 PII 工具时<b>必须</b>同时：① 在本数组加名；② 在 <c>scope-authority.json</c> 的
    /// <c>piiTools</c> 加条目（含 category/note）；③ 在《工具权限对照表》的 R7 增量段登记 PII 类别。
    /// </remarks>
    private static readonly string[] ExpectedPiiTools =
    [
        "attendance.query_user_flow",
        "attendance.get_flow",
        "attendance.query_stats_data",
        "contact.list_department_members",
        "contact.search_user",
        "contact.get_user",
        "im.get_history_messages",
        "mail.list_messages",
        "mail.get_message",
        "minutes.get_artifacts",
    ];

    private sealed record PiiEntry(string Tool, string Category, bool DefaultDisabled, string Note);

    [Fact]
    public void PiiRegistry_ShouldMatchExpectedSet()
    {
        var entries = LoadPiiEntries();

        entries.Select(static e => e.Tool).OrderBy(static t => t, StringComparer.Ordinal)
            .Should().BeEquivalentTo(
                ExpectedPiiTools.OrderBy(static t => t, StringComparer.Ordinal),
                "PII 工具集合是**合规面的事实**：新增/删除必须同批更新本基线与 scope-authority.json"
                + "（PII 出口一旦被无意启用，数据进入第三方模型即不可召回）");
    }

    [Fact]
    public void PiiRegistry_ShouldOnlyList_RealContractTools()
    {
        var entries = LoadPiiEntries();
        entries.Should().NotBeEmpty("PII 登记不得为空——空集合会让本守卫静默通过（假绿）");

        foreach (var entry in entries)
        {
            FeishuToolNames.All.Should().Contain(entry.Tool,
                $"PII 登记中的 {entry.Tool} 必须是真实契约工具（僵尸条目 = 该工具的 PII 纪律已失效）");
            entry.Category.Should().NotBeNullOrWhiteSpace($"{entry.Tool} 必须给出 PII 类别（人读视图）");
            entry.Note.Should().NotBeNullOrWhiteSpace($"{entry.Tool} 必须给出登记理由（可审计）");
        }
    }

    [Fact]
    public void DefaultDisabledPiiTools_ShouldNotBeInAnyDefaultWhitelist_AndDeclareConsequences()
    {
        var defaultDisabled = LoadPiiEntries().Where(static e => e.DefaultDisabled).ToArray();

        defaultDisabled.Should().NotBeEmpty(
            "DP-A5-1 档位 ① 的核心就是「他人考勤默认不启用」——没有 defaultDisabled 条目说明登记面被改坏了");

        // ① 默认不启用：默认配置（Tools=[] / WriteAllowList=[]）下不得出现在启用清单里。
        var options = new FeishuAgentOptions { Instructions = "test" };
        options.Tools.Should().BeEmpty("工具默认全不启用（安全默认）——PII 工具因此天然不在任何默认白名单");
        options.WriteAllowList.Should().BeEmpty();

        using var provider = GuardProviderFactory.CreateProvider(_ => { });
        var registry = provider.GetRequiredService<FeishuToolRegistry>();
        foreach (var entry in defaultDisabled)
        {
            registry.EnabledTools.Select(static t => t.Name).Should().NotContain(entry.Tool,
                $"{entry.Tool} 默认不启用（DP-A5-1 档位 ①）——出现即合规缺陷");
        }

        // ② 描述含后果句（数据进第三方模型上下文）+ 显式声明"默认不启用"。
        foreach (var entry in defaultDisabled)
        {
            var schema = FeishuToolSchemas.SchemaByToolName[entry.Tool];
            schema.Should().Contain("第三方模型上下文",
                $"{entry.Tool} 的描述必须声明后果（结果进第三方模型上下文，DP-A5-1 ②）");
            schema.Should().Contain("默认不启用",
                $"{entry.Tool} 的描述必须告诉模型「该能力需宿主显式启用」——否则模型会反复重试同一次被拒调用");
            schema.Should().Contain("PII");
        }
    }

    [Fact]
    public void Guard_ShouldActuallyReadTheRegistry()
    {
        // 反向自证：登记解析必须真读到条目，否则上面的用例会因"空集合"而假绿。
        LoadPiiEntries().Should().HaveCount(ExpectedPiiTools.Length,
            "解析到的条目数必须与基线一致——少于基线说明解析规则失效（假绿）");
    }

    private static IReadOnlyList<PiiEntry> LoadPiiEntries()
    {
        var path = Path.Combine(FindRepositoryRoot(), AuthorityFilePath);
        File.Exists(path).Should().BeTrue($"PII 登记所在文件必须存在：{AuthorityFilePath}");

        using var document = JsonDocument.Parse(File.ReadAllText(path));
        var root = document.RootElement;
        root.TryGetProperty("piiTools", out var array).Should().BeTrue(
            "scope-authority.json 必须含 piiTools 数组（DP-A5-1 ③ 的唯一真相源）");

        var entries = new List<PiiEntry>();
        foreach (var item in array.EnumerateArray())
        {
            entries.Add(new PiiEntry(
                item.GetProperty("tool").GetString() ?? string.Empty,
                item.TryGetProperty("category", out var category) ? category.GetString() ?? string.Empty : string.Empty,
                item.TryGetProperty("defaultDisabled", out var disabled) && disabled.GetBoolean(),
                item.TryGetProperty("note", out var note) ? note.GetString() ?? string.Empty : string.Empty));
        }

        return entries;
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
