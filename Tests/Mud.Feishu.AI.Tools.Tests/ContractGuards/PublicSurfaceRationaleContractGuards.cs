// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using System.Globalization;
using System.Reflection;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Mud.Feishu.AI.Tools.Tests.ContractGuards;

/// <summary>
/// F-2 收官（最终裁定：**不收缩公开面，但把"新增 public 必须说明消费方"机械化**）。
/// </summary>
/// <remarks>
/// <para>
/// <b>为什么是守卫而不是清单</b>：F-2 的消费方审计已证伪"可裁 30%–40%"——零仓内消费方的 public 类型
/// 全是宿主扩展点与导出管线的缝（删即删产品面）。既然答案是"保留"，那么可执行的动作就只剩一件：
/// <b>让"新增 public 要说明消费方"不再依赖评审者的记性</b>。本守卫用"冻结基线 + 增量理由"两段式实现：
/// <c>public-surface-rationale.json</c>（位置见 <see cref="FeishuDocs.PublicSurfaceRationale"/>）的 <c>baseline</c> 是实测快照，
/// 之后新增的类型必须出现在 <c>reasons</c> 里（值＝消费方说明），否则报红。
/// </para>
/// <para>
/// <b>为什么不要求给 289 个既有类型逐条写理由</b>：那是"补作业"，不产生信息——基线已由审计结论背书；
/// 真正需要拦的是**增量**（新增公开面时最容易忘记"谁会用"）。
/// </para>
/// </remarks>
public class PublicSurfaceRationaleContractGuards
{
    // F-2 收官：289 → 290（新增 ISkillDocumentReader 一个公开类型；理由已登记在
    // public-surface-rationale.json 的 reasons 段，本常量与公开面总数同步）。
    // R-10（2026-10-12）：290 → **284**——NDJSON 事件外桥与事件目录共 6 个 public 类型
    // （FeishuEventEnvelope / IFeishuEventSink / IFeishuEventFileRouter / FeishuEventNdjsonBridge /
    //  RegexEventFileRouter / FeishuEventCatalog）移出库发布面（降为 Demo 参考实现）。
    // T1/D1 + T4/D4 + T5/D5（2026-10-12）：284 → **297**——用户令牌前置探测、单轮工具预算、
    // 全局并发背压、群聊/媒体上下文装配共 13 个新 public 类型。基线（283 + 1 条既有理由）
    // 不重打快照：13 个类型全部按"冻结基线 + 增量理由"两段式登记在
    // public-surface-rationale.json 的 reasons 段（键=类型全名，值=消费方说明）。
    // 差距补齐批次（2026-10-13）：冻结基线（283）+ 既有 reasons（14）+ 本批新增 6 个 public 类型
    // （S-1 的 FeishuToolPolicyOptions / FeishuToolPolicyDomainOptions、BUG-4 的 GuidanceModes /
    //  GuidanceRoutingSummary、M-2 的 FeishuMcpResourceCatalog / McpResourceInfo）= 303
    // （全部登记在 reasons 段）。
    // v2.3 P1-D（2026-10-13）：354 → **350**——执行器骨架中立化上移 Mud.AI.Agent（决策⑮）：
    // FeishuToolResult / IToolResultShaper / ToolError / IToolErrorTextFormatter 四个 public 类型
    // 迁出飞书发布面（新家 = Mud.AI.Agent.Tools，由该包自己的 PublicAPI 台账登记）。
    // v2.3 P1-E：350 → **348**——声明契约中立化上移 Mud.AI.Abstractions：
    // AgentToolAttribute（原 FeishuToolAttribute）与 ToolParameterAttribute 两个 public 类型
    // 迁出 Mud.Feishu.AI 发布面（新家 = Mud.AI.Abstractions.Tools）。
    // 【合并两侧（2026-10-14）】：348（P1-E 迁出 6 个后的共同出发点）**+ 本地策展 9 个 = 357**，
    // 同时**传入侧 P1-H2a 净值 −2 = 346**——两侧各自只记了自己那一半，故**两个候选值都不能取**，
    // 必须相加：348 + 9 − 2 = **355**（实测快照 = 三份 PublicAPI.Unshipped.txt 去重后的类型级条目）。
    // 本地 +9（全部登记在 public-surface-rationale.json 的 reasons 段）：Application 写面 2
    // （update_visibility / update_management）+ HelpDesk 写面 4（start_service / answer_user_query /
    // create_ticket_message / create_bot_message）+ Aily 只读 3（list_data_assets / get_data_asset /
    // list_data_asset_tags）。
    // 传入 −2：P1-H2a 域 guidance 装配机制中立化上移 Mud.AI.Agent——
    // FeishuGuidanceBlock / FeishuGuidanceResult / FeishuGuidanceComposer 三个 public 类型迁出
    // 飞书发布面（−3），飞书侧只留预算常量 FeishuGuidanceBudget（+1）。
    // 新家 = Mud.AI.Agent.Guidance，由该包自己的 PublicAPI 台账登记（本守卫只数飞书发布面，
    // 这是「中立包自带台账」而非遗漏——见 ApiFiles 的工程白名单）。
    // N1/N2 中立化（2026-10-14）：355 → **335**——本轮把「AI 运行时机制」的公共面整批迁出
    // Mud.Feishu.AI 发布面：契约与值类型 → Mud.AI.Abstractions（会话请求 / 上下文事实 /
    // 流式通道 / 宿主批准通道 / 媒体转写 / 知识检索），机制实现 → Mud.AI.Agent（会话摘要器 /
    // 待确认快照存储 / 工具预算与名字模式 / 文档分块与向量检索）。
    // 这些类型的新家各自有 PublicAPI 台账（本守卫只数飞书发布面，见 ApiFiles 白名单），
    // 故 −20 是**搬迁的结果**而不是公开面收缩。
    // +1：ConversationScopeMapper（N1 的中立化接缝适配器，理由登记在 reasons 段）。
    // N2-b（2026-10-14）：335 → **333**——流式通道机制中立化：分片缓冲基类
    // BufferedMessageChannel 与降级链 StreamingChannelChain 两个 public 类型迁出
    // Mud.Feishu.AI 发布面（新家 = Mud.AI.Agent.Channels，由该包自己的 PublicAPI
    // 台账登记）。产品子类（EditMessageChannel / CardStreamMessageChannel）留飞书发布面。
    // N2-d（2026-10-14）：333 → **332**——单轮工具预算 FeishuToolCallBudget 一个 public 类型
    // 迁出飞书发布面（新家 = Mud.AI.Agent.Tools.ToolCallBudget，「AI 运行时 ↔ 工具面共享接缝」
    // 的正确归属是中立运行期包）。同批迁出的 ToolNamePattern 原是 internal（不计类型级条目），
    // 中立侧新增的 ToolPolicyMatcher / ToolPolicyRules / ToolPolicyDecision 由 Mud.AI.Agent
    // 自己的 PublicAPI 台账登记（本守卫只数飞书发布面）。
    // N2-e（2026-10-14）：332 → **327**——RAG 自建检索机制五个 public 类型迁出飞书发布面
    // （VectorRetriever / CorpusIndexer / DocumentChunker / ChunkingOptions / TokenEstimator；
    // 新家 = Mud.AI.Agent.Knowledge，由该包自己的 PublicAPI 台账登记）。
    // 产品侧只留 DI 装配面 FeishuVectorKnowledgeExtensions（签名随 ChunkingOptions 中立化更新）。
    // N2-f（2026-10-14）：327 → **326**——渐进式会话摘要器 ConversationSummarizer 一个 public 类型
    // 迁出飞书发布面（新家 = Mud.AI.Agent.Conversations，由该包自己的 PublicAPI 台账登记；
    // 产品侧只留产品事实 FeishuSummarizerFacts（internal）。
    // 326 = 三产品程序集（Mud.Feishu.AI.Tools 269 / Mud.Feishu.AI 50 / Mud.Feishu.AI.Mcp 7）
    // 反射实测的公开类型总数（= 原 PublicAPI.Unshipped.txt 类型级条目总数）。
    // 【N6 · D5 裁决落地（2026-10-14）】PublicAPI 台账文件全仓删除（用户裁决："这类文件对开发者
    // 没屁用"），本守卫的数据源改为**反射实测**——「新增 public 必须说明消费方」机制保留：
    // 新增公开类型而未在 public-surface-rationale.json 的 reasons 段登记消费方时，本守卫报红。
    // 增减公开类型时同批更新本常量（总数与实测同锁 = 扫描器读坏时不会"零缺失"假绿）。
    // T-1 读取闭环（2026-10-16）：326 → **330**——4 个新工具声明面 public 接口
    // （IFeishuTenantDriveDownloadFileTool / IFeishuTenantDriveExportDocumentTool /
    //  IFeishuTenantCalendarAgendaTool / IFeishuTenantCalendarRsvpTool，全部登记在 reasons 段）；
    // Mud.Feishu.AI.Tools 269 → 273（Mud.Feishu.AI 50 / Mud.Feishu.AI.Mcp 7 不变）。
    private const int BaselineTypeCount = 330;

    /// <summary>反射实测的公开面来源程序集（= 原台账覆盖面：三个飞书发布包）。</summary>
    private static readonly string[] ProductAssemblies =
    [
        "Mud.Feishu.AI.Tools",
        "Mud.Feishu.AI",
        "Mud.Feishu.AI.Mcp",
    ];

    [Fact]
    public void EveryPublicType_ShouldBeInBaselineOrDeclareItsConsumer()
    {
        var publicTypes = ReadPublicTypes();

        // 自证：总数与实测基线同锁——扫描器读空/读坏时不会"零缺失"假绿。
        publicTypes.Should().HaveCount(
            BaselineTypeCount,
            "公开面类型总数基线（三份 PublicAPI.Unshipped.txt 的类型级条目）；增减公开类型时同批更新");

        var baseline = ReadRationaleBaseline();
        baseline.Should().NotBeEmpty("基线必须来自 public-surface-rationale.json 的 baseline 数组");

        var unaccounted = FindUnaccounted(publicTypes, baseline, ReadRationaleReasons());

        unaccounted.Should().BeEmpty(
            "以下 public 类型既不在冻结基线里、也没有在 public-surface-rationale.json 的 reasons 中登记"
            + "「消费方」——公开面即产品面，新增它必须能回答「谁会用、怎么用」：\n{0}",
            string.Join("\n", unaccounted));
    }

    [Fact]
    public void DeclaredReasons_ShouldNotBeStale()
    {
        var publicTypes = ReadPublicTypes().ToHashSet(StringComparer.Ordinal);
        var reasons = ReadRationaleReasons();

        var stale = reasons
            .Where(type => !publicTypes.Contains(type))
            .OrderBy(static type => type, StringComparer.Ordinal)
            .ToArray();

        stale.Should().BeEmpty(
            "以下理由指向已不存在的 public 类型（改名的类型要同批改理由，否则基线会悄悄失守）：{0}",
            string.Join(", ", stale));
    }

    /// <summary>
    /// 检查器自证：既不在基线、也没理由的类型<b>必须</b>被点名；基线内或已登记理由的类型<b>不得</b>误报。
    /// （没有这条，"过滤器恒返回空数组"会让上面那条用例长期假绿。）
    /// </summary>
    [Fact]
    public void Scanner_ShouldFlagUnaccountedType_AndAcceptBothSources()
    {
        string[] publicTypes = ["Mud.Feishu.AI.Tools.Known", "Mud.Feishu.AI.Tools.BrandNew"];
        var baseline = new HashSet<string>(StringComparer.Ordinal) { "Mud.Feishu.AI.Tools.Known" };
        var reasons = new HashSet<string>(StringComparer.Ordinal) { "Mud.Feishu.AI.Tools.Explained" };

        var unaccounted = FindUnaccounted(publicTypes, baseline, reasons);

        unaccounted.Should().Equal(["Mud.Feishu.AI.Tools.BrandNew"], "基线内不得误报；无理由的新类型必须点名");
    }

    /// <summary>检查器（纯函数）：返回"既不在基线、也无理由"的公开类型（有序）。</summary>
    private static string[] FindUnaccounted(
        IEnumerable<string> publicTypes,
        IReadOnlySet<string> baseline,
        IReadOnlySet<string> reasons)
        => publicTypes
            .Where(type => !baseline.Contains(type) && !reasons.Contains(type))
            .OrderBy(static type => type, StringComparer.Ordinal)
            .ToArray();

    /// <summary>
    /// 反射实测三产品程序集的公开类型（<see cref="Assembly.GetExportedTypes"/>），
    /// 泛型类型定义的元数据名（<c>Ns.Name`1</c>）归一化为台账形态（<c>Ns.Name&lt;T&gt;</c>），
    /// 与 public-surface-rationale.json 的键空间逐字节兼容。
    /// </summary>
    private static string[] ReadPublicTypes()
    {
        var publicTypes = new SortedSet<string>(StringComparer.Ordinal);
        foreach (var assemblyName in ProductAssemblies)
        {
            var assembly = Assembly.Load(new AssemblyName(assemblyName));
            foreach (var type in assembly.GetExportedTypes())
            {
                // 导出类型必有全名（无泛型参数实参的类型定义恒可解析）。
                // 嵌套类型的元数据分隔符 '+' 归一化为基线键空间的 '.'（与原台账形态一致）。
                publicTypes.Add(NormalizeGenericName(type.FullName!).Replace('+', '.'));
            }
        }

        return publicTypes.ToArray();
    }

    /// <summary>把泛型类型定义的元数据名（<c>Ns.Name`N</c>）归一化为 <c>Ns.Name&lt;T&gt;</c> / <c>Ns.Name&lt;T1,...&gt;</c>（与基线键空间一致）。</summary>
    private static string NormalizeGenericName(string fullName)
        => GenericArityPattern.Replace(fullName, match =>
        {
            var arity = int.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture);
            return "<" + string.Join(",", Enumerable.Range(1, arity).Select(i => arity == 1 ? "T" : $"T{i}")) + ">";
        });

    private static readonly Regex GenericArityPattern = new(@"`(\d+)$", RegexOptions.Compiled);

    private static HashSet<string> ReadRationaleBaseline()
        => ReadRationaleArray("baseline");

    private static HashSet<string> ReadRationaleReasons()
    {
        using var document = JsonDocument.Parse(File.ReadAllText(RationaleFilePath()));
        var reasons = new HashSet<string>(StringComparer.Ordinal);
        if (document.RootElement.TryGetProperty("reasons", out var element))
        {
            foreach (var property in element.EnumerateObject())
            {
                reasons.Add(property.Name);
            }
        }

        return reasons;
    }

    private static HashSet<string> ReadRationaleArray(string propertyName)
    {
        using var document = JsonDocument.Parse(File.ReadAllText(RationaleFilePath()));
        document.RootElement.TryGetProperty(propertyName, out var element)
            .Should().BeTrue($"public-surface-rationale.json 必须含 {propertyName} 数组");

        return element.EnumerateArray()
            .Select(static item => item.GetString()!)
            .ToHashSet(StringComparer.Ordinal);
    }

    private static string RationaleFilePath()
        => Path.Combine(
            FindRepositoryRoot(),
            FeishuDocs.PublicSurfaceRationale.Replace('/', Path.DirectorySeparatorChar));

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
