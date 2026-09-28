// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.AI.FeishuTools.Internal;

/// <summary>
/// Wiki 双工具执行器（<c>wiki.get_node</c> / <c>wiki.list_nodes</c>）：
/// 节点白名单 node_token/title/obj_type/obj_token/parent_node_token（枢纽定位，§3.3.2）。
/// </summary>
internal sealed class WikiTools(Mud.Feishu.IFeishuTenantV2WikiNodes wikiNodesClient, IOptions<FeishuAgentOptions> options)
{
    private readonly Mud.Feishu.IFeishuTenantV2WikiNodes _wikiNodesClient = wikiNodesClient
        ?? throw new ArgumentNullException(nameof(wikiNodesClient));
    private readonly int _maxResultLength = (options ?? throw new ArgumentNullException(nameof(options))).Value.MaxToolResultLength;

    /// <summary>wiki.get_node：解析节点信息（单对象）。</summary>
    public async Task<FeishuToolResult> GetNodeAsync(IReadOnlyDictionary<string, object?> arguments, CancellationToken cancellationToken)
    {
        try
        {
            var token = ToolArgs.RequireString(arguments, "token");
            var objType = ToolArgs.OptionalString(arguments, "obj_type") ?? "wiki";

            var outcome = FeishuApiResultReader.Read(await _wikiNodesClient
                .GetNodeSpaceInfoAsync(token, objType, cancellationToken)
                .ConfigureAwait(false));
            if (!outcome.Ok)
            {
                return FeishuToolResult.FromError(FeishuToolBinding.StructuredError(FeishuToolNames.WikiGetNode, outcome.Code, outcome.ErrorText!));
            }

            var node = outcome.Data!.Node;
            var envelope = new JsonObject
            {
                ["node"] = ProjectNode(node),
            };
            return FeishuToolResult.FromText(ToolResultText.TruncateJson(envelope.ToJsonString(), _maxResultLength));
        }
        catch (ArgumentException ex)
        {
            return FeishuToolResult.FromError(FeishuToolBinding.StructuredError(FeishuToolNames.WikiGetNode, ex.Message));
        }
    }

    /// <summary>wiki.list_nodes：列出子节点（分页）。</summary>
    public async Task<FeishuToolResult> ListNodesAsync(IReadOnlyDictionary<string, object?> arguments, CancellationToken cancellationToken)
    {
        try
        {
            var spaceId = ToolArgs.RequireString(arguments, "space_id");
            var parentNodeToken = ToolArgs.OptionalString(arguments, "parent_node_token");
            var pageToken = ToolArgs.OptionalString(arguments, "page_token");

            var outcome = FeishuApiResultReader.Read(await _wikiNodesClient
                .GetSpaceNodesPageListAsync(spaceId, parentNodeToken, PageSizes.WikiNodes, pageToken, cancellationToken)
                .ConfigureAwait(false));
            if (!outcome.Ok)
            {
                return FeishuToolResult.FromError(FeishuToolBinding.StructuredError(FeishuToolNames.WikiListNodes, outcome.Code, outcome.ErrorText!));
            }

            var data = outcome.Data!;
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

            return FeishuToolResult.FromText(ToolResultText.TruncateJson(envelope.ToJsonString(), _maxResultLength));
        }
        catch (ArgumentException ex)
        {
            return FeishuToolResult.FromError(FeishuToolBinding.StructuredError(FeishuToolNames.WikiListNodes, ex.Message));
        }
    }

    private static JsonObject ProjectNode(Mud.Feishu.DataModels.Wiki.SpaceNodeInfo? node) => new()
    {
        ["node_token"] = node?.NodeToken,
        ["title"] = node?.Title,
        ["obj_type"] = node?.ObjType,
        ["obj_token"] = node?.ObjToken,
        ["parent_node_token"] = node?.ParentNodeToken,
        ["has_child"] = node?.HasChild,
    };
}
