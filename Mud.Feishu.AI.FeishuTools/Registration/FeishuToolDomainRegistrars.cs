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

    /// <summary>从编译期 Schema 常量提取注册表元数据（name/description/scopes/is_write 单一来源 = [FeishuTool] 特性）。</summary>
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

        return new FeishuToolDefinition(name, description, scopes, isWrite, handler);
    }
}

/// <summary>Bitable 域注册器（构造注入 <see cref="BitableTools"/> 执行器）。</summary>
internal sealed class BitableToolDomainRegistrar(BitableTools executor, FeishuToolBinding binding) : IFeishuToolDomainRegistrar
{
    public void Register(FeishuToolRegistry registry)
    {
        FeishuToolRegistration.RegisterTool(registry, FeishuToolNames.BitableListTables,
            (args, ctx, ct) => binding.ExecuteAsync(FeishuToolRegistration.Def(registry, FeishuToolNames.BitableListTables), args, ctx,
                token => executor.ListTablesAsync(args, token), ct));
        FeishuToolRegistration.RegisterTool(registry, FeishuToolNames.BitableListFields,
            (args, ctx, ct) => binding.ExecuteAsync(FeishuToolRegistration.Def(registry, FeishuToolNames.BitableListFields), args, ctx,
                token => executor.ListFieldsAsync(args, token), ct));
        FeishuToolRegistration.RegisterTool(registry, FeishuToolNames.BitableQueryRecords,
            (args, ctx, ct) => binding.ExecuteAsync(FeishuToolRegistration.Def(registry, FeishuToolNames.BitableQueryRecords), args, ctx,
                token => executor.QueryRecordsAsync(args, token), ct));
        FeishuToolRegistration.RegisterTool(registry, FeishuToolNames.BitableGetRecordsByIds,
            (args, ctx, ct) => binding.ExecuteAsync(FeishuToolRegistration.Def(registry, FeishuToolNames.BitableGetRecordsByIds), args, ctx,
                token => executor.GetRecordsByIdsAsync(args, token), ct));
    }
}

/// <summary>Docx 域注册器。</summary>
internal sealed class DocxToolDomainRegistrar(DocxTools executor, FeishuToolBinding binding) : IFeishuToolDomainRegistrar
{
    public void Register(FeishuToolRegistry registry)
    {
        FeishuToolRegistration.RegisterTool(registry, FeishuToolNames.DocxGetRawContent,
            (args, ctx, ct) => binding.ExecuteAsync(FeishuToolRegistration.Def(registry, FeishuToolNames.DocxGetRawContent), args, ctx,
                token => executor.GetRawContentAsync(args, token), ct));
        FeishuToolRegistration.RegisterTool(registry, FeishuToolNames.DocxGetDocumentBlocks,
            (args, ctx, ct) => binding.ExecuteAsync(FeishuToolRegistration.Def(registry, FeishuToolNames.DocxGetDocumentBlocks), args, ctx,
                token => executor.GetDocumentBlocksAsync(args, token), ct));
    }
}

/// <summary>Wiki 域注册器。</summary>
internal sealed class WikiToolDomainRegistrar(WikiTools executor, FeishuToolBinding binding) : IFeishuToolDomainRegistrar
{
    public void Register(FeishuToolRegistry registry)
    {
        FeishuToolRegistration.RegisterTool(registry, FeishuToolNames.WikiGetNode,
            (args, ctx, ct) => binding.ExecuteAsync(FeishuToolRegistration.Def(registry, FeishuToolNames.WikiGetNode), args, ctx,
                token => executor.GetNodeAsync(args, token), ct));
        FeishuToolRegistration.RegisterTool(registry, FeishuToolNames.WikiListNodes,
            (args, ctx, ct) => binding.ExecuteAsync(FeishuToolRegistration.Def(registry, FeishuToolNames.WikiListNodes), args, ctx,
                token => executor.ListNodesAsync(args, token), ct));
    }
}

/// <summary>Search 域注册器。</summary>
internal sealed class SearchToolDomainRegistrar(SearchTools executor, FeishuToolBinding binding) : IFeishuToolDomainRegistrar
{
    public void Register(FeishuToolRegistry registry)
    {
        FeishuToolRegistration.RegisterTool(registry, FeishuToolNames.SearchDocWiki,
            (args, ctx, ct) => binding.ExecuteAsync(FeishuToolRegistration.Def(registry, FeishuToolNames.SearchDocWiki), args, ctx,
                token => executor.SearchAsync(args, token), ct));
    }
}

/// <summary>IM 域注册器（只读两工具；写工具见 <see cref="WriteToolDomainRegistrar"/>）。</summary>
internal sealed class ImToolDomainRegistrar(ImTools executor, FeishuToolBinding binding) : IFeishuToolDomainRegistrar
{
    public void Register(FeishuToolRegistry registry)
    {
        FeishuToolRegistration.RegisterTool(registry, FeishuToolNames.ImGetHistoryMessages,
            (args, ctx, ct) => binding.ExecuteAsync(FeishuToolRegistration.Def(registry, FeishuToolNames.ImGetHistoryMessages), args, ctx,
                token => executor.GetHistoryAsync(args, token), ct));
        FeishuToolRegistration.RegisterTool(registry, FeishuToolNames.ImGetMessageContent,
            (args, ctx, ct) => binding.ExecuteAsync(FeishuToolRegistration.Def(registry, FeishuToolNames.ImGetMessageContent), args, ctx,
                token => executor.GetContentAsync(args, token), ct));
    }
}

/// <summary>Sheets 域注册器。</summary>
internal sealed class SheetsToolDomainRegistrar(SheetsTools executor, FeishuToolBinding binding) : IFeishuToolDomainRegistrar
{
    public void Register(FeishuToolRegistry registry)
    {
        FeishuToolRegistration.RegisterTool(registry, FeishuToolNames.SheetsListSheets,
            (args, ctx, ct) => binding.ExecuteAsync(FeishuToolRegistration.Def(registry, FeishuToolNames.SheetsListSheets), args, ctx,
                token => executor.ListSheetsAsync(args, token), ct));
        FeishuToolRegistration.RegisterTool(registry, FeishuToolNames.SheetsGetRangeValues,
            (args, ctx, ct) => binding.ExecuteAsync(FeishuToolRegistration.Def(registry, FeishuToolNames.SheetsGetRangeValues), args, ctx,
                token => executor.GetRangeValuesAsync(args, token), ct));
    }
}

/// <summary>Drive 域注册器（AI-FD-D12 P1D-1b 批次 A 新域）。</summary>
internal sealed class DriveToolDomainRegistrar(DriveTools executor, FeishuToolBinding binding) : IFeishuToolDomainRegistrar
{
    public void Register(FeishuToolRegistry registry)
    {
        FeishuToolRegistration.RegisterTool(registry, FeishuToolNames.DriveListFolderFiles,
            (args, ctx, ct) => binding.ExecuteAsync(FeishuToolRegistration.Def(registry, FeishuToolNames.DriveListFolderFiles), args, ctx,
                token => executor.ListFolderFilesAsync(args, token), ct));
        FeishuToolRegistration.RegisterTool(registry, FeishuToolNames.DriveGetFileMetas,
            (args, ctx, ct) => binding.ExecuteAsync(FeishuToolRegistration.Def(registry, FeishuToolNames.DriveGetFileMetas), args, ctx,
                token => executor.GetFileMetasAsync(args, token), ct));
    }
}

/// <summary>Knowledge 域注册器（AI-FD-D12 P2D-4b；绑定 IRetriever 门面，Aily 客户端缺席→不注册）。</summary>
internal sealed class KnowledgeToolDomainRegistrar(KnowledgeSearchTools executor, FeishuToolBinding binding) : IFeishuToolDomainRegistrar
{
    public void Register(FeishuToolRegistry registry)
    {
        FeishuToolRegistration.RegisterTool(registry, FeishuToolNames.KnowledgeSearch,
            (args, ctx, ct) => binding.ExecuteAsync(FeishuToolRegistration.Def(registry, FeishuToolNames.KnowledgeSearch), args, ctx,
                token => executor.SearchAsync(args, token), ct));
    }
}

/// <summary>通讯录域注册器（P0：把"姓名/邮箱 → ID"接上，否则写类工具凑不出入参）。</summary>
internal sealed class ContactToolDomainRegistrar(ContactTools executor, FeishuToolBinding binding) : IFeishuToolDomainRegistrar
{
    public void Register(FeishuToolRegistry registry)
    {
        FeishuToolRegistration.RegisterTool(registry, FeishuToolNames.ContactResolveUser,
            (args, ctx, ct) => binding.ExecuteAsync(FeishuToolRegistration.Def(registry, FeishuToolNames.ContactResolveUser), args, ctx,
                token => executor.ResolveUsersAsync(args, token), ct));
        FeishuToolRegistration.RegisterTool(registry, FeishuToolNames.ContactGetUser,
            (args, ctx, ct) => binding.ExecuteAsync(FeishuToolRegistration.Def(registry, FeishuToolNames.ContactGetUser), args, ctx,
                token => executor.GetUserAsync(args, token), ct));
        FeishuToolRegistration.RegisterTool(registry, FeishuToolNames.ContactBatchGet,
            (args, ctx, ct) => binding.ExecuteAsync(FeishuToolRegistration.Def(registry, FeishuToolNames.ContactBatchGet), args, ctx,
                token => executor.BatchGetUsersAsync(args, token), ct));
    }
}

/// <summary>写域注册器（Phase 2 三个写工具；对应域客户端缺席时跳过——宿主未接该域则工具不暴露）。</summary>
internal sealed class WriteToolDomainRegistrar(MessageWriteTools? messageWrite, BitableWriteTools? bitableWrite, ApprovalWriteTools? approvalWrite, FeishuToolBinding binding) : IFeishuToolDomainRegistrar
{
    public void Register(FeishuToolRegistry registry)
    {
        if (messageWrite is not null)
        {
            FeishuToolRegistration.RegisterTool(registry, FeishuToolNames.ImSendMessage,
                (args, ctx, ct) => binding.ExecuteAsync(FeishuToolRegistration.Def(registry, FeishuToolNames.ImSendMessage), args, ctx,
                    token => messageWrite.SendMessageAsync(args, token), ct));
        }

        if (bitableWrite is not null)
        {
            FeishuToolRegistration.RegisterTool(registry, FeishuToolNames.BitableAddRecord,
                (args, ctx, ct) => binding.ExecuteAsync(FeishuToolRegistration.Def(registry, FeishuToolNames.BitableAddRecord), args, ctx,
                    token => bitableWrite.AddRecordAsync(args, token), ct));
        }

        if (approvalWrite is not null)
        {
            FeishuToolRegistration.RegisterTool(registry, FeishuToolNames.ApprovalCreateInstance,
                (args, ctx, ct) => binding.ExecuteAsync(FeishuToolRegistration.Def(registry, FeishuToolNames.ApprovalCreateInstance), args, ctx,
                    token => approvalWrite.CreateInstanceAsync(args, token), ct));
        }
    }
}
