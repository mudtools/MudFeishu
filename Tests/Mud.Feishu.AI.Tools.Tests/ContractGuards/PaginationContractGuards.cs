// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！
//  任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.AI.Tools.Tests.ContractGuards;

/// <summary>
/// B1 自动分页契约守卫（方案 §3.B1 守卫四条）。
/// </summary>
/// <remarks>
/// <para>
/// 守卫四条：
/// <list type="number">
/// <item>每个分页工具的 Schema 必须同时含 <c>fetch_all</c> 与 <c>max_items</c>（缺一即红）；</item>
/// <item>执行器内不得手写 <c>while</c>/<c>do</c> 翻页循环（防"两套翻页口径"）；</item>
/// <item>上限值只能来自 <c>PageSizes</c> 或 Options 常量；</item>
/// <item><c>fetch_all=false</c> 路径的行为等价性（金丝雀）。</item>
/// </list>
/// </para>
/// </remarks>
public class PaginationContractGuards
{
    /// <summary>方案 §3.B1 首批 12 个分页工具名 + R7 增量（Drive 评论 / Spark 三工具）。</summary>
    private static readonly string[] PaginatedTools =
    [
        "bitable.list_tables",
        "bitable.query_records",
        "wiki.list_nodes",
        "drive.list_folder_files",
        "drive.list_comments",
        "docx.list_block_children",
        "docx.get_document_blocks",
        "docx.get_chat_announcement",
        "calendar.list_events",
        "task.list_my_tasks",
        "approval.list_pending_tasks",
        "mail.list_messages",
        "contact.list_departments",
        "contact.list_department_members",
        // R7/A6：Spark 妙搭三个分页工具（官方 page_size 不进模型面，只暴露 fetch_all/max_items）。
        "spark.list_apps",
        "spark.list_tables",
        "spark.query_table_records",
    ];

    /// <summary>
    /// 守卫 ①：每个分页工具的 Schema 必须含 <c>fetch_all</c> 参数。
    /// </summary>
    /// <remarks>
    /// 金丝雀：注掉一个工具的 fetch_all 参数即红。
    /// 注：docx.list_block_children 是 A2 新增工具，当前可能在 Schema 中不存在——
    /// 存在性检查由 <see cref="PaginatedTools_ShouldExistInContracts"/> 独立断言。
    /// </remarks>
    [Fact]
    public void PaginatedTools_ShouldHaveFetchAllInParameterSchema()
    {
        foreach (var toolName in PaginatedTools)
        {
            if (!FeishuToolSchemas.SchemaByToolName.TryGetValue(toolName, out var schema))
            {
                // A2 新增工具可能尚未存在——跳过（由存在性守卫独立断言）
                continue;
            }

            schema.Should().Contain("fetch_all",
                $"{toolName} 的 Schema 必须含 fetch_all 参数（B1 自动分页）");
        }
    }

    /// <summary>
    /// 守卫 ①（续）：每个分页工具的 Schema 必须含 <c>max_items</c> 参数。
    /// </summary>
    [Fact]
    public void PaginatedTools_ShouldHaveMaxItemsInParameterSchema()
    {
        foreach (var toolName in PaginatedTools)
        {
            if (!FeishuToolSchemas.SchemaByToolName.TryGetValue(toolName, out var schema))
            {
                continue;
            }

            schema.Should().Contain("max_items",
                $"{toolName} 的 Schema 必须含 max_items 参数（B1 自动分页）");
        }
    }

    /// <summary>
    /// <b>P-0 守卫（统一分页语义的机械判据）</b>：<b>凡声明了 <c>page_token</c> 的工具，必须同时声明 <c>fetch_all</c></b>
    /// ——未登记在豁免清单里的缺口即红。
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>为什么用"声明了 page_token"作判据</b>：这是<b>工具自己</b>声明"我是分页列表"的事实
    /// （比按工具名前缀猜 <c>list_/query_/search_</c> 精确：<c>vc.list_reserves</c> 名字像列表、
    /// 实际按 ID 取详情；<c>attendance.query_*</c> 名字像列表、接口根本不分页）。
    /// 判据 = "契约说了要翻页，就必须提供自动翻页的能力"。
    /// </para>
    /// <para>
    /// <b>豁免清单必须带理由</b>（方案 §5.3 P-0 的原文要求）：豁免不是"允许漏"，而是
    /// "把未完成的批次登记成可见的技术债"——清单里的条目在补齐后<b>必须</b>删除，
    /// 否则本用例会因"豁免已过期"而报红（见 <c>StaleExemptions_ShouldBeRemoved</c>）。
    /// </para>
    /// </remarks>
    [Fact]
    public void ToolsWithPageToken_MustDeclareFetchAll_UnlessRegisteredAsExempt()
    {
        var exemptions = PendingPaginationExemptions()
            .Select(static e => e.Tool)
            .ToHashSet(StringComparer.Ordinal);

        var missing = FeishuToolSchemas.SchemaByToolName
            .Where(pair => DeclaresPageTokenParameter(pair.Value))
            .Where(pair => !DeclaresFetchAllParameter(pair.Value))
            .Select(pair => pair.Key)
            .Where(name => !exemptions.Contains(name))
            .OrderBy(static name => name, StringComparer.Ordinal)
            .ToArray();

        // 扫描面自证：若"含 page_token 入参的工具"集合为空，本用例恒绿（假门禁）。
        FeishuToolSchemas.SchemaByToolName
            .Count(pair => DeclaresPageTokenParameter(pair.Value))
            .Should().BeGreaterThan(0, "扫描面为空 = 判据失效（假绿）");

        missing.Should().BeEmpty(
            "以下工具**声明了 page_token 入参却未提供 fetch_all**——模型只能手工翻页（每次往返一次调用）。"
            + "统一分页语义要求：要么接入 ToolPagination（推荐），要么登记进 PendingPaginationExemptions 并写清理由：{0}",
            string.Join(", ", missing));
    }

    /// <summary>
    /// 豁免清单必须<b>仍然真的需要豁免</b>（已补齐的工具必须从清单里删掉，防"技术债清单静静腐烂"）。
    /// </summary>
    [Fact]
    public void StaleExemptions_ShouldBeRemoved()
    {
        var stale = PendingPaginationExemptions()
            .Where(entry => !FeishuToolSchemas.SchemaByToolName.TryGetValue(entry.Tool, out var schema)
                || !DeclaresPageTokenParameter(schema)
                || DeclaresFetchAllParameter(schema))
            .Select(static entry => entry.Tool)
            .ToArray();

        stale.Should().BeEmpty(
            "以下豁免条目已过期（工具已接入 fetch_all，或已不再是分页工具，或已不存在）——"
            + "请从 PendingPaginationExemptions 中删除：{0}",
            string.Join(", ", stale));
    }

    /// <summary>
    /// 工具是否<b>声明了 <c>page_token</c> 入参</b>（只看 <c>parameters</c> 区）。
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>为什么必须只看 parameters 区（实测教训）</b>：首版判据是在整份 schema 文本里找
    /// <c>"page_token"</c>，结果把 <c>drive.add_comment</c>（写工具）误报成"分页工具"——
    /// 它的命中来自<b>输出投影</b>里的 <c>has_more</c>/<c>page_token</c> 字段（返回体结构），
    /// 与"入参能不能翻页"毫无关系。而这会让"写工具不得带 fetch_all"与"分页工具必须带 fetch_all"
    /// 形成**互相矛盾的强制要求**（该工具无论怎么做都报红）——假红比漏报更糟：它会逼人放宽判据。
    /// </para>
    /// <para>
    /// 实现上做 JSON 字符串感知的花括号配对（描述文本里可能含 <c>{}</c>），
    /// 并由 <see cref="ParameterSectionScanner_ShouldIsolateParametersOnly"/> 自证。
    /// </para>
    /// </remarks>
    internal static bool DeclaresPageTokenParameter(string schema)
        => ParametersSection(schema).Contains("\"page_token\":", StringComparison.Ordinal);

    /// <summary>工具是否声明了 <c>fetch_all</c> 入参（同样只看 parameters 区）。</summary>
    internal static bool DeclaresFetchAllParameter(string schema)
        => ParametersSection(schema).Contains("\"fetch_all\":", StringComparison.Ordinal);

    /// <summary>取出 schema 的 <c>"parameters":{...}</c> 对象（含花括号；找不到返回空串）。</summary>
    internal static string ParametersSection(string schema)
    {
        const string marker = "\"parameters\":{";
        var start = schema.IndexOf(marker, StringComparison.Ordinal);
        if (start < 0)
        {
            return string.Empty;
        }

        var open = start + marker.Length - 1;
        var depth = 0;
        var inString = false;
        var escaped = false;

        for (var p = open; p < schema.Length; p++)
        {
            var ch = schema[p];
            if (inString)
            {
                if (escaped)
                {
                    escaped = false;
                }
                else if (ch == '\\')
                {
                    escaped = true;
                }
                else if (ch == '"')
                {
                    inString = false;
                }

                continue;
            }

            if (ch == '"')
            {
                inString = true;
            }
            else if (ch == '{')
            {
                depth++;
            }
            else if (ch == '}')
            {
                depth--;
                if (depth == 0)
                {
                    return schema.Substring(open, p - open + 1);
                }
            }
        }

        return string.Empty;
    }

    /// <summary>判据自证：只认参数区（输出投影里的同名字段不得计入）。</summary>
    [Fact]
    public void ParameterSectionScanner_ShouldIsolateParametersOnly()
    {
        const string withParamOnly =
            """{"name":"x.y","parameters":{"type":"object","properties":{"page_token":{"type":"string"}}},"output_schema":{"page_token":{"type":"string"}}}""";
        const string withOutputOnly =
            """{"name":"x.z","parameters":{"type":"object","properties":{"file_token":{"type":"string"}}},"output_schema":{"has_more":{"type":"boolean"},"page_token":{"type":"string"}}}""";

        DeclaresPageTokenParameter(withParamOnly).Should().BeTrue("入参区声明了 page_token 必须被认出");
        DeclaresPageTokenParameter(withOutputOnly).Should().BeFalse(
            "page_token 只出现在输出投影里 ⇒ 不是分页入参（drive.add_comment 的实测形态）");

        ParametersSection(withParamOnly).Should()
            .Contain("\"page_token\":")
            .And.NotContain("output_schema", "参数区切片不得越界到后面的键");

        DeclaresFetchAllParameter(
            """{"parameters":{"properties":{"fetch_all":{"type":"boolean"}}}}""")
            .Should().BeTrue("fetch_all 入参必须被认出");
    }

    /// <summary>
    /// <b>写工具不得声明 <c>fetch_all</c></b>（方案 §5.2 的"副作用放大风险"红线）。
    /// </summary>
    [Fact]
    public void WriteTools_ShouldNeverDeclareFetchAll()
    {
        var offenders = FeishuToolContracts.ByToolName
            .Where(static pair => pair.Value.IsWrite)
            .Where(pair => FeishuToolSchemas.SchemaByToolName.TryGetValue(pair.Key, out var schema)
                && schema.Contains("\"fetch_all\"", StringComparison.Ordinal))
            .Select(static pair => pair.Key)
            .ToArray();

        offenders.Should().BeEmpty(
            "写工具不得带 fetch_all——自动翻页会把一次写操作放大成 N 次副作用：{0}",
            string.Join(", ", offenders));
    }

    /// <summary>
    /// 统一分页语义的<b>未完成批次登记</b>（每条必须带理由；补齐后必须删除）。
    /// </summary>
    /// <remarks>
    /// 本清单的存在本身是"诚实的进度记录"：方案 §5.3 把改造分成 P-1~P-4 四批，
    /// 已完成批次（IM / VC / OKR / Contact / Search）不在此列；未完成批次在此显式登记，
    /// 由 <see cref="ToolsWithPageToken_MustDeclareFetchAll_UnlessRegisteredAsExempt"/> 兜住。
    /// </remarks>
    private static (string Tool, string Reason)[] PendingPaginationExemptions() =>
    [
        // 注：方案 §5.3 点名的 P-1~P-4 批次已全部落地（IM / VC / OKR / Hire / Minutes / Contact / Search），
        // 故原"批次未完成"条目已按本守卫的纪律**删除**（StaleExemptions 会点名任何过期条目）。

        // ────────── P-0 判据扩展后**新暴露**的缺口（此前无人知道） ──────────
        // 这批工具都"声明了 page_token 却未提供 fetch_all"：模型能翻页但每页一次往返。
        // 登记为技术债（而不是让守卫静默放行），补齐后必须从本清单删除。
        ("contact.get_department_parents", "P-0 新暴露：通讯录部门父级链未接 fetch_all"),
        ("contact.search_departments", "P-0 新暴露：部门搜索未接 fetch_all"),
        ("org.list_job_families", "P-0 新暴露：组织元数据四个 list_* 未接 fetch_all"),
        ("org.list_job_levels", "P-0 新暴露（同上）"),
        ("org.list_job_titles", "P-0 新暴露（同上）"),
        ("org.list_work_cities", "P-0 新暴露（同上）"),
        ("security.list_openapi_logs", "P-0 新暴露：安全日志两个查询未接 fetch_all"),
        ("security.query_audit_logs", "P-0 新暴露（同上）"),
    ];

    /// <summary>
    /// 守卫 ②：执行器内不得手写翻页循环（<c>while</c>/<c>do</c> 循环翻页）。
    /// </summary>
    /// <remarks>
    /// <para>
    /// 源码结构断言：翻页只能经 <c>ToolPagination.AggregateOutcomesAsync</c> 实现。
    /// </para>
    /// <para>
    /// <b>文案订正（B-3 / R-4）</b>：本守卫原写「必须经 <c>ToolPagination.AggregateAsync</c>」——
    /// 而生产侧<b>零调用</b>该方法（它是仅测试使用的裸页函数适配包装，R-4 已删除），
    /// 文案会误导维护者去修改错误的方法。现按生产实际实现（<c>AggregateOutcomesAsync</c>）更正。
    /// </para>
    /// </remarks>
    [Fact]
    public void Executors_ShouldNotHandWritePaginationLoops()
    {
        var executorFiles = Directory.GetFiles(
            Path.Combine(FindRepositoryRoot(), "Mud.Feishu.AI.Tools", "Internal"),
            "*Tools.cs",
            SearchOption.TopDirectoryOnly);

        executorFiles.Should().NotBeEmpty("执行器文件必须存在");

        foreach (var file in executorFiles)
        {
            var source = File.ReadAllText(file);

            // 检查是否同时包含翻页相关标识 + while/do 循环
            // 注意：ToolPagination.cs 本身是翻页实现，应被排除
            if (file.EndsWith("ToolSearchTools.cs", StringComparison.Ordinal))
            {
                continue;
            }

            // 检查是否手写了翻页循环（while + HasMore / while + page_token）
            var hasWhileLoop = source.Contains("while (", StringComparison.Ordinal)
                || source.Contains("while(", StringComparison.Ordinal);
            var hasPaginationTerms = source.Contains("HasMore", StringComparison.Ordinal)
                || source.Contains("has_more", StringComparison.Ordinal)
                || source.Contains("PageToken", StringComparison.Ordinal)
                || source.Contains("page_token", StringComparison.Ordinal);

            // ToolPagination.cs 是翻页实现本身，允许
            if (file.EndsWith("ToolPagination.cs", StringComparison.Ordinal))
            {
                continue;
            }

            // CapabilityLookupTools.cs 等元工具不含翻页循环
            if (hasWhileLoop && hasPaginationTerms)
            {
                throw new Xunit.Sdk.XunitException(
                    $"{Path.GetFileName(file)} 同时包含 while 循环与翻页术语（HasMore/page_token）——"
                    + "翻页必须经 ToolPagination.AggregateOutcomesAsync 实现，禁止手写循环");
            }
        }
    }

    /// <summary>
    /// <b>R-3 守卫</b>：<b>单页信封字段只允许来自统一帮助方法</b>（<c>ToolResultJsons</c>）。
    /// </summary>
    /// <remarks>
    /// <para>
    /// 源码结构断言：<c>Internal/</c> 下的执行器不得再出现
    /// <list type="bullet">
    /// <item><c>["items"] = new JsonArray()</c>（信封起手形态）；</item>
    /// <item><c>envelope["page_token"] = …</c>（<c>page_token</c> 赋值形态）。</item>
    /// </list>
    /// </para>
    /// <para>
    /// <b>为什么拦这两条形态</b>：它们正是"逐字重写信封"的最小指纹——29 处 / 18 个文件的
    /// 重复在此收敛；任一处重新出现即意味着某执行器又自带了一套信封口径
    /// （键名/空 token 语义可静默漂移，模型续不上页且无任何信号）。
    /// 白名单为空：<c>ToolResultJsons</c> 是唯一实现，且在 <c>Results/</c>（不在本守卫扫描面内）。
    /// </para>
    /// </remarks>
    [Fact]
    public void PageEnvelopeFields_ShouldOnlyComeFromSharedHelper()
    {
        var executorFiles = Directory.GetFiles(
            Path.Combine(FindRepositoryRoot(), "Mud.Feishu.AI.Tools", "Internal"),
            "*.cs",
            SearchOption.TopDirectoryOnly);

        executorFiles.Should().NotBeEmpty("执行器文件必须存在");

        var offenders = new List<string>();
        foreach (var file in executorFiles)
        {
            var source = File.ReadAllText(file);

            if (source.Contains("[\"items\"] = new JsonArray()", StringComparison.Ordinal))
            {
                offenders.Add($"{Path.GetFileName(file)}: [\"items\"] = new JsonArray()（应改为 ToolResultJsons.ItemsEnvelope()/PageEnvelope(...)）");
            }

            if (source.Contains("[\"page_token\"] =", StringComparison.Ordinal))
            {
                offenders.Add($"{Path.GetFileName(file)}: envelope[\"page_token\"] = …（应改为 ToolResultJsons.WithPageToken/PageEnvelope(...)）");
            }
        }

        offenders.Should().BeEmpty(
            "单页信封（items/has_more/page_token）必须经 ToolResultJsons 单源构造——"
            + "逐文件重写会让『空 token 不写』这类语义静默漂移（模型续不上页且无信号）：\n"
            + string.Join("\n", offenders));
    }

    /// <summary>
    /// 守卫 ③：ToolPagination 硬上限常量存在且为正。
    /// </summary>
    [Fact]
    public void ToolPagination_HardLimits_ShouldBePositive()
    {
        ToolPagination.HardMaxItems.Should().BeGreaterThan(0, "硬上限必须为正数");
        ToolPagination.HardMaxItems.Should().BeLessThanOrEqualTo(1000, "硬上限不得过大");

        ToolPagination.HardMaxPages.Should().BeGreaterThan(0, "硬上限必须为正数");
        ToolPagination.HardMaxPages.Should().BeLessThanOrEqualTo(100, "硬上限不得过大");
    }

    /// <summary>
    /// 守卫 ④：FeishuAgentOptions 含 B1 分页配置项且默认值合理。
    /// </summary>
    [Fact]
    public void FeishuAgentOptions_ShouldHavePaginationDefaults()
    {
        var options = new FeishuAgentOptions();
        options.MaxAutoFetchItems.Should().BeGreaterThan(0, "默认预算必须为正");
        options.MaxAutoFetchItems.Should().BeLessThanOrEqualTo(ToolPagination.HardMaxItems,
            "默认预算不得超硬上限");
        options.MaxAutoFetchPages.Should().BeGreaterThan(0, "默认页数必须为正");
        options.MaxAutoFetchPages.Should().BeLessThanOrEqualTo(ToolPagination.HardMaxPages,
            "默认页数不得超硬上限");
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
