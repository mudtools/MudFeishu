// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using System.Text.Json;
using Mud.Feishu.AI.FeishuTools.Internal;

namespace Mud.Feishu.AI.FeishuTools.Registration;

/// <summary>
/// 域级工具注册器契约（AI-FD-D12 P1D-1c，internal）：每域一个注册器——把该域工具注册进
/// <see cref="FeishuToolRegistry"/>。<b>客户端缺席 → 执行器缺席 → 注册器不注册</b>：域缺失表现为
/// 「该域工具不在注册表」，白名单映射期 fail-fast 报「未注册」——错误面从启动崩溃收敛为白名单期明确报错。
/// </summary>
/// <remarks>均为 FeishuTools 包 internal 实现（依赖方向不变）；<c>AddFeishuTools</c> 扫描全部注册器。</remarks>
internal interface IFeishuToolDomainRegistrar
{
    /// <summary>把该域工具注册进注册表（执行器缺席时不注册任何工具）。</summary>
    /// <param name="registry">工具注册表（宿主构建期单线程调用）。</param>
    void Register(FeishuToolRegistry registry);
}

/// <summary>注册表登记助手：从编译期 Schema 常量构造定义并注册（各域注册器共用）。</summary>
internal static class FeishuToolRegistration
{
    /// <summary>按名注册工具（缺编译期 Schema 即 fail-fast——契约守卫防漂移）。</summary>
    public static FeishuToolDefinition RegisterTool(FeishuToolRegistry registry, string toolName, FeishuToolHandler handler)
    {
        if (!FeishuToolSchemas.SchemaByToolName.TryGetValue(toolName, out var schemaJson))
        {
            throw new InvalidOperationException(
                $"工具 '{toolName}' 缺少编译期 Schema——接口须标注 [FeishuTool]（契约守卫防漂移）");
        }

        var definition = FromSchema(schemaJson, handler);
        registry.Register(definition);
        return definition;
    }

    /// <summary>按名取已注册定义（执行期调用；注册顺序保证存在性）。</summary>
    public static FeishuToolDefinition Def(FeishuToolRegistry registry, string toolName)
        => registry.TryGet(toolName, out var definition) && definition is not null
            ? definition
            : throw new InvalidOperationException($"工具 '{toolName}' 尚未注册（注册顺序错误）");

    /// <summary>
    /// 注册一枚"经执行链包裹的执行器方法"工具（AT-F16(a) / R3 评审 C-6：<b>纯样板抽取，零新机制</b>）。
    /// </summary>
    /// <remarks>
    /// <para>
    /// 取代此前在 11 个域注册器里逐行重复的三段式写法：
    /// <c>RegisterTool(registry, X, (args, ctx, ct) =&gt; binding.ExecuteAsync(Def(registry, X), args, ctx, token =&gt; executor.YAsync(args, token), ct))</c>。
    /// 抽出的只是"取定义 → 执行链包裹 → 传参闭包"这一机械结构，<b>不引入任何新特性、抽象或包</b>
    /// （对照 `[ToolProjection]` 那类声明式方案——后者属新造机制，已明确另立批次）。
    /// </para>
    /// <para>
    /// <b>定义必须在执行期经 <see cref="Def"/> 反查</b>（而非在注册期捕获）：工具定义本身持有 handler，
    /// 而 handler 又需要定义来走执行链——捕获会造成初始化循环。注册表在构建期单线程填充、
    /// 运行期只读，故该反查是纯字典命中，无并发与性能代价。
    /// </para>
    /// </remarks>
    /// <param name="registry">工具注册表。</param>
    /// <param name="toolName">工具名（须与 <c>[FeishuTool]</c> 派生的契约名一致）。</param>
    /// <param name="binding">工具执行链。</param>
    /// <param name="executorCall">分域执行器调用（入参 + 取消令牌 → 已投影/截断的结果）。</param>
    public static void RegisterExecution(
        FeishuToolRegistry registry,
        string toolName,
        FeishuToolBinding binding,
        Func<IReadOnlyDictionary<string, object?>, CancellationToken, Task<FeishuToolResult>> executorCall)
        => RegisterTool(registry, toolName, (args, ctx, ct) => binding.ExecuteAsync(
            Def(registry, toolName), args, ctx, token => executorCall(args, token), ct));

    /// <summary>从编译期 Schema 常量提取注册表元数据（单一来源 = [FeishuTool] 特性 + 生成器派生的 SDK 事实）。</summary>
    /// <remarks>
    /// <b>fail-fast 纪律（AT-B13 / R3 评审 C-14）</b>：<c>x-feishu.risk</c> 与 <c>x-feishu.identity</c> 是
    /// 策略判定（<c>MaxToolRisk</c>/<c>AllowedIdentities</c>）的输入，<b>缺字段意味着策略轴静默失效</b>
    /// （风险轴恒放行）。故此处不用 <c>GetProperty</c>（会抛裸 <c>KeyNotFoundException</c>，对宿主毫无指引），
    /// 而给出"应做/不应做"的可读异常——那条异常本身就是修复指引。
    /// </remarks>
    private static FeishuToolDefinition FromSchema(string schemaJson, FeishuToolHandler handler)
    {
        using var document = JsonDocument.Parse(schemaJson);
        var root = document.RootElement;
        var name = root.GetProperty("name").GetString()
            ?? throw new InvalidOperationException("Schema 常量缺少 name");
        var description = root.TryGetProperty("description", out var descriptionElement)
            ? descriptionElement.GetString() ?? string.Empty
            : string.Empty;

        var extension = root.GetProperty("x-feishu");
        var scopes = extension.GetProperty("required_scopes")
            .EnumerateArray()
            .Select(static e => e.GetString() ?? string.Empty)
            .Where(static s => s.Length > 0)
            .ToArray();
        var isWrite = extension.GetProperty("is_write").GetBoolean();
        var riskLiteral = ReadExtensionString(extension, "risk", name);
        if (!FeishuToolRiskNames.TryParse(riskLiteral, out var risk))
        {
            throw new InvalidOperationException(
                $"工具 '{name}' 的 x-feishu.risk 取值非法: '{riskLiteral}'——合法值为 {FeishuToolRiskNames.AllowedValuesText}。"
                + "该字段由源生成器从 SDK 事实派生，请勿手写；取值异常说明生成器产物被破坏。");
        }

        var identity = ReadExtensionString(extension, "identity", name);

        return new FeishuToolDefinition(name, description, scopes, isWrite, risk, identity, handler);
    }

    /// <summary>读取 <c>x-feishu</c> 扩展中的必填字符串（缺失时给出可读异常，而非裸 <c>KeyNotFoundException</c>）。</summary>
    private static string ReadExtensionString(JsonElement extension, string propertyName, string toolName)
        => extension.TryGetProperty(propertyName, out var element) && element.ValueKind == JsonValueKind.String
            ? element.GetString() ?? string.Empty
            : throw new InvalidOperationException(
                $"工具 '{toolName}' 的 Schema 缺少 x-feishu.{propertyName}——该字段由源生成器从 SDK 事实派生，"
                + "缺失意味着策略轴（MaxToolRisk / AllowedIdentities）将静默失效；请检查生成器与 golden 快照。");
}

/// <summary>Bitable 域注册器（构造注入 <see cref="BitableTools"/> 执行器）。</summary>
internal sealed class BitableToolDomainRegistrar(BitableTools executor, FeishuToolBinding binding) : IFeishuToolDomainRegistrar
{
    public void Register(FeishuToolRegistry registry)
    {
        FeishuToolRegistration.RegisterExecution(registry, FeishuToolNames.BitableListTables, binding,
            (args, ct) => executor.ListTablesAsync(args, ct));
        FeishuToolRegistration.RegisterExecution(registry, FeishuToolNames.BitableListFields, binding,
            (args, ct) => executor.ListFieldsAsync(args, ct));
        FeishuToolRegistration.RegisterExecution(registry, FeishuToolNames.BitableQueryRecords, binding,
            (args, ct) => executor.QueryRecordsAsync(args, ct));
        FeishuToolRegistration.RegisterExecution(registry, FeishuToolNames.BitableGetRecordsByIds, binding,
            (args, ct) => executor.GetRecordsByIdsAsync(args, ct));
    }
}

/// <summary>Docx 域注册器。</summary>
internal sealed class DocxToolDomainRegistrar(DocxTools executor, FeishuToolBinding binding) : IFeishuToolDomainRegistrar
{
    public void Register(FeishuToolRegistry registry)
    {
        FeishuToolRegistration.RegisterExecution(registry, FeishuToolNames.DocxGetRawContent, binding,
            (args, ct) => executor.GetRawContentAsync(args, ct));
        FeishuToolRegistration.RegisterExecution(registry, FeishuToolNames.DocxGetDocumentBlocks, binding,
            (args, ct) => executor.GetDocumentBlocksAsync(args, ct));
    }
}

/// <summary>Wiki 域注册器。</summary>
internal sealed class WikiToolDomainRegistrar(WikiTools executor, FeishuToolBinding binding) : IFeishuToolDomainRegistrar
{
    public void Register(FeishuToolRegistry registry)
    {
        FeishuToolRegistration.RegisterExecution(registry, FeishuToolNames.WikiGetNode, binding,
            (args, ct) => executor.GetNodeAsync(args, ct));
        FeishuToolRegistration.RegisterExecution(registry, FeishuToolNames.WikiListNodes, binding,
            (args, ct) => executor.ListNodesAsync(args, ct));
    }
}

/// <summary>Search 域注册器。</summary>
internal sealed class SearchToolDomainRegistrar(SearchTools executor, FeishuToolBinding binding) : IFeishuToolDomainRegistrar
{
    public void Register(FeishuToolRegistry registry)
        => FeishuToolRegistration.RegisterExecution(registry, FeishuToolNames.SearchDocWiki, binding,
            (args, ct) => executor.SearchAsync(args, ct));
}

/// <summary>IM 域注册器（只读两工具；写工具见 <see cref="WriteToolDomainRegistrar"/>）。</summary>
internal sealed class ImToolDomainRegistrar(ImTools executor, FeishuToolBinding binding) : IFeishuToolDomainRegistrar
{
    public void Register(FeishuToolRegistry registry)
    {
        FeishuToolRegistration.RegisterExecution(registry, FeishuToolNames.ImGetHistoryMessages, binding,
            (args, ct) => executor.GetHistoryAsync(args, ct));
        FeishuToolRegistration.RegisterExecution(registry, FeishuToolNames.ImGetMessageContent, binding,
            (args, ct) => executor.GetContentAsync(args, ct));
    }
}

/// <summary>Sheets 域注册器。</summary>
internal sealed class SheetsToolDomainRegistrar(SheetsTools executor, FeishuToolBinding binding) : IFeishuToolDomainRegistrar
{
    public void Register(FeishuToolRegistry registry)
    {
        FeishuToolRegistration.RegisterExecution(registry, FeishuToolNames.SheetsListSheets, binding,
            (args, ct) => executor.ListSheetsAsync(args, ct));
        FeishuToolRegistration.RegisterExecution(registry, FeishuToolNames.SheetsGetRangeValues, binding,
            (args, ct) => executor.GetRangeValuesAsync(args, ct));
    }
}

/// <summary>Drive 域注册器（AI-FD-D12 P1D-1b 批次 A 新域）。</summary>
internal sealed class DriveToolDomainRegistrar(DriveTools executor, FeishuToolBinding binding) : IFeishuToolDomainRegistrar
{
    public void Register(FeishuToolRegistry registry)
    {
        FeishuToolRegistration.RegisterExecution(registry, FeishuToolNames.DriveListFolderFiles, binding,
            (args, ct) => executor.ListFolderFilesAsync(args, ct));
        FeishuToolRegistration.RegisterExecution(registry, FeishuToolNames.DriveGetFileMetas, binding,
            (args, ct) => executor.GetFileMetasAsync(args, ct));
    }
}

/// <summary>Knowledge 域注册器（AI-FD-D12 P2D-4b；绑定 IRetriever 门面，Aily 客户端缺席→不注册）。</summary>
internal sealed class KnowledgeToolDomainRegistrar(KnowledgeSearchTools executor, FeishuToolBinding binding) : IFeishuToolDomainRegistrar
{
    public void Register(FeishuToolRegistry registry)
        => FeishuToolRegistration.RegisterExecution(registry, FeishuToolNames.KnowledgeSearch, binding,
            (args, ct) => executor.SearchAsync(args, ct));
}

/// <summary>通讯录域注册器（P0：把"姓名/邮箱/关键字 → ID"接上，否则写类工具凑不出入参）。</summary>
internal sealed class ContactToolDomainRegistrar(ContactTools executor, FeishuToolBinding binding) : IFeishuToolDomainRegistrar
{
    public void Register(FeishuToolRegistry registry)
    {
        FeishuToolRegistration.RegisterExecution(registry, FeishuToolNames.ContactResolveUser, binding,
            (args, ct) => executor.ResolveUsersAsync(args, ct));
        FeishuToolRegistration.RegisterExecution(registry, FeishuToolNames.ContactSearchUser, binding,
            (args, ct) => executor.SearchUsersAsync(args, ct));
        FeishuToolRegistration.RegisterExecution(registry, FeishuToolNames.ContactGetUser, binding,
            (args, ct) => executor.GetUserAsync(args, ct));
        FeishuToolRegistration.RegisterExecution(registry, FeishuToolNames.ContactBatchGet, binding,
            (args, ct) => executor.BatchGetUsersAsync(args, ct));
    }
}

/// <summary>
/// 能力出处域注册器（AT-F12）：<c>feishu.capability_lookup</c>。
/// </summary>
/// <remarks>
/// <b>无软缺席</b>：该工具的数据源是编译期常量（能力目录 + 工具名契约表），不依赖任何飞书客户端，
/// 故只要装配了工具包它就是<b>已注册</b>的——只是仍然默认<b>不启用</b>（与其他工具一致，
/// 需 <c>FeishuAgent:Tools</c> 白名单显式启用，见 §10.3 回滚说明）。
/// </remarks>
internal sealed class CapabilityLookupToolDomainRegistrar(CapabilityLookupTools executor, FeishuToolBinding binding) : IFeishuToolDomainRegistrar
{
    public void Register(FeishuToolRegistry registry)
        => FeishuToolRegistration.RegisterExecution(registry, FeishuToolNames.FeishuCapabilityLookup, binding,
            (args, ct) => executor.LookupAsync(args, ct));
}

/// <summary>写域注册器（Phase 2 三个写工具；对应域客户端缺席时跳过——宿主未接该域则工具不暴露）。</summary>
internal sealed class WriteToolDomainRegistrar(MessageWriteTools? messageWrite, BitableWriteTools? bitableWrite, ApprovalWriteTools? approvalWrite, FeishuToolBinding binding) : IFeishuToolDomainRegistrar
{
    public void Register(FeishuToolRegistry registry)
    {
        if (messageWrite is not null)
        {
            FeishuToolRegistration.RegisterExecution(registry, FeishuToolNames.ImSendMessage, binding,
                (args, ct) => messageWrite.SendMessageAsync(args, ct));
        }

        if (bitableWrite is not null)
        {
            FeishuToolRegistration.RegisterExecution(registry, FeishuToolNames.BitableAddRecord, binding,
                (args, ct) => bitableWrite.AddRecordAsync(args, ct));
        }

        if (approvalWrite is not null)
        {
            FeishuToolRegistration.RegisterExecution(registry, FeishuToolNames.ApprovalCreateInstance, binding,
                (args, ct) => approvalWrite.CreateInstanceAsync(args, ct));
        }
    }
}
