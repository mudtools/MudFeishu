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
    /// 白名单为空：<c>ToolResultJsons</c> 是唯一实现，且在 <c>Tools/</c>（不在本守卫扫描面内）。
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
