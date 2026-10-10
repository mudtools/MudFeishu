// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using System.Text.Json.Nodes;

using Mud.Feishu.AI.Tools.Tools;
using Mud.Feishu.DataModels.Spark;

namespace Mud.Feishu.AI.Tools.Internal;

/// <summary>
/// Spark 妙搭应用面执行器（R7 / A6：6 个工具，只读 2 + 写 4）。
/// </summary>
/// <remarks>
/// <para>
/// <b>软依赖</b>：客户端可空注入——宿主未启用妙搭 API 时给出"须启用"的可执行结构化错误。
/// </para>
/// <para>
/// <b>二进制/本地文件防线（A10 + DP-A6-2）</b>：<c>upload_html_release</c>（<c>[FormContent]</c> 本地 tar）、
/// <c>upload_app_icon</c>（本地文件）、<c>upload_storage</c>/<c>download_storage</c>（<c>byte[]</c>）、
/// <c>execute_sql</c>（任意 SQL）<b>均不策展</b>——模型无文件系统，且 SQL 是比 <c>feishu.api_call</c> 更宽的越权通道。
/// </para>
/// </remarks>
internal sealed class SparkAppTools(
    IOptions<FeishuAgentOptions> options,
    Mud.Feishu.IFeishuTenantV1SparkApp? tenantAppClient = null,
    Mud.Feishu.IFeishuUserV1SparkApp? userAppClient = null)
{
    private readonly Mud.Feishu.IFeishuTenantV1SparkApp? _tenantAppClient = tenantAppClient;
    private readonly Mud.Feishu.IFeishuUserV1SparkApp? _userAppClient = userAppClient;
    private readonly int _maxResultLength = (options ?? throw new ArgumentNullException(nameof(options))).Value.MaxToolResultLength;
    private readonly int _maxAutoFetchItems = (options ?? throw new ArgumentNullException(nameof(options))).Value.MaxAutoFetchItems;
    private readonly int _maxAutoFetchPages = (options ?? throw new ArgumentNullException(nameof(options))).Value.MaxAutoFetchPages;

    // ─────────────────────────── 只读面（3 个） ───────────────────────────

    /// <summary>spark.list_apps：列出妙搭应用（分页，fetch_all 自动翻页）。</summary>
    [FeishuToolHandler(typeof(IFeishuTenantSparkListAppsTool))]
    public Task<FeishuToolResult> GetAppListAsync(IReadOnlyDictionary<string, object?> arguments, CancellationToken cancellationToken)
    {
        var executor = new ToolExecutor(FeishuToolNames.SparkListApps, _maxResultLength);
        return executor.RunAsync(async () =>
        {
            var client = RequireTenant(executor.ToolName);
            var args = SparkListAppsArgs.Unpack(arguments);

            if (args.FetchAll == true)
            {
                var maxItems = ToolPagination.ResolveMaxItems(args.MaxItems, _maxAutoFetchItems);
                var aggregated = await ToolPagination.AggregateOutcomesAsync<GetAppListResult>(
                    async (token, ct) => FeishuApiResultReader.Read(await client
                        .GetAppListAsync(
                            new GetAppListQuery { PageSize = PageSizes.SparkApps, PageToken = token },
                            ct)
                        .ConfigureAwait(false)),
                    page => (page.HasMore, page.PageToken),
                    page => ProjectAppList(page)["items"]!.AsArray(),
                    maxItems,
                    ToolPagination.ResolveMaxPages(_maxAutoFetchPages),
                    cancellationToken).ConfigureAwait(false);
                return executor.FromPagedResult(aggregated, maxItems);
            }

            var outcome = FeishuApiResultReader.Read(await client
                .GetAppListAsync(
                    new GetAppListQuery { PageSize = PageSizes.SparkApps, PageToken = args.PageToken },
                    cancellationToken)
                .ConfigureAwait(false));

            return executor.FromApi(outcome, ProjectAppList);
        });
    }

    /// <summary>spark.get_app_analytics：获取应用运营数据总览。</summary>
    [FeishuToolHandler(typeof(IFeishuTenantSparkGetAppAnalyticsTool))]
    public Task<FeishuToolResult> GetAppAnalyticsOverviewAsync(IReadOnlyDictionary<string, object?> arguments, CancellationToken cancellationToken)
    {
        var executor = new ToolExecutor(FeishuToolNames.SparkGetAppAnalytics, _maxResultLength);
        return executor.RunAsync(async () =>
        {
            var client = RequireTenant(executor.ToolName);
            var args = SparkGetAppAnalyticsArgs.Unpack(arguments);

            var outcome = FeishuApiResultReader.Read(await client
                .GetAppAnalyticsOverviewAsync(
                    args.AppId,
                    args.StartTime,
                    args.EndTime,
                    cancellationToken)
                .ConfigureAwait(false));

            return executor.FromApi(outcome, data => new JsonObject
            {
                ["app_id"] = args.AppId,
                ["start_time"] = args.StartTime,
                ["end_time"] = args.EndTime,
                ["active_users"] = ProjectMetric(data.ActiveUsers),
                ["signups"] = ProjectMetric(data.Signups),
                ["page_views"] = ProjectMetric(data.PageViews),
            });
        });
    }

    /// <summary>spark.get_app_visibility：获取应用可用范围。</summary>
    [FeishuToolHandler(typeof(IFeishuUserSparkGetAppVisibilityTool))]
    public Task<FeishuToolResult> GetAppVisibilityAsync(IReadOnlyDictionary<string, object?> arguments, CancellationToken cancellationToken)
    {
        var executor = new ToolExecutor(FeishuToolNames.SparkGetAppVisibility, _maxResultLength);
        return executor.RunAsync(async () =>
        {
            var client = RequireUser(executor.ToolName);
            var args = SparkGetAppVisibilityArgs.Unpack(arguments);

            var outcome = FeishuApiResultReader.Read(await client
                .GetAppVisibilityAsync(args.AppId, args.UserIdType, cancellationToken)
                .ConfigureAwait(false));

            return executor.FromApi(outcome, data => ProjectVisibility(args.AppId, data));
        });
    }

    // ─────────────────────────── 写面（3 个） ───────────────────────────

    /// <summary>spark.create_app：创建妙搭应用（<c>dry_run=true</c> 时只预演）。</summary>
    [FeishuToolHandler(typeof(IFeishuUserSparkCreateAppTool))]
    public Task<FeishuToolResult> CreateAppAsync(IReadOnlyDictionary<string, object?> arguments, CancellationToken cancellationToken)
    {
        var executor = new ToolExecutor(FeishuToolNames.SparkCreateApp);
        return executor.RunAsync(async () =>
        {
            var args = SparkCreateAppArgs.Unpack(arguments);

            if (string.IsNullOrWhiteSpace(args.Name))
            {
                throw new ArgumentException("name 不能为空");
            }

            if (ToolDryRun.IsRequested(args.DryRun))
            {
                return FeishuToolResult.FromText(ToolDryRun.Describe(
                    executor.ToolName, "POST", "/open-apis/spark/v1/apps",
                    ToolDryRun.IdempotencyNote(null),
                    ("name", args.Name.Length), ("app_type", args.AppType?.Length ?? 0), ("description", args.Desc?.Length ?? 0)));
            }

            var client = RequireUser(executor.ToolName);

            var outcome = FeishuApiResultReader.Read(await client
                .CreateAppAsync(
                    new CreateAppRequest
                    {
                        Name = args.Name,
                        AppType = args.AppType,
                        Description = args.Desc,
                        IconUrl = args.IconUrl,
                    },
                    cancellationToken)
                .ConfigureAwait(false));

            return executor.FromApiUntruncated(outcome, ProjectAppResult);
        });
    }

    /// <summary>spark.patch_app：修改应用元信息（<c>dry_run=true</c> 时只预演）。</summary>
    [FeishuToolHandler(typeof(IFeishuUserSparkPatchAppTool))]
    public Task<FeishuToolResult> PatchAppAsync(IReadOnlyDictionary<string, object?> arguments, CancellationToken cancellationToken)
    {
        var executor = new ToolExecutor(FeishuToolNames.SparkPatchApp);
        return executor.RunAsync(async () =>
        {
            var args = SparkPatchAppArgs.Unpack(arguments);

            if (args.Name is null && args.Desc is null && args.IconUrl is null)
            {
                throw new ArgumentException("至少需要提供 name / desc / icon_url 之一（空 PATCH 无意义）");
            }

            if (ToolDryRun.IsRequested(args.DryRun))
            {
                return FeishuToolResult.FromText(ToolDryRun.Describe(
                    executor.ToolName, "PATCH", $"/open-apis/spark/v1/apps/{args.AppId}",
                    ToolDryRun.IdempotencyNote(null),
                    ("name", args.Name?.Length ?? 0), ("description", args.Desc?.Length ?? 0), ("icon_url", args.IconUrl?.Length ?? 0)));
            }

            var client = RequireUser(executor.ToolName);

            var outcome = FeishuApiResultReader.Read(await client
                .PatchAppAsync(
                    args.AppId,
                    new PatchAppRequest
                    {
                        Name = args.Name,
                        Description = args.Desc,
                        IconUrl = args.IconUrl,
                    },
                    cancellationToken)
                .ConfigureAwait(false));

            return executor.FromApiUntruncated(outcome, ProjectAppResult);
        });
    }

    /// <summary>spark.update_app_visibility：修改应用可用范围（<c>dry_run=true</c> 时只预演）。</summary>
    [FeishuToolHandler(typeof(IFeishuUserSparkUpdateAppVisibilityTool))]
    public Task<FeishuToolResult> UpdateAppVisibilityAsync(IReadOnlyDictionary<string, object?> arguments, CancellationToken cancellationToken)
    {
        var executor = new ToolExecutor(FeishuToolNames.SparkUpdateAppVisibility);
        return executor.RunAsync(async () =>
        {
            var args = SparkUpdateAppVisibilityArgs.Unpack(arguments);

            var request = ParseVisibilityRequest(args.VisibilityJson);

            if (ToolDryRun.IsRequested(args.DryRun))
            {
                return FeishuToolResult.FromText(ToolDryRun.Describe(
                    executor.ToolName, "PUT", $"/open-apis/spark/v1/apps/{args.AppId}/access-scope",
                    ToolDryRun.IdempotencyNote(null),
                    ("scope", request.Scope.Length),
                    ("users", request.Users?.Length ?? 0),
                    ("departments", request.Departments?.Length ?? 0),
                    ("chats", request.Chats?.Length ?? 0)));
            }

            var client = RequireUser(executor.ToolName);

            var result = await client
                .UpdateAppVisibilityAsync(args.AppId, request, user_id_type: null, cancellationToken)
                .ConfigureAwait(false);
            if (RequireNullDataSuccess(executor.ToolName, result) is { } failure)
            {
                return failure;
            }

            return FeishuToolResult.FromText(ToolResultJson.ToText(new JsonObject
            {
                ["app_id"] = args.AppId,
                ["scope"] = request.Scope,
                ["updated"] = true,
            }));
        });
    }

    // ─────────────────────────── 投影 / 解析辅助 ───────────────────────────

    /// <summary>应用列表投影：items（app_id/name/status/online_url/updated_at）+ 翻页契约。</summary>
    private static JsonObject ProjectAppList(GetAppListResult data)
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

        foreach (var app in data.Items ?? [])
        {
            envelope["items"]!.AsArray().AddNode(ProjectApp(app));
        }

        return envelope;
    }

    /// <summary>单应用投影（白名单：标识/名称/状态/在线地址——不含开发者与管理员 ID 列表）。</summary>
    private static JsonObject ProjectApp(App app) => new()
    {
        ["app_id"] = app.AppId,
        ["app_type"] = app.AppType,
        ["name"] = app.Name,
        ["description"] = app.Description,
        ["status"] = app.Status,
        ["is_published"] = app.IsPublished,
        ["online_url"] = app.OnlineUrl,
        ["updated_at"] = app.UpdatedAt,
    };

    /// <summary>create/patch 出参投影（AppResult.App）。</summary>
    private static JsonObject ProjectAppResult(AppResult data)
        => data.App is null
            ? new JsonObject
            {
                ["created"] = false,
                ["message"] = "飞书未返回应用载荷，请用 spark.list_apps 复核是否已写入。",
            }
            : ProjectApp(data.App);

    /// <summary>可见范围投影（scope + 三类主体 + 是否需登录 + 审批配置）。</summary>
    private static JsonObject ProjectVisibility(string appId, GetAppVisibilityResult data)
    {
        var envelope = new JsonObject
        {
            ["app_id"] = appId,
            ["scope"] = data.Scope,
            ["require_login"] = data.RequireLogin,
        };

        AddIfAny(envelope, "users", data.Users);
        AddIfAny(envelope, "departments", data.Departments);
        AddIfAny(envelope, "chats", data.Chats);

        if (data.ApplyConfig is { } apply)
        {
            envelope["apply_config"] = new JsonObject
            {
                ["enabled"] = apply.Enabled,
                ["approvers"] = new JsonArray([.. (apply.Approvers ?? []).Select(static x => (JsonNode?)x)]),
            };
        }

        return envelope;
    }

    /// <summary>运营指标投影（value/prev_value/diff/ratio）。</summary>
    private static JsonNode? ProjectMetric(AnalyticsMetric? metric)
        => metric is null
            ? null
            : new JsonObject
            {
                ["value"] = metric.Value,
                ["prev_value"] = metric.PrevValue,
                ["diff"] = metric.Diff,
                ["ratio"] = metric.Ratio,
            };

    /// <summary>可见范围配置 JSON 的**取值闭集**（未列出的键一律拒绝，防"静默忽略模型意图"）。</summary>
    private static readonly string[] VisibilityKeys =
        ["scope", "users", "departments", "chats", "apply_config", "require_login"];

    /// <summary>apply_config 的允许子键（enabled / approvers）。</summary>
    private static readonly string[] ApplyConfigKeys = ["enabled", "approvers"];

    /// <summary>scope 的取值闭集（官方：Public / Tenant / Range）。</summary>
    private static readonly string[] VisibilityScopes = ["Public", "Tenant", "Range"];

    /// <summary>
    /// 把模型的 <c>visibility_json</c> 解析为 <c>UpdateAppVisibilityRequest</c>。
    /// </summary>
    /// <remarks>
    /// 手工逐字段解析（不反射反序列化）：① 与 SDK 的 AOT 约束一致；
    /// ② <b>未知键一律拒绝</b>——静默丢弃会让模型以为已生效（越权面最容易蒙混过关的形态）。
    /// </remarks>
    private static UpdateAppVisibilityRequest ParseVisibilityRequest(string json)
    {
        JsonNode? node;
        try
        {
            node = JsonNode.Parse(json);
        }
        catch (System.Text.Json.JsonException ex)
        {
            throw new ArgumentException($"visibility_json 不是合法 JSON：{ex.Message}");
        }

        if (node is not JsonObject obj)
        {
            throw new ArgumentException("visibility_json 必须是 JSON 对象");
        }

        foreach (var pair in obj)
        {
            if (!VisibilityKeys.Contains(pair.Key, StringComparer.Ordinal))
            {
                throw new ArgumentException(
                    $"visibility_json 含未知字段 '{pair.Key}'。允许的字段：{string.Join(" / ", VisibilityKeys)}");
            }
        }

        var scope = obj["scope"]?.GetValue<string>();
        if (string.IsNullOrWhiteSpace(scope))
        {
            throw new ArgumentException("visibility_json.scope 必填（Public / Tenant / Range）");
        }

        if (!VisibilityScopes.Contains(scope, StringComparer.Ordinal))
        {
            throw new ArgumentException(
                $"visibility_json.scope '{scope}' 不合法。可用值：{string.Join(" / ", VisibilityScopes)}");
        }

        var request = new UpdateAppVisibilityRequest
        {
            Scope = scope,
            Users = ReadStringArray(obj, "users"),
            Departments = ReadStringArray(obj, "departments"),
            Chats = ReadStringArray(obj, "chats"),
            RequireLogin = obj["require_login"]?.GetValue<bool>(),
        };

        if (obj["apply_config"] is JsonObject apply)
        {
            foreach (var pair in apply)
            {
                if (!ApplyConfigKeys.Contains(pair.Key, StringComparer.Ordinal))
                {
                    throw new ArgumentException(
                        $"visibility_json.apply_config 含未知字段 '{pair.Key}'。允许的字段：{string.Join(" / ", ApplyConfigKeys)}");
                }
            }

            request.ApplyConfig = new ApplyConfig
            {
                Enabled = apply["enabled"]?.GetValue<bool>(),
                Approvers = ReadStringArray(apply, "approvers"),
            };
        }

        // Range 语义校验：三个主体列表全空时范围无意义（飞书会按"无人可用"处理，模型多半是漏填）。
        if (string.Equals(scope, "Range", StringComparison.Ordinal)
            && (request.Users?.Length ?? 0) == 0
            && (request.Departments?.Length ?? 0) == 0
            && (request.Chats?.Length ?? 0) == 0)
        {
            throw new ArgumentException(
                "scope=Range 时 users / departments / chats 至少提供一个（否则可用范围为空，模型意图未被表达）");
        }

        return request;
    }

    /// <summary>读取字符串数组字段（缺省 null；非法元素 → 结构化 invalid_args）。</summary>
    private static string[]? ReadStringArray(JsonObject obj, string key)
    {
        if (obj[key] is not JsonArray array)
        {
            return null;
        }

        var values = new string[array.Count];
        for (var i = 0; i < array.Count; i++)
        {
            values[i] = array[i]?.GetValue<string>()
                ?? throw new ArgumentException($"visibility_json.{key}[{i}] 必须是非空字符串");
        }

        return values;
    }

    /// <summary>软依赖缺席（tenant 客户端）时的可执行提示。</summary>
    private Mud.Feishu.IFeishuTenantV1SparkApp RequireTenant(string toolName)
        => _tenantAppClient
            ?? throw new ArgumentException(
                $"{toolName} 需要 IFeishuTenantV1SparkApp（租户令牌）——宿主须启用妙搭 API（AddSparkApi）");

    /// <summary>软依赖缺席（user 客户端）时的可执行提示。</summary>
    private Mud.Feishu.IFeishuUserV1SparkApp RequireUser(string toolName)
        => _userAppClient
            ?? throw new ArgumentException(
                $"{toolName} 需要 IFeishuUserV1SparkApp（用户令牌）——宿主须启用妙搭 API 的用户侧客户端"
                + "并提供当前用户身份（FeishuToolContext.UserId）");

    /// <summary>非空数组才写入信封（保持"缺省字段不出现"的紧凑形态）。</summary>
    private static void AddIfAny(JsonObject envelope, string key, string[]? values)
    {
        if (values is { Length: > 0 })
        {
            envelope[key] = new JsonArray([.. values.Select(static v => (JsonNode?)v)]);
        }
    }

    /// <summary>
    /// 解包 <c>FeishuNullDataApiResult</c>（<c>UpdateAppVisibilityAsync</c> 无业务载荷）：
    /// 成功返回 <see langword="null"/>，失败返回结构化错误结果。
    /// </summary>
    private static FeishuToolResult? RequireNullDataSuccess(string toolName, FeishuNullDataApiResult? result)
    {
        if (result is null)
        {
            return FeishuToolResult.FromError(
                FeishuToolBinding.StructuredError(toolName, "飞书接口无响应（result 为空）"));
        }

        if (result.Code != 0)
        {
            return FeishuToolResult.FromError(
                FeishuToolBinding.StructuredError(
                    toolName,
                    result.Code,
                    $"飞书接口返回错误 code={result.Code.ToString(CultureInfo.InvariantCulture)}, "
                    + $"msg={result.Msg ?? "(无错误信息)"}"));
        }

        return null;
    }
}

/// <summary>
/// Spark 妙搭数据表面执行器（R7 / A6：4 个工具，只读 2 + 写 2）。
/// </summary>
/// <remarks>
/// <b>不策展</b>：<c>execute_sql</c>（任意 SQL 执行，无法机械校验表归属）、
/// <c>batch_update_table_records</c>（约束反直觉且与 <c>update_table_records</c> 重叠）。
/// </remarks>
internal sealed class SparkTableTools(
    IOptions<FeishuAgentOptions> options,
    Mud.Feishu.IFeishuUserV1SparkAppTable? tableClient = null)
{
    private readonly Mud.Feishu.IFeishuUserV1SparkAppTable? _tableClient = tableClient;
    private readonly int _maxResultLength = (options ?? throw new ArgumentNullException(nameof(options))).Value.MaxToolResultLength;
    private readonly int _maxAutoFetchItems = (options ?? throw new ArgumentNullException(nameof(options))).Value.MaxAutoFetchItems;
    private readonly int _maxAutoFetchPages = (options ?? throw new ArgumentNullException(nameof(options))).Value.MaxAutoFetchPages;

    /// <summary>单次 add_table_records 的记录数上限（官方限制）。</summary>
    private const int MaxRecordsPerRequest = 500;

    // ─────────────────────────── 只读面（2 个） ───────────────────────────

    /// <summary>spark.list_tables：列出数据表（分页，fetch_all 自动翻页）。</summary>
    [FeishuToolHandler(typeof(IFeishuUserSparkListTablesTool))]
    public Task<FeishuToolResult> GetTableListAsync(IReadOnlyDictionary<string, object?> arguments, CancellationToken cancellationToken)
    {
        var executor = new ToolExecutor(FeishuToolNames.SparkListTables, _maxResultLength);
        return executor.RunAsync(async () =>
        {
            var client = Require(executor.ToolName);
            var args = SparkListTablesArgs.Unpack(arguments);

            if (args.FetchAll == true)
            {
                var maxItems = ToolPagination.ResolveMaxItems(args.MaxItems, _maxAutoFetchItems);
                var aggregated = await ToolPagination.AggregateOutcomesAsync<GetTableListResult>(
                    async (token, ct) => FeishuApiResultReader.Read(await client
                        .GetTableListAsync(args.AppId, PageSizes.SparkTables, token, cancellationToken: ct)
                        .ConfigureAwait(false)),
                    page => (page.HasMore, page.PageToken),
                    page => ProjectTableList(page)["items"]!.AsArray(),
                    maxItems,
                    ToolPagination.ResolveMaxPages(_maxAutoFetchPages),
                    cancellationToken).ConfigureAwait(false);
                return executor.FromPagedResult(aggregated, maxItems);
            }

            var outcome = FeishuApiResultReader.Read(await client
                .GetTableListAsync(args.AppId, PageSizes.SparkTables, args.PageToken, cancellationToken: cancellationToken)
                .ConfigureAwait(false));

            return executor.FromApi(outcome, ProjectTableList);
        });
    }

    /// <summary>spark.query_table_records：查询数据表记录（分页，fetch_all 自动翻页）。</summary>
    [FeishuToolHandler(typeof(IFeishuUserSparkQueryTableRecordsTool))]
    public Task<FeishuToolResult> GetTableRecordListAsync(IReadOnlyDictionary<string, object?> arguments, CancellationToken cancellationToken)
    {
        var executor = new ToolExecutor(FeishuToolNames.SparkQueryTableRecords, _maxResultLength);
        return executor.RunAsync(async () =>
        {
            var client = Require(executor.ToolName);
            var args = SparkQueryTableRecordsArgs.Unpack(arguments);

            if (args.FetchAll == true)
            {
                var maxItems = ToolPagination.ResolveMaxItems(args.MaxItems, _maxAutoFetchItems);
                var aggregated = await ToolPagination.AggregateOutcomesAsync<GetTableRecordListResult>(
                    async (token, ct) => FeishuApiResultReader.Read(await client
                        .GetTableRecordListAsync(
                            args.AppId,
                            args.TableId,
                            BuildRecordQuery(args, PageSizes.SparkRecords, token),
                            ct)
                        .ConfigureAwait(false)),
                    page => (page.HasMore, page.PageToken),
                    page => ParseRecords(page.Items),
                    maxItems,
                    ToolPagination.ResolveMaxPages(_maxAutoFetchPages),
                    cancellationToken).ConfigureAwait(false);
                return executor.FromPagedResult(aggregated, maxItems);
            }

            var outcome = FeishuApiResultReader.Read(await client
                .GetTableRecordListAsync(
                    args.AppId,
                    args.TableId,
                    BuildRecordQuery(args, PageSizes.SparkRecords, args.PageToken),
                    cancellationToken)
                .ConfigureAwait(false));

            return executor.FromApi(outcome, ProjectRecordList);
        });
    }

    // ─────────────────────────── 写面（2 个） ───────────────────────────

    /// <summary>spark.add_table_records：添加记录（<c>dry_run=true</c> 时只预演）。</summary>
    [FeishuToolHandler(typeof(IFeishuUserSparkAddTableRecordsTool))]
    public Task<FeishuToolResult> PostTableRecordsAsync(IReadOnlyDictionary<string, object?> arguments, CancellationToken cancellationToken)
    {
        var executor = new ToolExecutor(FeishuToolNames.SparkAddTableRecords);
        return executor.RunAsync(async () =>
        {
            var args = SparkAddTableRecordsArgs.Unpack(arguments);

            var records = ParseRecordArray(args.RecordsJson);

            if (ToolDryRun.IsRequested(args.DryRun))
            {
                return FeishuToolResult.FromText(ToolDryRun.Describe(
                    executor.ToolName, "POST",
                    $"/open-apis/spark/v1/apps/{args.AppId}/tables/{args.TableId}/records",
                    ToolDryRun.IdempotencyNote(null),
                    ("records", records.Count),
                    ("upsert", args.Upsert == true ? 1 : 0)));
            }

            var client = Require(executor.ToolName);

            var outcome = FeishuApiResultReader.Read(await client
                .PostTableRecordsAsync(
                    args.AppId,
                    args.TableId,
                    new PostTableRecordsRequest { Records = args.RecordsJson },
                    // upsert → 请求头 Prefer: resolution=merge-duplicates（官方唯一表达方式，DTO 无 upsert 字段）。
                    prefer: args.Upsert == true ? "resolution=merge-duplicates" : null,
                    cancellationToken: cancellationToken)
                .ConfigureAwait(false));

            return executor.FromApiUntruncated(outcome, ProjectUpsertResult);
        });
    }

    /// <summary>spark.update_table_records：按 filter 条件更新记录（<c>dry_run=true</c> 时只预演）。</summary>
    [FeishuToolHandler(typeof(IFeishuUserSparkUpdateTableRecordsTool))]
    public Task<FeishuToolResult> PatchTableRecordsAsync(IReadOnlyDictionary<string, object?> arguments, CancellationToken cancellationToken)
    {
        var executor = new ToolExecutor(FeishuToolNames.SparkUpdateTableRecords);
        return executor.RunAsync(async () =>
        {
            var args = SparkUpdateTableRecordsArgs.Unpack(arguments);

            if (string.IsNullOrWhiteSpace(args.Filter))
            {
                throw new ArgumentException(
                    "filter 不能为空——无条件 PATCH 会更新全表（官方要求显式过滤条件）");
            }

            ParseRecordObject(args.RecordJson);

            if (ToolDryRun.IsRequested(args.DryRun))
            {
                return FeishuToolResult.FromText(ToolDryRun.Describe(
                    executor.ToolName, "PATCH",
                    $"/open-apis/spark/v1/apps/{args.AppId}/tables/{args.TableId}/records",
                    ToolDryRun.IdempotencyNote(null),
                    ("filter", args.Filter.Length),
                    ("record", args.RecordJson.Length)));
            }

            var client = Require(executor.ToolName);

            var outcome = FeishuApiResultReader.Read(await client
                .PatchTableRecordsAsync(
                    args.AppId,
                    args.TableId,
                    new PatchTableRecordsRequest { Record = args.RecordJson },
                    filter: args.Filter,
                    env: null,
                    cancellationToken: cancellationToken)
                .ConfigureAwait(false));

            return executor.FromApiUntruncated(outcome, ProjectUpsertResult);
        });
    }

    // ─────────────────────────── 投影 / 解析辅助 ───────────────────────────

    /// <summary>数据表列表投影：items（name/description/columns）+ 翻页契约。</summary>
    private static JsonObject ProjectTableList(GetTableListResult data)
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

        foreach (var table in data.Items ?? [])
        {
            var columns = new JsonArray();
            foreach (var column in table.Columns ?? [])
            {
                columns.AddNode(new JsonObject
                {
                    ["name"] = column.Name,
                    ["data_type"] = column.DataType,
                    ["is_primary_key"] = column.IsPrimaryKey,
                    ["is_allow_null"] = column.IsAllowNull,
                });
            }

            envelope["items"]!.AsArray().AddNode(new JsonObject
            {
                ["name"] = table.Name,
                ["description"] = table.Description,
                ["columns"] = columns,
            });
        }

        return envelope;
    }

    /// <summary>记录列表投影：items（记录数组）+ total + 翻页契约。</summary>
    private static JsonObject ProjectRecordList(GetTableRecordListResult data)
    {
        var envelope = new JsonObject
        {
            ["items"] = ParseRecords(data.Items),
            ["total"] = data.Total,
            ["has_more"] = data.HasMore,
        };
        if (!string.IsNullOrEmpty(data.PageToken))
        {
            envelope["page_token"] = data.PageToken;
        }

        return envelope;
    }

    /// <summary>
    /// SDK 把记录列表建模为 <b>JSON 数组字符串</b>（<c>GetTableRecordListResult.Items</c> 是 <c>string</c>）——
    /// 解析后回填为真正的 JSON 数组，模型无需二次解析字符串。
    /// </summary>
    /// <remarks>解析失败不抛异常（那是<b>下游载荷</b>问题，不是模型参数问题）：回填空数组 + 原始片段，模型可自行判断。</remarks>
    private static JsonArray ParseRecords(string? itemsJson)
    {
        if (string.IsNullOrWhiteSpace(itemsJson))
        {
            return [];
        }

        try
        {
            return JsonNode.Parse(itemsJson) is JsonArray array ? array : [];
        }
        catch (System.Text.Json.JsonException)
        {
            // 守卫白名单：下游载荷不是合法 JSON 数组时回填空数组（**非静默**——这是下游数据形态问题，
            // 不是模型参数问题；回填空数组比抛 shape_mismatch 更诚实：模型拿到信封的 total/has_more
            // 仍可判断"确实有记录但载荷不可解析"，而抛异常会误报成"参数错"）。
            return [];
        }
    }

    /// <summary>构造查询对象（select/filter/order 可选透传）。</summary>
    private static GetTableRecordListQuery BuildRecordQuery(
        SparkQueryTableRecordsArgs args,
        int pageSize,
        string? pageToken)
        => new()
        {
            PageSize = pageSize,
            PageToken = pageToken,
            Select = args.Select,
            Filter = args.Filter,
            Order = args.Order,
        };

    /// <summary>校验 <c>records_json</c>：必须是 JSON 数组且 ≤ 500 条。</summary>
    private static JsonArray ParseRecordArray(string recordsJson)
    {
        JsonNode? node;
        try
        {
            node = JsonNode.Parse(recordsJson);
        }
        catch (System.Text.Json.JsonException ex)
        {
            throw new ArgumentException($"records_json 不是合法 JSON：{ex.Message}");
        }

        if (node is not JsonArray array)
        {
            throw new ArgumentException("records_json 必须是 JSON 数组（每个元素为字段名→值的对象）");
        }

        if (array.Count == 0)
        {
            throw new ArgumentException("records_json 不能为空数组");
        }

        if (array.Count > MaxRecordsPerRequest)
        {
            throw new ArgumentException(
                $"records_json 最多 {MaxRecordsPerRequest.ToString(CultureInfo.InvariantCulture)} 条，实际 {array.Count.ToString(CultureInfo.InvariantCulture)} 条——请分批调用");
        }

        return array;
    }

    /// <summary>校验 <c>record_json</c>：必须是 JSON 对象。</summary>
    private static JsonObject ParseRecordObject(string recordJson)
    {
        JsonNode? node;
        try
        {
            node = JsonNode.Parse(recordJson);
        }
        catch (System.Text.Json.JsonException ex)
        {
            throw new ArgumentException($"record_json 不是合法 JSON：{ex.Message}");
        }

        return node as JsonObject
            ?? throw new ArgumentException("record_json 必须是 JSON 对象（字段名→值）");
    }

    /// <summary>写入出参投影（record_ids + 条数）。</summary>
    private static JsonObject ProjectUpsertResult(UpsertTableRecordsResult data)
    {
        var ids = data.RecordIds ?? [];
        return new JsonObject
        {
            ["record_ids"] = new JsonArray([.. ids.Select(static x => (JsonNode?)x)]),
            ["count"] = ids.Length,
        };
    }

    /// <summary>软依赖缺席时的可执行提示。</summary>
    private Mud.Feishu.IFeishuUserV1SparkAppTable Require(string toolName)
        => _tableClient
            ?? throw new ArgumentException(
                $"{toolName} 需要 IFeishuUserV1SparkAppTable（用户令牌）——宿主须启用妙搭 API 的数据表侧客户端"
                + "并提供当前用户身份（FeishuToolContext.UserId）");
}
