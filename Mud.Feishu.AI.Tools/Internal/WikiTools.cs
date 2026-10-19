// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.DataModels.Wiki;

namespace Mud.Feishu.AI.Tools.Internal;

/// <summary>
/// Wiki 域执行器（节点面 <c>get_node</c> / <c>list_nodes</c> / 写面 3 枚 + 空间与成员面 6 枚）：
/// 节点白名单 node_token/title/obj_type/obj_token/parent_node_token（枢纽定位，§3.3.2）。
/// </summary>
/// <remarks>执行骨架（catch/回填/截断）由 <see cref="ToolExecutor"/> 承担（WP3）；本类只保留参数校验与投影语义。
/// 空间侧客户端 <c>IFeishuTenantV2Wiki</c> 为<b>软依赖</b>（可空）：缺席时只让空间类工具给出可执行提示，
/// 不牵连节点类工具。</remarks>
internal sealed class WikiTools(
    Mud.Feishu.IFeishuTenantV2WikiNodes wikiNodesClient,
    Mud.Feishu.IFeishuTenantV2Wiki? wikiSpaceClient,
    IOptions<FeishuAgentOptions> options)
{
    private readonly Mud.Feishu.IFeishuTenantV2WikiNodes _wikiNodesClient = wikiNodesClient
        ?? throw new ArgumentNullException(nameof(wikiNodesClient));
    private readonly Mud.Feishu.IFeishuTenantV2Wiki? _wikiSpaceClient = wikiSpaceClient;
    private readonly int _maxResultLength = ToolExecutor.Require(options).MaxToolResultLength;
    private readonly int _maxAutoFetchItems = ToolExecutor.Require(options).MaxAutoFetchItems;
    private readonly int _maxAutoFetchPages = ToolExecutor.Require(options).MaxAutoFetchPages;

    /// <summary>wiki.get_node：解析节点信息（单对象）。</summary>
    [ToolHandler(typeof(IFeishuTenantWikiGetNodeTool))]
    public Task<ToolResult> GetNodeAsync(IReadOnlyDictionary<string, object?> arguments, CancellationToken cancellationToken)
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
    [ToolHandler(typeof(IFeishuTenantWikiListNodesTool))]
    public Task<ToolResult> ListNodesAsync(IReadOnlyDictionary<string, object?> arguments, CancellationToken cancellationToken)
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
    [ToolHandler(typeof(IFeishuTenantWikiCreateNodeTool))]
    public Task<ToolResult> CreateNodeAsync(IReadOnlyDictionary<string, object?> arguments, CancellationToken cancellationToken)
    {
        var executor = new ToolExecutor(FeishuToolNames.WikiCreateNode, _maxResultLength);
        return executor.RunAsync(async () =>
        {
            var args = WikiCreateNodeArgs.Unpack(arguments);

            if (ToolDryRun.IsRequested(args.DryRun))
            {
                return ToolResult.FromText(ToolDryRun.Describe(
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
    [ToolHandler(typeof(IFeishuTenantWikiMoveNodeTool))]
    public Task<ToolResult> MoveNodeAsync(IReadOnlyDictionary<string, object?> arguments, CancellationToken cancellationToken)
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
                return ToolResult.FromText(ToolDryRun.Describe(
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
    [ToolHandler(typeof(IFeishuTenantWikiMoveDocsToSpaceTool))]
    public Task<ToolResult> MoveDocsToSpaceAsync(IReadOnlyDictionary<string, object?> arguments, CancellationToken cancellationToken)
    {
        var executor = new ToolExecutor(FeishuToolNames.WikiMoveDocsToSpace, _maxResultLength);
        return executor.RunAsync(async () =>
        {
            var args = WikiMoveDocsToSpaceArgs.Unpack(arguments);

            if (ToolDryRun.IsRequested(args.DryRun))
            {
                return ToolResult.FromText(ToolDryRun.Describe(
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

    // ────────── 空间与成员（space_id 的来源层；节点类工具此前只能靠用户口述拿到空间 ID） ──────────

    /// <summary>wiki.space_list：列出当前调用身份有权限的知识空间（分页）。</summary>
    [ToolHandler(typeof(IFeishuTenantWikiListSpacesTool))]
    public Task<ToolResult> ListSpacesAsync(IReadOnlyDictionary<string, object?> arguments, CancellationToken cancellationToken)
    {
        var executor = new ToolExecutor(FeishuToolNames.WikiSpaceList, _maxResultLength);
        return executor.RunAsync(async () =>
        {
            var client = RequireSpace(executor.ToolName);
            var args = WikiSpaceListArgs.Unpack(arguments);

            if (args.FetchAll == true)
            {
                var maxItems = ToolPagination.ResolveMaxItems(args.MaxItems, _maxAutoFetchItems);
                var aggregated = await ToolPagination.AggregateOutcomesAsync<ApiPageListResult<SpaceInfo>>(
                    async (token, ct) => FeishuApiResultReader.Read(await client
                        .GetSpacesPageListAsync(PageSizes.WikiSpaces, token, ct)
                        .ConfigureAwait(false)),
                    page => (page.HasMore, page.PageToken),
                    page => ProjectSpaces(page)["items"]!.AsArray(),
                    maxItems,
                    ToolPagination.ResolveMaxPages(_maxAutoFetchPages),
                    cancellationToken).ConfigureAwait(false);
                return executor.FromPagedResult(aggregated, maxItems);
            }

            var outcome = FeishuApiResultReader.Read(await client
                .GetSpacesPageListAsync(PageSizes.WikiSpaces, args.PageToken, cancellationToken)
                .ConfigureAwait(false));
            return executor.FromApi(outcome, ProjectSpaces);
        });
    }

    /// <summary>wiki.space_get：单个知识空间信息（含 visibility/space_type，是 member_add 的前置判据）。</summary>
    [ToolHandler(typeof(IFeishuTenantWikiGetSpaceTool))]
    public Task<ToolResult> GetSpaceAsync(IReadOnlyDictionary<string, object?> arguments, CancellationToken cancellationToken)
    {
        var executor = new ToolExecutor(FeishuToolNames.WikiSpaceGet, _maxResultLength);
        return executor.RunAsync(async () =>
        {
            var client = RequireSpace(executor.ToolName);
            var args = WikiSpaceGetArgs.Unpack(arguments);

            var outcome = FeishuApiResultReader.Read(await client
                .GetSpaceInfoAsync(args.SpaceId, args.Lang ?? "en", cancellationToken)
                .ConfigureAwait(false));
            return executor.FromApi(outcome, static data => new JsonObject
            {
                ["space"] = ProjectSpace(data.Space),
            });
        });
    }

    /// <summary>wiki.member_list：列出空间成员（member_remove 的硬前置——三元组只有列出来才知道）。</summary>
    [ToolHandler(typeof(IFeishuTenantWikiListMembersTool))]
    public Task<ToolResult> ListMembersAsync(IReadOnlyDictionary<string, object?> arguments, CancellationToken cancellationToken)
    {
        var executor = new ToolExecutor(FeishuToolNames.WikiMemberList, _maxResultLength);
        return executor.RunAsync(async () =>
        {
            var client = RequireSpace(executor.ToolName);
            var args = WikiMemberListArgs.Unpack(arguments);

            if (args.FetchAll == true)
            {
                var maxItems = ToolPagination.ResolveMaxItems(args.MaxItems, _maxAutoFetchItems);
                var aggregated = await ToolPagination.AggregateOutcomesAsync<SpaceMemberResult>(
                    async (token, ct) => FeishuApiResultReader.Read(await client
                        .GetSpaceMemberPageListAsync(args.SpaceId, PageSizes.WikiMembers, token, ct)
                        .ConfigureAwait(false)),
                    page => (page.HasMore, page.PageToken),
                    page => ProjectMembers(page)["items"]!.AsArray(),
                    maxItems,
                    ToolPagination.ResolveMaxPages(_maxAutoFetchPages),
                    cancellationToken).ConfigureAwait(false);
                return executor.FromPagedResult(aggregated, maxItems);
            }

            var outcome = FeishuApiResultReader.Read(await client
                .GetSpaceMemberPageListAsync(args.SpaceId, PageSizes.WikiMembers, args.PageToken, cancellationToken)
                .ConfigureAwait(false));
            return executor.FromApi(outcome, ProjectMembers);
        });
    }

    /// <summary>wiki.member_add：为空间添加成员/管理员（授权动作，<c>dry_run=true</c> 时只预演）。</summary>
    [ToolHandler(typeof(IFeishuTenantWikiAddMemberTool))]
    public Task<ToolResult> AddMemberAsync(IReadOnlyDictionary<string, object?> arguments, CancellationToken cancellationToken)
    {
        var executor = new ToolExecutor(FeishuToolNames.WikiMemberAdd, _maxResultLength);
        return executor.RunAsync(async () =>
        {
            var client = RequireSpace(executor.ToolName);
            var args = WikiMemberAddArgs.Unpack(arguments);

            // 校验先于预演：闭集外的值即便预演也只是"看起来会成功"，下游照样会静默失败。
            ValidateMemberType(args.MemberType);
            ValidateMemberRole(args.MemberRole);

            if (ToolDryRun.IsRequested(args.DryRun))
            {
                return ToolResult.FromText(ToolDryRun.Describe(
                    executor.ToolName, "POST", $"/open-apis/wiki/v2/spaces/{args.SpaceId}/members",
                    ToolDryRun.IdempotencyNote(null),
                    ("space_id", args.SpaceId.Length), ("member_type", args.MemberType.Length),
                    ("member_id", args.MemberId.Length), ("member_role", args.MemberRole.Length)));
            }

            var outcome = FeishuApiResultReader.Read(await client
                .CreateSpaceMemberAsync(
                    args.SpaceId,
                    new CreateSpaceMemberRequest
                    {
                        MemberType = args.MemberType,
                        MemberId = args.MemberId,
                        MemberRole = args.MemberRole,
                    },
                    args.NeedNotification,
                    cancellationToken)
                .ConfigureAwait(false));

            return executor.FromApi(outcome, static data => new JsonObject
            {
                ["space"] = ProjectSpace(data.Space),
                ["note"] = "成员/管理员已添加；如平台返回的空间信息未含该成员，请用 wiki.member_list 复核",
            });
        });
    }

    /// <summary>wiki.member_remove：移除空间成员/管理员（high-risk-write，<c>dry_run=true</c> 时只预演）。</summary>
    [ToolHandler(typeof(IFeishuTenantWikiRemoveMemberTool))]
    public Task<ToolResult> RemoveMemberAsync(IReadOnlyDictionary<string, object?> arguments, CancellationToken cancellationToken)
    {
        var executor = new ToolExecutor(FeishuToolNames.WikiMemberRemove, _maxResultLength);
        return executor.RunAsync(async () =>
        {
            var client = RequireSpace(executor.ToolName);
            var args = WikiMemberRemoveArgs.Unpack(arguments);

            ValidateMemberType(args.MemberType);
            ValidateMemberRole(args.MemberRole);

            if (ToolDryRun.IsRequested(args.DryRun))
            {
                return ToolResult.FromText(ToolDryRun.Describe(
                    executor.ToolName, "DELETE",
                    $"/open-apis/wiki/v2/spaces/{args.SpaceId}/members/{args.MemberId}",
                    ToolDryRun.IdempotencyNote(null),
                    ("space_id", args.SpaceId.Length), ("member_id", args.MemberId.Length),
                    ("member_type", args.MemberType.Length), ("member_role", args.MemberRole.Length)));
            }

            // 平台按 member_id + member_type + member_role 三元组定位；member_id 走路径参数。
            var outcome = FeishuApiResultReader.Read(await client
                .DeleteSpaceMemberAsync(
                    args.SpaceId,
                    args.MemberId,
                    new DeleteSpaceMemberRequest
                    {
                        MemberType = args.MemberType,
                        MemberRole = args.MemberRole,
                    },
                    cancellationToken)
                .ConfigureAwait(false));

            return executor.FromApi(outcome, static data => new JsonObject
            {
                ["removed"] = true,
                ["member_type"] = data.Member.MemberType,
                ["member_id"] = data.Member.MemberId,
                ["member_role"] = data.Member.MemberRole,
            });
        });
    }

    /// <summary>wiki.node_copy：复制节点副本到指定位置（原节点保留；<c>dry_run=true</c> 时只预演）。</summary>
    [ToolHandler(typeof(IFeishuTenantWikiCopyNodeTool))]
    public Task<ToolResult> CopyNodeAsync(IReadOnlyDictionary<string, object?> arguments, CancellationToken cancellationToken)
    {
        var executor = new ToolExecutor(FeishuToolNames.WikiNodeCopy, _maxResultLength);
        return executor.RunAsync(async () =>
        {
            var args = WikiNodeCopyArgs.Unpack(arguments);

            // 与 move_node 同款防线：目标全空时平台不会复制，但调用仍返回成功（空操作）。
            if (string.IsNullOrWhiteSpace(args.TargetParentToken) && string.IsNullOrWhiteSpace(args.TargetSpaceId))
            {
                throw new ArgumentException(
                    "target_parent_token 与 target_space_id 至少要提供一个——两者都为空时平台不接受复制请求，"
                    + "本工具已提前拒绝，避免模型把空操作读成'已复制'");
            }

            if (ToolDryRun.IsRequested(args.DryRun))
            {
                return ToolResult.FromText(ToolDryRun.Describe(
                    executor.ToolName, "POST",
                    $"/open-apis/wiki/v2/spaces/{args.SpaceId}/nodes/{args.NodeToken}/copy",
                    ToolDryRun.IdempotencyNote(null),
                    ("space_id", args.SpaceId.Length), ("node_token", args.NodeToken.Length)));
            }

            var outcome = FeishuApiResultReader.Read(await _wikiNodesClient
                .CopySpaceNodeAsync(
                    args.SpaceId,
                    args.NodeToken,
                    new CopySpaceNodeRequest
                    {
                        TargetParentToken = args.TargetParentToken,
                        TargetSpaceId = args.TargetSpaceId,
                        Title = args.Title,
                    },
                    cancellationToken)
                .ConfigureAwait(false));

            return executor.FromApi(outcome, data => ProjectNode(data.Node, created: true));
        });
    }

    /// <summary>space_list 投影：items（空间白名单）+ 翻页契约 + 空结果释义。</summary>
    private static JsonObject ProjectSpaces(ApiPageListResult<SpaceInfo> data)
    {
        var envelope = ToolResultJsons.PageEnvelope(data.HasMore, data.PageToken);
        var items = data.Items ?? [];
        foreach (var space in items)
        {
            envelope["items"]!.AsArray().AddNode(ProjectSpace(space));
        }

        if (items.Count == 0)
        {
            envelope["note"] = "平台按可见性过滤：列表为空表示**当前调用身份没有被加入任何知识空间**，"
                + "不代表企业里没有知识库（须先把应用/机器人加为空间成员或管理员，见 wiki.member_add 的前提说明）";
        }

        return envelope;
    }

    /// <summary>member_list 投影：items（成员三元组）+ 翻页契约。</summary>
    private static JsonObject ProjectMembers(SpaceMemberResult data)
    {
        var envelope = ToolResultJsons.PageEnvelope(data.HasMore, data.PageToken);
        foreach (var member in data.Members ?? [])
        {
            envelope["items"]!.AsArray().AddNode(new JsonObject
            {
                ["member_type"] = member.MemberType,
                ["member_id"] = member.MemberId,
                ["member_role"] = member.MemberRole,
            });
        }

        return envelope;
    }

    private static JsonObject ProjectSpace(SpaceInfo? space) => new()
    {
        ["space_id"] = space?.SpaceId,
        ["name"] = space?.Name,
        ["description"] = space?.Description,
        ["space_type"] = space?.SpaceType,
        ["visibility"] = space?.Visibility,
        ["open_sharing"] = space?.OpenSharing,
    };

    /// <summary>member_type 取值闭集（官方：openid / unionid / userid / email / openchat / opendepartmentid）。</summary>
    private static readonly HashSet<string> ValidMemberTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "openid", "unionid", "userid", "email", "openchat", "opendepartmentid",
    };

    /// <summary>member_role 取值闭集（官方：admin / member）。</summary>
    private static readonly HashSet<string> ValidMemberRoles = new(StringComparer.OrdinalIgnoreCase)
    {
        "admin", "member",
    };

    private static void ValidateMemberType(string memberType)
    {
        if (!ValidMemberTypes.Contains(memberType))
        {
            throw new ArgumentException(
                $"member_type '{memberType}' 不合法。可用值：{string.Join(" / ", ValidMemberTypes.OrderBy(static x => x, StringComparer.Ordinal))}");
        }

        // 本工具固定走 tenant access token，平台在该身份下拒绝部门 ID。
        if (string.Equals(memberType, "opendepartmentid", StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException(
                "member_type=opendepartmentid 在应用身份（tenant access token）下不被平台支持——"
                + "请改用 openid / unionid / userid / email / openchat");
        }
    }

    private static void ValidateMemberRole(string memberRole)
    {
        if (!ValidMemberRoles.Contains(memberRole))
        {
            throw new ArgumentException(
                $"member_role '{memberRole}' 不合法。可用值：admin（管理员） / member（成员）");
        }
    }

    /// <summary>取空间侧软依赖客户端；缺席时给出<b>可执行</b>提示（宿主该启用什么），而不是空引用。</summary>
    private Mud.Feishu.IFeishuTenantV2Wiki RequireSpace(string toolName)
        => _wikiSpaceClient ?? throw new ArgumentException(
            $"{toolName} 需要 IFeishuTenantV2Wiki——宿主须启用 AddWikiApi 的空间侧客户端；"
            + "未启用时本工具不在工具列表中（软缺席，不影响 wiki 其它工具）");

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
