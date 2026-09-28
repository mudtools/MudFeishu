// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.DataModels.Wiki;

namespace Mud.Feishu.AI.FeishuTools.Internal;

/// <summary>
/// Wiki 双工具执行器（<c>wiki.get_node</c> / <c>wiki.list_nodes</c>）：
/// 节点白名单 node_token/title/obj_type/obj_token/parent_node_token（枢纽定位，§3.3.2）。
/// </summary>
/// <remarks>执行骨架（catch/回填/截断）由 <see cref="ToolExecutor"/> 承担（WP3）；本类只保留参数校验与投影语义。</remarks>
internal sealed class WikiTools(Mud.Feishu.IFeishuTenantV2WikiNodes wikiNodesClient, IOptions<FeishuAgentOptions> options)
{
    private readonly Mud.Feishu.IFeishuTenantV2WikiNodes _wikiNodesClient = wikiNodesClient
        ?? throw new ArgumentNullException(nameof(wikiNodesClient));
    private readonly int _maxResultLength = (options ?? throw new ArgumentNullException(nameof(options))).Value.MaxToolResultLength;

    /// <summary>wiki.get_node：解析节点信息（单对象）。</summary>
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
    public Task<FeishuToolResult> ListNodesAsync(IReadOnlyDictionary<string, object?> arguments, CancellationToken cancellationToken)
    {
        var executor = new ToolExecutor(FeishuToolNames.WikiListNodes, _maxResultLength);
        return executor.RunAsync(async () =>
        {
            var args = WikiListNodesArgs.Unpack(arguments);

            var outcome = FeishuApiResultReader.Read(await _wikiNodesClient
                .GetSpaceNodesPageListAsync(args.SpaceId, args.ParentNodeToken, PageSizes.WikiNodes, args.PageToken, cancellationToken)
                .ConfigureAwait(false));
            return executor.FromApi(outcome, ProjectNodes);
        });
    }

    /// <summary>list_nodes 投影：items（节点白名单）+ 翻页契约。</summary>
    private static JsonObject ProjectNodes(ApiPageListResult<SpaceNodeInfo> data)
    {
        var envelope = new JsonObject
        {
            ["items"] = new JsonArray(),
            ["has_more"] = data.HasMore,
        };
        if (!string.IsNullOrEmpty(data.PageToken))
        {
            envelope["page_token"] = data.PageToken;
        }

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
}
