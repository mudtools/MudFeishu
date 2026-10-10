// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.AI.Tools.Tools;
using Mud.Feishu.DataModels.Wiki;

namespace Mud.Feishu.AI.Tools.Internal;

/// <summary>
/// Wiki 双工具执行器（<c>wiki.get_node</c> / <c>wiki.list_nodes</c>）：
/// 节点白名单 node_token/title/obj_type/obj_token/parent_node_token（枢纽定位，§3.3.2）。
/// </summary>
/// <remarks>执行骨架（catch/回填/截断）由 <see cref="ToolExecutor"/> 承担（WP3）；本类只保留参数校验与投影语义。</remarks>
internal sealed class WikiTools(Mud.Feishu.IFeishuTenantV2WikiNodes wikiNodesClient, IOptions<FeishuAgentOptions> options)
{
    private readonly Mud.Feishu.IFeishuTenantV2WikiNodes _wikiNodesClient = wikiNodesClient
        ?? throw new ArgumentNullException(nameof(wikiNodesClient));
    private readonly int _maxResultLength = ToolExecutor.Require(options).MaxToolResultLength;
    private readonly int _maxAutoFetchItems = ToolExecutor.Require(options).MaxAutoFetchItems;
    private readonly int _maxAutoFetchPages = ToolExecutor.Require(options).MaxAutoFetchPages;

    /// <summary>wiki.get_node：解析节点信息（单对象）。</summary>
    [FeishuToolHandler(typeof(IFeishuTenantWikiGetNodeTool))]
    public Task<FeishuToolResult> GetNodeAsync(IReadOnlyDictionary<string, object?> arguments, CancellationToken cancellationToken)
    {
        var executor = new ToolExecutor(FeishuToolNames.WikiGetNode, _maxResultLength);
        return executor.RunAsync(async () =>
        {
            var args = WikiGetNodeArgs.Unpack(arguments);

            var outcome = FeishuApiResultReader.Read(await _wikiNodesClient
                .GetNodeSpaceInfoAsync(args.Token, args.ObjType ?? "wiki", cancellationToken)
                .ConfigureAwait(false));
            return executor.FromApi(outcome, static data => new JsonObject
            {
                ["node"] = ProjectNode(data.Node),
            });
        });
    }

    /// <summary>wiki.list_nodes：列出子节点（分页）。</summary>
    [FeishuToolHandler(typeof(IFeishuTenantWikiListNodesTool))]
    public Task<FeishuToolResult> ListNodesAsync(IReadOnlyDictionary<string, object?> arguments, CancellationToken cancellationToken)
    {
        var executor = new ToolExecutor(FeishuToolNames.WikiListNodes, _maxResultLength);
        return executor.RunAsync(async () =>
        {
            var args = WikiListNodesArgs.Unpack(arguments);

            // B1：fetch_all=true 时循环翻页（唯一翻页实现 ToolPagination）。
            if (args.FetchAll == true)
            {
                var maxItems = ToolPagination.ResolveMaxItems(args.MaxItems, _maxAutoFetchItems);
                var aggregated = await ToolPagination.AggregateOutcomesAsync<ApiPageListResult<SpaceNodeInfo>>(
                    async (token, ct) => FeishuApiResultReader.Read(await _wikiNodesClient
                        .GetSpaceNodesPageListAsync(args.SpaceId, args.ParentNodeToken, PageSizes.WikiNodes, token, ct)
                        .ConfigureAwait(false)),
                    page => (page.HasMore, page.PageToken),
                    page => ProjectNodes(page)["items"]!.AsArray(),
                    maxItems,
                    ToolPagination.ResolveMaxPages(_maxAutoFetchPages),
                    cancellationToken).ConfigureAwait(false);
                return executor.FromPagedResult(aggregated, maxItems);
            }

            var outcome = FeishuApiResultReader.Read(await _wikiNodesClient
                .GetSpaceNodesPageListAsync(args.SpaceId, args.ParentNodeToken, PageSizes.WikiNodes, args.PageToken, cancellationToken)
                .ConfigureAwait(false));
            return executor.FromApi(outcome, ProjectNodes);
        });
    }

    /// <summary>list_nodes 投影：items（节点白名单）+ 翻页契约。</summary>
    private static JsonObject ProjectNodes(ApiPageListResult<SpaceNodeInfo> data)
    {
        // R-3：信封形态单源（ToolResultJsons.PageEnvelope）。
        var envelope = ToolResultJsons.PageEnvelope(data.HasMore, data.PageToken);

        foreach (var node in data.Items ?? [])
        {
            envelope["items"]!.AsArray().AddNode(ProjectNode(node));
        }

        return envelope;
    }

    private static JsonObject ProjectNode(SpaceNodeInfo? node) => new()
    {
        ["node_token"] = node?.NodeToken,
        ["title"] = node?.Title,
        ["obj_type"] = node?.ObjType,
        ["obj_token"] = node?.ObjToken,
        ["parent_node_token"] = node?.ParentNodeToken,
        ["has_child"] = node?.HasChild,
    };

    // ────────── R5 / F-11：wiki 写面（此前本域只有读） ──────────

    /// <summary>wiki.create_node：在知识空间下创建节点。</summary>
    [FeishuToolHandler(typeof(IFeishuTenantWikiCreateNodeTool))]
    public Task<FeishuToolResult> CreateNodeAsync(IReadOnlyDictionary<string, object?> arguments, CancellationToken cancellationToken)
    {
        var executor = new ToolExecutor(FeishuToolNames.WikiCreateNode, _maxResultLength);
        return executor.RunAsync(async () =>
        {
            var args = WikiCreateNodeArgs.Unpack(arguments);

            if (ToolDryRun.IsRequested(args.DryRun))
            {
                return FeishuToolResult.FromText(ToolDryRun.Describe(
                    executor.ToolName, "POST", $"/open-apis/wiki/v2/spaces/{args.SpaceId}/nodes",
                    ToolDryRun.IdempotencyNote(null),
                    ("space_id", args.SpaceId.Length), ("title", args.Title.Length)));
            }

            var outcome = FeishuApiResultReader.Read(await _wikiNodesClient
                .CreateSpaceNodeAsync(
                    args.SpaceId,
                    new CreateSpaceNodeRequest
                    {
                        Title = args.Title,
                        ObjType = args.ObjType ?? "docx",
                        ParentNodeToken = args.ParentNodeToken,
                        NodeType = args.NodeType ?? "origin",
                    },
                    cancellationToken)
                .ConfigureAwait(false));

            return executor.FromApi(outcome, data => ProjectNode(data.Node, created: true));
        });
    }

    /// <summary>wiki.move_node：移动节点（改父节点或换空间）。</summary>
    [FeishuToolHandler(typeof(IFeishuTenantWikiMoveNodeTool))]
    public Task<FeishuToolResult> MoveNodeAsync(IReadOnlyDictionary<string, object?> arguments, CancellationToken cancellationToken)
    {
        var executor = new ToolExecutor(FeishuToolNames.WikiMoveNode, _maxResultLength);
        return executor.RunAsync(async () =>
        {
            var args = WikiMoveNodeArgs.Unpack(arguments);

            // 目标缺失必须提前拒绝：平台会把"移到哪"留空后原地不动，模型却会以为已移动。
            if (string.IsNullOrWhiteSpace(args.TargetParentToken) && string.IsNullOrWhiteSpace(args.TargetSpaceId))
            {
                throw new ArgumentException(
                    "target_parent_token 与 target_space_id 至少要提供一个——两者都为空时平台不会移动节点，"
                    + "但调用会'成功'（空操作），容易让模型误判");
            }

            if (ToolDryRun.IsRequested(args.DryRun))
            {
                return FeishuToolResult.FromText(ToolDryRun.Describe(
                    executor.ToolName, "POST",
                    $"/open-apis/wiki/v2/spaces/{args.SpaceId}/nodes/{args.NodeToken}/move",
                    ToolDryRun.IdempotencyNote(null),
                    ("space_id", args.SpaceId.Length), ("node_token", args.NodeToken.Length)));
            }

            var outcome = FeishuApiResultReader.Read(await _wikiNodesClient
                .MoveSpaceNodeAsync(
                    args.SpaceId,
                    args.NodeToken,
                    new MoveSpaceNodeRequest
                    {
                        TargetParentToken = args.TargetParentToken,
                        TargetSpaceId = args.TargetSpaceId,
                    },
                    cancellationToken)
                .ConfigureAwait(false));

            return executor.FromApi(outcome, data => ProjectNode(data.Node, created: false));
        });
    }

    /// <summary>
    /// wiki.move_docs_to_space：把已有云文档迁入知识空间（异步任务）。
    /// </summary>
    /// <remarks>
    /// <b>为何必须如实回传 <c>applied</c> 与 <c>task_id</c></b>：平台以异步任务执行，
    /// 立即返回并不代表已生效。只回"成功"会让模型误以为文档已在知识库里。
    /// </remarks>
    [FeishuToolHandler(typeof(IFeishuTenantWikiMoveDocsToSpaceTool))]
    public Task<FeishuToolResult> MoveDocsToSpaceAsync(IReadOnlyDictionary<string, object?> arguments, CancellationToken cancellationToken)
    {
        var executor = new ToolExecutor(FeishuToolNames.WikiMoveDocsToSpace, _maxResultLength);
        return executor.RunAsync(async () =>
        {
            var args = WikiMoveDocsToSpaceArgs.Unpack(arguments);

            if (ToolDryRun.IsRequested(args.DryRun))
            {
                return FeishuToolResult.FromText(ToolDryRun.Describe(
                    executor.ToolName, "POST", $"/open-apis/wiki/v2/spaces/{args.SpaceId}/nodes/move_docs_to_wiki",
                    "平台以**异步任务**执行：立即返回不代表已生效，applied 字段为真实状态。"
                    + ToolDryRun.IdempotencyNote(null),
                    ("space_id", args.SpaceId.Length), ("obj_token", args.ObjToken.Length)));
            }

            var outcome = FeishuApiResultReader.Read(await _wikiNodesClient
                .MoveDocsToWikiSpaceNodeAsync(
                    args.SpaceId,
                    new MoveDocsToWikiSpaceNodeRequest
                    {
                        ObjToken = args.ObjToken,
                        ObjType = args.ObjType ?? "docx",
                        ParentWikiToken = args.ParentWikiToken,
                    },
                    cancellationToken)
                .ConfigureAwait(false));

            return executor.FromApi(outcome, data => new JsonObject
            {
                ["wiki_token"] = data.WikiToken,
                ["task_id"] = data.TaskId,
                ["applied"] = data.Applied,
                ["note"] = data.Applied == true
                    ? "已迁入知识空间"
                    : "平台已受理但**尚未生效**（异步任务）——请稍后用 wiki.get_node 确认 wiki_token 对应节点是否出现",
            });
        });
    }

    /// <summary>投影 <c>SpaceNodeInfo</c>（写面复用同一白名单，避免两套字段口径）。</summary>
    private static JsonObject ProjectNode(SpaceNodeInfo? node, bool created)
    {
        if (node is null)
        {
            return new JsonObject
            {
                ["ok"] = false,
                ["message"] = created
                    ? "平台未返回新节点信息——请用 wiki.list_nodes 确认是否已创建"
                    : "平台未返回移动后的节点信息——请用 wiki.list_nodes 确认实际位置",
            };
        }

        return new JsonObject
        {
            ["ok"] = true,
            ["node_token"] = node.NodeToken,
            ["obj_token"] = node.ObjToken,
            ["obj_type"] = node.ObjType,
            ["title"] = node.Title,
            ["space_id"] = node.SpaceId,
            ["parent_node_token"] = node.ParentNodeToken,
        };
    }
}
