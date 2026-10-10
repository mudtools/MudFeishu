// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using System.Text.Json;
using System.Text.Json.Nodes;

using Mud.Feishu.AI.Tools.Tools;
using Mud.Feishu.DataModels;
using Mud.Feishu.DataModels.Board;

namespace Mud.Feishu.AI.Tools.Internal;

/// <summary>
/// Board 画板工具执行器（R7 / A4：6 个工具，只读 2 + 写 4）。
/// </summary>
/// <remarks>
/// <para>
/// <b>软依赖</b>：画板客户端为可空注入——宿主未启用画板 API 时执行器软缺席（与 MinutesReadTools 同口径）。
/// </para>
/// <para>
/// <b>画板 ID 获取链路</b>：whiteboard_id 来自文档块——<c>docx.get_document_blocks</c> 中
/// <c>block_type=43</c> 的 block.token 即画板 ID。Guidance <c>board.md</c> 必须写清这条路径。
/// </para>
/// <para>
/// <b>二进制防线（A10）</b>：<c>DownloadWhiteboardImageAsync</c> 返回 <c>byte[]</c> → 不策展。
/// </para>
/// </remarks>
internal sealed class BoardTools(Mud.Feishu.IFeishuTenantV1Board? boardClient = null)
{
    private readonly Mud.Feishu.IFeishuTenantV1Board? _boardClient = boardClient;

    // ─────────────────────────── 只读面（2 个） ───────────────────────────

    [FeishuToolHandler(typeof(IFeishuTenantBoardGetThemeTool))]
    public Task<FeishuToolResult> GetWhiteboardThemeAsync(IReadOnlyDictionary<string, object?> arguments, CancellationToken cancellationToken)
    {
        var executor = new ToolExecutor(FeishuToolNames.BoardGetTheme);
        return executor.RunAsync(async () =>
        {
            var args = BoardGetThemeArgs.Unpack(arguments);

            var outcome = FeishuApiResultReader.Read(await Require(executor.ToolName)
                .GetWhiteboardThemeAsync(args.WhiteboardId, cancellationToken)
                .ConfigureAwait(false));

            return executor.FromApiUntruncated(outcome, data =>
            {
                // WhiteboardsTheme 只有一个 Theme 字符串属性（主题标识，如 classic / vibrant_color 等）。
                var theme = data.Theme;
                if (string.IsNullOrEmpty(theme))
                {
                    return new JsonObject
                    {
                        ["found"] = false,
                        ["whiteboard_id"] = args.WhiteboardId,
                        ["message"] = "未找到画板主题，请确认 whiteboard_id 是否正确。",
                    };
                }

                return new JsonObject
                {
                    ["found"] = true,
                    ["whiteboard_id"] = args.WhiteboardId,
                    ["theme"] = theme,
                };
            });
        });
    }

    [FeishuToolHandler(typeof(IFeishuTenantBoardListNodesTool))]
    public Task<FeishuToolResult> GetWhiteboardNodesAsync(IReadOnlyDictionary<string, object?> arguments, CancellationToken cancellationToken)
    {
        var executor = new ToolExecutor(FeishuToolNames.BoardListNodes);
        return executor.RunAsync(async () =>
        {
            var args = BoardListNodesArgs.Unpack(arguments);

            var outcome = FeishuApiResultReader.Read(await Require(executor.ToolName)
                .GetWhiteboardNodesAsync(args.WhiteboardId, cancellationToken: cancellationToken)
                .ConfigureAwait(false));

            return executor.FromApiUntruncated(outcome, data =>
            {
                var nodes = new JsonArray();
                foreach (var node in data.Nodes ?? [])
                {
                    // WhiteboardNodeInfo.Id（不是 NodeId）；Children 是 string[]?（子节点 ID 列表）。
                    nodes.AddNode(new JsonObject
                    {
                        ["node_id"] = node.Id,
                        ["parent_id"] = node.ParentId,
                        ["type"] = node.Type,
                        ["children"] = new JsonArray([.. (node.Children ?? []).Select(static c => (JsonNode?)c)]),
                    });
                }

                return new JsonObject
                {
                    ["whiteboard_id"] = args.WhiteboardId,
                    ["nodes"] = nodes,
                };
            });
        });
    }

    // ─────────────────────────── 写面（4 个） ───────────────────────────

    [FeishuToolHandler(typeof(IFeishuTenantBoardUpdateThemeTool))]
    public Task<FeishuToolResult> UpdateWhiteboardThemeAsync(IReadOnlyDictionary<string, object?> arguments, CancellationToken cancellationToken)
    {
        var executor = new ToolExecutor(FeishuToolNames.BoardUpdateTheme);
        return executor.RunAsync(async () =>
        {
            var args = BoardUpdateThemeArgs.Unpack(arguments);

            // UpdateWhiteboardThemeRequest 继承自 WhiteboardsTheme，属性是 Theme（不是 ThemeId）。
            var request = new UpdateWhiteboardThemeRequest
            {
                Theme = args.ThemeId,
            };

            if (ToolDryRun.IsRequested(args.DryRun))
            {
                return FeishuToolResult.FromText(ToolDryRun.Describe(
                    executor.ToolName, "PATCH", "/open-apis/board/v1/whiteboards/{whiteboard_id}",
                    ToolDryRun.IdempotencyNote(null),
                    ("theme", args.ThemeId.Length)));
            }

            // UpdateWhiteboardThemeAsync 返回 FeishuNullDataApiResult?（无 Data 载荷）。
            // 走 RequireNullDataSuccess 模式：FeishuApiResultReader.Read<object> 在 Data 为 null 时
            // 会误判为失败，而 FeishuNullDataApiResult 的 Data 天然为 null。
            var result = await Require(executor.ToolName)
                .UpdateWhiteboardThemeAsync(args.WhiteboardId, request, cancellationToken)
                .ConfigureAwait(false);
            RequireNullDataSuccess(executor.ToolName, result);

            return FeishuToolResult.FromText(ToolResultJson.ToText(new JsonObject
            {
                ["whiteboard_id"] = args.WhiteboardId,
                ["theme"] = args.ThemeId,
                ["updated"] = true,
            }));
        });
    }

    [FeishuToolHandler(typeof(IFeishuTenantBoardRenderDslTool))]
    public Task<FeishuToolResult> CreatePlantumlWhiteboardNodeAsync(IReadOnlyDictionary<string, object?> arguments, CancellationToken cancellationToken)
    {
        var executor = new ToolExecutor(FeishuToolNames.BoardRenderDsl);
        return executor.RunAsync(async () =>
        {
            var args = BoardRenderDslArgs.Unpack(arguments);

            if (string.IsNullOrWhiteSpace(args.DslType) || (args.DslType != "plantuml" && args.DslType != "mermaid"))
            {
                throw new ArgumentException("dsl_type 必须为 'plantuml' 或 'mermaid'");
            }

            if (string.IsNullOrWhiteSpace(args.Content))
            {
                throw new ArgumentException("content 不能为空");
            }

            // CreatePlantumlWhiteboardNodeRequest: PlantUmlCode + SyntaxType (1=PlantUML, 2=Mermaid)。
            var request = new CreatePlantumlWhiteboardNodeRequest
            {
                PlantUmlCode = args.Content,
                SyntaxType = args.DslType == "plantuml" ? 1 : 2,
            };

            if (ToolDryRun.IsRequested(args.DryRun))
            {
                return FeishuToolResult.FromText(ToolDryRun.Describe(
                    executor.ToolName, "POST", "/open-apis/board/v1/whiteboards/{whiteboard_id}/nodes/plantuml",
                    ToolDryRun.IdempotencyNote(null),
                    ("dsl_type", args.DslType.Length), ("content", args.Content.Length)));
            }

            // CreatePlantumlWhiteboardNodeAsync 返回 FeishuNullDataApiResult?（无 Data 载荷）。
            var result = await Require(executor.ToolName)
                .CreatePlantumlWhiteboardNodeAsync(args.WhiteboardId, request, cancellationToken)
                .ConfigureAwait(false);
            RequireNullDataSuccess(executor.ToolName, result);

            return FeishuToolResult.FromText(ToolResultJson.ToText(new JsonObject
            {
                ["whiteboard_id"] = args.WhiteboardId,
                ["dsl_type"] = args.DslType,
                ["rendered"] = true,
            }));
        });
    }

    [FeishuToolHandler(typeof(IFeishuTenantBoardCreateNodesTool))]
    public Task<FeishuToolResult> CreateWhiteboardNodeAsync(IReadOnlyDictionary<string, object?> arguments, CancellationToken cancellationToken)
    {
        var executor = new ToolExecutor(FeishuToolNames.BoardCreateNodes);
        return executor.RunAsync(async () =>
        {
            var args = BoardCreateNodesArgs.Unpack(arguments);

            if (string.IsNullOrWhiteSpace(args.NodesJson))
            {
                throw new ArgumentException("nodes_json 不能为空");
            }

            // 解析节点 JSON（模型传入的节点数组 JSON 字符串）→ WhiteboardNode[]。
            // CreateWhiteboardNodeRequest.Nodes 是 WhiteboardNode[]?（不是 JsonNode）。
            List<WhiteboardNode> nodeList;
            try
            {
                nodeList = JsonSerializer.Deserialize<List<WhiteboardNode>>(args.NodesJson)
                    ?? throw new ArgumentException("nodes_json 反序列化结果为 null");
                if (nodeList.Count == 0)
                {
                    throw new ArgumentException("nodes_json 必须为非空 JSON 数组");
                }
            }
            catch (JsonException ex)
            {
                throw new ArgumentException($"nodes_json 解析失败：{ex.Message}");
            }

            var request = new CreateWhiteboardNodeRequest
            {
                Nodes = [.. nodeList],
            };

            if (ToolDryRun.IsRequested(args.DryRun))
            {
                return FeishuToolResult.FromText(ToolDryRun.Describe(
                    executor.ToolName, "POST", "/open-apis/board/v1/whiteboards/{whiteboard_id}/nodes",
                    ToolDryRun.IdempotencyNote(args.IdempotencyKey),
                    ("nodes", nodeList.Count)));
            }

            var outcome = FeishuApiResultReader.Read(await Require(executor.ToolName)
                .CreateWhiteboardNodeAsync(
                    args.WhiteboardId,
                    request,
                    client_token: args.IdempotencyKey,
                    cancellationToken: cancellationToken)
                .ConfigureAwait(false));

            return executor.FromApiUntruncated(outcome, data =>
            {
                // CreateWhiteboardNodeResult.Ids 是 string[]（创建的节点 ID 列表），不是 Nodes。
                var nodes = new JsonArray();
                foreach (var id in data.Ids ?? [])
                {
                    nodes.AddNode(new JsonObject
                    {
                        ["node_id"] = id,
                    });
                }

                return new JsonObject
                {
                    ["whiteboard_id"] = args.WhiteboardId,
                    ["created_nodes"] = nodes,
                };
            });
        });
    }

    [FeishuToolHandler(typeof(IFeishuTenantBoardDeleteNodesTool))]
    public Task<FeishuToolResult> BatchDeleteWhiteboardNodeAsync(IReadOnlyDictionary<string, object?> arguments, CancellationToken cancellationToken)
    {
        var executor = new ToolExecutor(FeishuToolNames.BoardDeleteNodes);
        return executor.RunAsync(async () =>
        {
            var args = BoardDeleteNodesArgs.Unpack(arguments);

            if (args.NodeIds is null || args.NodeIds.Length == 0)
            {
                throw new ArgumentException("node_ids 不能为空数组");
            }

            // BatchDeleteWhiteboardNodeRequest.Ids（不是 NodeIds）。
            var request = new BatchDeleteWhiteboardNodeRequest
            {
                Ids = [.. args.NodeIds],
            };

            if (ToolDryRun.IsRequested(args.DryRun))
            {
                return FeishuToolResult.FromText(ToolDryRun.Describe(
                    executor.ToolName, "DELETE", "/open-apis/board/v1/whiteboards/{whiteboard_id}/nodes/batch_delete",
                    "预演不占坑：本操作无幂等键（删除不可撤销，请核对 node_ids 后去掉 dry_run 重放）",
                    ("node_ids", args.NodeIds.Length)));
            }

            // BatchDeleteWhiteboardNodeAsync 返回 FeishuApiResult<BatchDeleteWhiteboardNodeResult>?。
            // 成功时 Data 可能为 null（无业务载荷）——只检查 Code，不走 Read<T>（Data null 会误判失败）。
            var deleteResult = await Require(executor.ToolName)
                .BatchDeleteWhiteboardNodeAsync(args.WhiteboardId, request, cancellationToken: cancellationToken)
                .ConfigureAwait(false);
            RequireResultCode(executor.ToolName, deleteResult);

            return FeishuToolResult.FromText(ToolResultJson.ToText(new JsonObject
            {
                ["whiteboard_id"] = args.WhiteboardId,
                ["deleted_count"] = args.NodeIds.Length,
                ["deleted"] = true,
            }));
        });
    }

    /// <summary>取客户端；缺席时给出<b>可执行</b>提示。</summary>
    private Mud.Feishu.IFeishuTenantV1Board Require(string toolName)
        => _boardClient
            ?? throw new ArgumentException(
                $"{toolName} 需要 IFeishuTenantV1Board——宿主须启用画板（Board）API；"
                + "未启用时本工具不在工具列表中（软缺席，不影响其它域工具）");

    /// <summary>
    /// 解包 <c>FeishuNullDataApiResult</c>（不走 <c>FeishuApiResultReader.Read&lt;T&gt;</c>，
    /// T 不可推断且 Data 天然为 null）。
    /// </summary>
    private static void RequireNullDataSuccess(string toolName, FeishuNullDataApiResult? result)
    {
        if (result is null)
        {
            throw new ArgumentException("飞书接口无响应（result 为空）");
        }

        if (result.Code != 0)
        {
            throw new ArgumentException(
                FeishuToolBinding.StructuredError(
                    toolName,
                    result.Code,
                    $"飞书接口返回错误 code={result.Code.ToString(CultureInfo.InvariantCulture)}, "
                    + $"msg={result.Msg ?? "(无错误信息)"}"));
        }
    }

    /// <summary>
    /// 泛型版本：只检查 Code，不检查 Data（成功时 Data 可能为 null）。
    /// </summary>
    private static void RequireResultCode<T>(string toolName, FeishuApiResult<T>? result) where T : class
    {
        if (result is null)
        {
            throw new ArgumentException("飞书接口无响应（result 为空）");
        }

        if (result.Code != 0)
        {
            throw new ArgumentException(
                FeishuToolBinding.StructuredError(
                    toolName,
                    result.Code,
                    $"飞书接口返回错误 code={result.Code.ToString(CultureInfo.InvariantCulture)}, "
                    + $"msg={result.Msg ?? "(无错误信息)"}"));
        }
    }
}
