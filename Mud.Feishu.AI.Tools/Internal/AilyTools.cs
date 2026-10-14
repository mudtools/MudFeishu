// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.DataModels.Aily;

namespace Mud.Feishu.AI.Tools.Internal;

/// <summary>
/// Aily「智能伙伴 · 数据知识」只读工具执行器（B2 补全域：3 个只读工具）。
/// </summary>
/// <remarks>
/// <para>
/// <b>软依赖</b>：客户端可空注入——宿主未接入 Aily 数据知识 API 时本执行器软缺席
/// （执行期给可执行的错误说明），不影响其它域工具。
/// 注意 <c>IFeishuTenantV1AilyDataKnowledge</c> 已被 <c>AilyKnowledgeProvider</c> 消费
/// （<c>knowledge.search</c> 的门面），故本执行器复用同一注册，不引入新的 DI 缺口。
/// </para>
/// <para>
/// <b>投影口径</b>：<c>Label</c> / <c>Description</c> 是 <c>Dictionary&lt;string,string&gt;</c>
/// 多语言映射——按 <c>zh_cn → en_us → 第一个</c> 的优先序取一个值，
/// 避免把整份多语言表铺进上下文（与 <c>I18nName</c> 的处理同思路）。
/// </para>
/// <para>
/// <b>分页</b>：本域两个列表结果都继承 <c>ApiPageListResult</c>（<c>HasMore</c> 为 <c>bool</c>），
/// 走<b>约束版</b>统一分页出口。
/// </para>
/// </remarks>
internal sealed class AilyTools(
    IOptions<FeishuAgentOptions> options,
    Mud.Feishu.IFeishuTenantV1AilyDataKnowledge? dataKnowledgeClient = null)
{
    /// <summary>标题 / 描述的预览截断长度（知识库标题通常很短，描述可能很长）。</summary>
    private const int TextPreviewLength = 200;

    private readonly Mud.Feishu.IFeishuTenantV1AilyDataKnowledge? _dataKnowledgeClient = dataKnowledgeClient;

    private readonly int _maxResultLength = ToolExecutor.Require(options).MaxToolResultLength;

    /// <summary>P-4（统一分页语义）：<c>fetch_all</c> 的条目 / 页数预算（来自宿主配置）。</summary>
    private readonly int _maxAutoFetchItems = ToolExecutor.Require(options).MaxAutoFetchItems;
    private readonly int _maxAutoFetchPages = ToolExecutor.Require(options).MaxAutoFetchPages;

    /// <summary>客户端缺席时的统一说明。</summary>
    private const string ClientName = "IFeishuTenantV1AilyDataKnowledge（Aily 数据知识 API）";

    // ─────────────────── aily.list_data_assets ───────────────────

    /// <summary>aily.list_data_assets：列出数据知识库。</summary>
    [ToolHandler(typeof(IFeishuTenantAilyListDataAssetsTool))]
    public Task<ToolResult> ListDataAssetsAsync(IReadOnlyDictionary<string, object?> arguments, CancellationToken cancellationToken)
    {
        var executor = new ToolExecutor(FeishuToolNames.AilyListDataAssets, _maxResultLength);
        if (Absent(executor.ToolName, _dataKnowledgeClient) is { } unavailable)
        {
            return Task.FromResult(unavailable);
        }

        return executor.RunAsync(async () =>
        {
            var args = AilyListDataAssetsArgs.Unpack(arguments);

            // P-4：统一分页出口——聚合路径与单页路径共用同一次调用定义与同一份投影。
            async Task<ToolApiOutcome<DataAssetPageListResult>> FetchPageAsync(string? token, CancellationToken ct)
                => FeishuApiResultReader.Read(await _dataKnowledgeClient!
                    .GetDataAssetPageListAsync(
                        args.AppId,
                        new DataAssetListQuery
                        {
                            PageToken = token,
                            Keyword = args.Keyword,
                            DataAssetIds = args.DataAssetIds,
                            DataAssetTagIds = args.DataAssetTagIds,
                            WithConnectStatus = args.WithConnectStatus,

                            // page_size 刻意不下发（保持 SDK 默认）——运维参数不进模型可见面（DP-B1-0）。
                        },
                        ct)
                    .ConfigureAwait(false));

            return await executor.FromPagedApiAsync(
                FetchPageAsync,
                ProjectDataAssetPage,
                args.FetchAll,
                args.MaxItems,
                args.PageToken,
                _maxAutoFetchItems,
                _maxAutoFetchPages,
                cancellationToken).ConfigureAwait(false);
        });
    }

    /// <summary>数据知识库列表投影：标识 + 单语言标题/描述 + 连接状态 + 标签。</summary>
    private static JsonObject ProjectDataAssetPage(DataAssetPageListResult result)
    {
        var envelope = ToolResultJsons.PageEnvelope(result.HasMore, result.PageToken);
        var items = envelope["items"]!.AsArray();

        foreach (var asset in result.Items ?? [])
        {
            var tags = new JsonArray();
            foreach (var tag in asset.Tags ?? [])
            {
                tags.AddNode(tag.Name);
            }

            items.AddNode(new JsonObject
            {
                ["data_asset_id"] = asset.DataAssetId,
                ["label"] = Pick(asset.Label),
                ["description"] = ToolResultText.Truncate(Pick(asset.Description), TextPreviewLength),
                ["data_source_type"] = asset.DataSourceType,
                ["connect_status"] = asset.ConnectStatus,
                ["connect_failed_reason"] = asset.ConnectFailedReason,
                ["tags"] = tags,
                ["item_count"] = asset.Items?.Length ?? 0,
                ["updated_time"] = asset.UpdatedTime,
            });
        }

        if (items.Count == 0)
        {
            envelope["found"] = false;
            envelope["message"] = "该应用下没有数据知识库。可能是应用未配置数据知识，或被 keyword / "
                + "data_asset_tag_ids 过滤掉了——先去掉收窄条件重试一次再判断。";
        }

        return envelope;
    }

    // ─────────────────── aily.get_data_asset ───────────────────

    /// <summary>aily.get_data_asset：查询单个数据知识库详情。</summary>
    [ToolHandler(typeof(IFeishuTenantAilyGetDataAssetTool))]
    public Task<ToolResult> GetDataAssetAsync(IReadOnlyDictionary<string, object?> arguments, CancellationToken cancellationToken)
    {
        var executor = new ToolExecutor(FeishuToolNames.AilyGetDataAsset, _maxResultLength);
        if (Absent(executor.ToolName, _dataKnowledgeClient) is { } unavailable)
        {
            return Task.FromResult(unavailable);
        }

        return executor.RunAsync(async () =>
        {
            var args = AilyGetDataAssetArgs.Unpack(arguments);

            var outcome = FeishuApiResultReader.Read(await _dataKnowledgeClient!
                .GetDataAssetAsync(
                    args.AppId,
                    args.DataAssetId,
                    with_data_asset_item: args.WithDataAssetItem,
                    cancellationToken: cancellationToken)
                .ConfigureAwait(false));

            return executor.FromApi(outcome, data => data.DataAsset is null
                ? NotFound(executor.ToolName, "data_asset_id", args.DataAssetId,
                    "请确认该 ID 来自 aily.list_data_assets，且 app_id 与知识库归属同一个 Aily 应用。")
                : WithFound(new JsonObject
                {
                    ["data_asset_id"] = data.DataAsset.DataAssetId,
                    ["label"] = Pick(data.DataAsset.Label),
                    ["description"] = ToolResultText.Truncate(Pick(data.DataAsset.Description), TextPreviewLength),
                    ["data_source_type"] = data.DataAsset.DataSourceType,
                    ["connect_type"] = data.DataAsset.ConnectType,
                    ["connect_status"] = data.DataAsset.ConnectStatus,
                    ["connect_failed_reason"] = data.DataAsset.ConnectFailedReason,
                    ["item_count"] = data.DataAsset.Items?.Length ?? 0,
                    ["created_time"] = data.DataAsset.CreatedTime,
                    ["updated_time"] = data.DataAsset.UpdatedTime,
                    ["notice"] = "条目明细（items）仅在 with_data_asset_item=true 时由平台返回；"
                        + "知识库**内容**的问答请用 knowledge.search。",
                }));
        });
    }

    // ─────────────────── aily.list_data_asset_tags ───────────────────

    /// <summary>aily.list_data_asset_tags：列出数据知识库标签。</summary>
    [ToolHandler(typeof(IFeishuTenantAilyListDataAssetTagsTool))]
    public Task<ToolResult> ListDataAssetTagsAsync(IReadOnlyDictionary<string, object?> arguments, CancellationToken cancellationToken)
    {
        var executor = new ToolExecutor(FeishuToolNames.AilyListDataAssetTags, _maxResultLength);
        if (Absent(executor.ToolName, _dataKnowledgeClient) is { } unavailable)
        {
            return Task.FromResult(unavailable);
        }

        return executor.RunAsync(async () =>
        {
            var args = AilyListDataAssetTagsArgs.Unpack(arguments);

            async Task<ToolApiOutcome<DataAssetTagPageListResult>> FetchPageAsync(string? token, CancellationToken ct)
                => FeishuApiResultReader.Read(await _dataKnowledgeClient!
                    .GetDataAssetTagPageListAsync(
                        args.AppId,
                        page_token: token,
                        keyword: args.Keyword,
                        cancellationToken: ct)
                    .ConfigureAwait(false));

            return await executor.FromPagedApiAsync(
                FetchPageAsync,
                ProjectTagPage,
                args.FetchAll,
                args.MaxItems,
                args.PageToken,
                _maxAutoFetchItems,
                _maxAutoFetchPages,
                cancellationToken).ConfigureAwait(false);
        });
    }

    /// <summary>标签列表投影：tag_id + name。</summary>
    private static JsonObject ProjectTagPage(DataAssetTagPageListResult result)
    {
        var envelope = ToolResultJsons.PageEnvelope(result.HasMore, result.PageToken);
        var items = envelope["items"]!.AsArray();

        foreach (var tag in result.Items ?? [])
        {
            items.AddNode(new JsonObject
            {
                ["data_asset_tag_id"] = tag.DataAssetTagId,
                ["name"] = tag.Name,
            });
        }

        if (items.Count == 0)
        {
            envelope["found"] = false;
            envelope["message"] = "该应用下没有数据知识库标签（或被 keyword 过滤掉了）。"
                + "标签用于给知识库分类，没有标签不影响知识库本身可用。";
        }

        return envelope;
    }

    // ─────────────────── 内部工具 ───────────────────

    /// <summary>多语言映射取单值：优先 zh_cn，其次 en_us，最后取第一个。</summary>
    private static string? Pick(IReadOnlyDictionary<string, string>? values)
    {
        if (values is null || values.Count == 0)
        {
            return null;
        }

        return values.TryGetValue("zh_cn", out var zh) && !string.IsNullOrEmpty(zh)
            ? zh
            : values.TryGetValue("en_us", out var en) && !string.IsNullOrEmpty(en)
                ? en
                : values.Values.FirstOrDefault(static value => !string.IsNullOrEmpty(value));
    }

    /// <summary>单对象"未找到"哨兵（把"ID 不对"与"没权限/应用不对"分开讲）。</summary>
    private static JsonObject NotFound(string toolName, string parameter, string value, string message)
        => new()
        {
            ["tool"] = toolName,
            ["found"] = false,
            [parameter] = value,
            ["message"] = message,
        };

    /// <summary>单对象成功投影：补 <c>found=true</c> 便于模型统一判断。</summary>
    private static JsonObject WithFound(JsonObject payload)
    {
        payload["found"] = true;
        return payload;
    }

    /// <summary>
    /// 软缺席：客户端为空时给出<b>可执行的错误结果</b>而不是抛异常。
    /// </summary>
    private static ToolResult? Absent(string toolName, object? client)
        => client is null
            ? ToolResult.FromError(FeishuToolBinding.StructuredError(
                toolName,
                $"未注册 {ClientName} 客户端——Aily 数据知识能力需宿主启用 Aily API（软缺席，不影响其它域工具）"))
            : null;
}
