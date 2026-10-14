// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using System.Globalization;
using Mud.Feishu.DataModels.Application;

namespace Mud.Feishu.AI.Tools.Internal;

/// <summary>
/// Application「飞书应用管理」只读工具执行器（B1 第二个补域：7 个只读工具）。
/// </summary>
/// <remarks>
/// <para>
/// <b>软依赖</b>：五个客户端全部可空注入——宿主未启用应用管理 API 时本执行器软缺席
/// （执行期给可执行的错误说明），不影响其它域工具。
/// </para>
/// <para>
/// <b>出站净化（本域最硬的一条纪律）</b>：<c>ApplicationInfo</c> 自带
/// <c>secret</c>（应用凭据）、<c>encryption</c>（加密密钥 + 校验令牌）、<c>security</c>
/// （回调白名单 / 可信域）、<c>event</c>/<c>callback</c>（回调地址）、<c>redirect_urls</c>，
/// 以及 <c>owner</c>（负责人<b>姓名 / 邮箱 / 手机号</b>）。本执行器<b>一律不带出</b>：
/// 前四类是应用凭据与安全配置（进上下文等于把密钥写进对话史与第三方模型），
/// 后一类是员工联系方式——需要"负责人是谁"请拿 <c>owner_id</c> 走通讯录域解析
/// （那里有正确的 PII 分级与授权口径）。
/// </para>
/// <para>
/// <b>一次做对分页</b>：两个列表工具首批即接 <c>FromPagedApiAsync</c>（应用列表走
/// "页状态显式"重载——<c>GetApplicationsResult</c> 自带 <c>has_more</c> 但不继承
/// <c>ApiPageListResult</c>；版本列表走约束版）。
/// </para>
/// </remarks>
internal sealed class ApplicationTools(
    IOptions<FeishuAgentOptions> options,
    Mud.Feishu.IFeishuTenantV6ApplicationAdmin? adminClient = null,
    Mud.Feishu.IFeishuTenantV6ApplicationAppInfo? appInfoClient = null,
    Mud.Feishu.IFeishuTenantV6Application? applicationClient = null,
    Mud.Feishu.IFeishuTenantV6ApplicationScope? scopeClient = null,
    Mud.Feishu.IFeishuTenantV6ApplicationAppVersion? appVersionClient = null)
{
    /// <summary>文案语言默认值：与 SDK 默认口径一致（模型不传时用它，避免"参数缺失"式失败）。</summary>
    private const string DefaultLang = "zh_cn";

    /// <summary>应用描述的预览截断长度（摘要够用；要全文用 application.get_app）。</summary>
    private const int DescriptionPreviewLength = 200;

    /// <summary>客户端缺席时的统一说明（五个客户端各有一条 <see cref="Absent{T}"/> 文案）。</summary>
    private const string ClientName = "IFeishuTenantV6Application*（应用管理 API）";

    private readonly Mud.Feishu.IFeishuTenantV6ApplicationAdmin? _adminClient = adminClient;
    private readonly Mud.Feishu.IFeishuTenantV6ApplicationAppInfo? _appInfoClient = appInfoClient;
    private readonly Mud.Feishu.IFeishuTenantV6Application? _applicationClient = applicationClient;
    private readonly Mud.Feishu.IFeishuTenantV6ApplicationScope? _scopeClient = scopeClient;
    private readonly Mud.Feishu.IFeishuTenantV6ApplicationAppVersion? _appVersionClient = appVersionClient;

    private readonly int _maxResultLength = ToolExecutor.Require(options).MaxToolResultLength;

    /// <summary>统一分页语义（专题四）：<c>fetch_all</c> 的条目 / 页数预算。</summary>
    private readonly int _maxAutoFetchItems = ToolExecutor.Require(options).MaxAutoFetchItems;
    private readonly int _maxAutoFetchPages = ToolExecutor.Require(options).MaxAutoFetchPages;

    // ─────────────────────────── application.list_apps ───────────────────────────

    /// <summary>application.list_apps：分页列出应用（本域第一跳）。</summary>
    [ToolHandler(typeof(IFeishuTenantApplicationListAppsTool))]
    public Task<ToolResult> ListAppsAsync(IReadOnlyDictionary<string, object?> arguments, CancellationToken cancellationToken)
    {
        var executor = new ToolExecutor(FeishuToolNames.ApplicationListApps, _maxResultLength);
        if (Absent(executor.ToolName, _adminClient) is { } unavailable)
        {
            return Task.FromResult(unavailable);
        }

        return executor.RunAsync(async () =>
        {
            var args = ApplicationListAppsArgs.Unpack(arguments);

            // page_size 刻意不下发（保持 SDK 默认）——运维参数不进模型可见面（DP-B1-0）。
            // GetApplicationsResult 是"自带 has_more/page_token/total_count 但不继承
            // ApiPageListResult"的历史 DTO（HasMore 为 bool?），故走显式页状态重载。
            async Task<ToolApiOutcome<GetApplicationsResult>> FetchPageAsync(string? token, CancellationToken ct)
                => FeishuApiResultReader.Read(await _adminClient!
                    .GetApplicationsPageListAsync(
                        lang: args.Lang ?? DefaultLang,
                        page_token: token,
                        status: args.Status,
                        payment_type: args.PaymentType,
                        owner_type: args.OwnerType,
                        cancellationToken: ct)
                    .ConfigureAwait(false));

            return await executor.FromPagedApiAsync(
                FetchPageAsync,
                ProjectApps,
                static page => (page.HasMore == true, page.PageToken),
                args.FetchAll,
                args.MaxItems,
                args.PageToken,
                _maxAutoFetchItems,
                _maxAutoFetchPages,
                cancellationToken).ConfigureAwait(false);
        });
    }

    /// <summary>应用列表投影：信封 + app 摘要（**不含** secret/加密/安全配置/负责人联系方式）。</summary>
    private static JsonObject ProjectApps(GetApplicationsResult result)
    {
        var envelope = ToolResultJsons.PageEnvelope(result.HasMore, result.PageToken);
        var items = envelope["items"]!.AsArray();
        foreach (var app in result.AppList ?? [])
        {
            items.AddNode(AppSummary(app));
        }

        envelope["total"] = result.TotalCount;
        return envelope;
    }

    /// <summary>应用摘要（列表与详情共用；**这一层决定了出站字段面**）。</summary>
    private static JsonObject AppSummary(ApplicationInfo app)
        => new()
        {
            ["app_id"] = app.AppId,
            ["app_name"] = app.AppName,
            ["description"] = ToolResultText.Truncate(app.Description, DescriptionPreviewLength),
            ["status"] = app.Status,
            ["scene_type"] = app.SceneType,
            ["payment_type"] = app.PaymentType,
            ["create_source"] = app.CreateSource,
            ["primary_language"] = app.PrimaryLanguage,
            ["avatar_url"] = app.AvatarUrl,
            ["online_version_id"] = app.OnlineVersionId,
            ["unaudit_version_id"] = app.UnauditVersionId,
        };

    // ─────────────────────────── application.get_app ───────────────────────────

    /// <summary>application.get_app：按 ID 查应用详情。</summary>
    [ToolHandler(typeof(IFeishuTenantApplicationGetAppTool))]
    public Task<ToolResult> GetAppAsync(IReadOnlyDictionary<string, object?> arguments, CancellationToken cancellationToken)
    {
        var executor = new ToolExecutor(FeishuToolNames.ApplicationGetApp, _maxResultLength);
        if (Absent(executor.ToolName, _appInfoClient) is { } unavailable)
        {
            return Task.FromResult(unavailable);
        }

        return executor.RunAsync(async () =>
        {
            var args = ApplicationGetAppArgs.Unpack(arguments);

            var outcome = FeishuApiResultReader.Read(await _appInfoClient!
                .GetApplicationAsync(args.AppId, lang: args.Lang ?? DefaultLang, cancellationToken: cancellationToken)
                .ConfigureAwait(false));

            return executor.FromApi(outcome, result => result.App is null
                ? new JsonObject
                {
                    ["found"] = false,
                    ["app_id"] = args.AppId,
                    ["message"] = "未查询到该应用。请确认 app_id 形如 cli_xxx 且来自 application.list_apps 的返回；"
                        + "若列表本身为空，多为当前身份没有 application:application:readonly 权限或该租户未安装应用市场应用。",
                }
                : ProjectApp(result.App));
        });
    }

    /// <summary>应用详情投影：摘要 + 能力/默认主页/权限点（**仍不含**密钥与负责人联系方式）。</summary>
    private static JsonObject ProjectApp(ApplicationInfo app)
    {
        var payload = AppSummary(app);
        payload["found"] = true;
        payload["back_home_url"] = app.BackHomeUrl;
        payload["mobile_default_ability"] = app.MobileDefaultAbility;
        payload["pc_default_ability"] = app.PcDefaultAbility;
        payload["allow_refresh_token"] = app.AllowRefreshToken;
        payload["owner_id"] = app.Owner?.OwnerId;
        payload["scope_names"] = ScopeNames(app.Scopes);
        return payload;
    }

    // ─────────────────────────── application.get_app_visibility ───────────────────────────

    /// <summary>application.get_app_visibility：查询应用可用范围。</summary>
    [ToolHandler(typeof(IFeishuTenantApplicationGetAppVisibilityTool))]
    public Task<ToolResult> GetAppVisibilityAsync(IReadOnlyDictionary<string, object?> arguments, CancellationToken cancellationToken)
    {
        var executor = new ToolExecutor(FeishuToolNames.ApplicationGetAppVisibility, _maxResultLength);
        if (Absent(executor.ToolName, _adminClient) is { } unavailable)
        {
            return Task.FromResult(unavailable);
        }

        return executor.RunAsync(async () =>
        {
            var args = ApplicationGetAppVisibilityArgs.Unpack(arguments);

            var outcome = FeishuApiResultReader.Read(await _adminClient!
                .GetAppVisibilityAsync(args.AppId, user_page_token: args.UserPageToken, cancellationToken: cancellationToken)
                .ConfigureAwait(false));

            return executor.FromApi(outcome, ProjectVisibility);
        });
    }

    /// <summary>可用范围投影：只给标识符（部门 / 用户组 ID、用户 user_id·open_id），不给姓名。</summary>
    private static JsonObject ProjectVisibility(GetAppVisibilityResult result)
    {
        var payload = new JsonObject
        {
            ["is_visible_to_all"] = result.IsVisibleToAll,
            ["departments"] = Ids(result.Departments),
            ["invisible_departments"] = Ids(result.InvisibleDepartments),
            ["groups"] = Ids(result.Groups),
            ["invisible_groups"] = Ids(result.InvisibleGroups),
            ["users"] = VisibilityUsers(result.Users),
            ["invisible_users"] = VisibilityUsers(result.InvisibleUsers),
            ["has_more_users"] = result.HasMoreUsers,
        };

        if (!string.IsNullOrEmpty(result.UserPageToken))
        {
            // 只在非空时写出（空串会让模型当"可续游标"照抄回传，重复拉第一页）。
            payload["user_page_token"] = result.UserPageToken;
        }

        // 语义提示：这是"两条通道"（可见 / 不可见），不是一份名单——避免模型把不可见列表当成"完全无权限"。
        payload["notice"] = "可见清单与不可见清单分别生效：不可见优先。is_visible_to_all=1 时可见清单可能为空（表示全员可见）。";
        return payload;
    }

    private static JsonArray Ids(List<AppVisibilityIdItem>? items)
    {
        var array = new JsonArray();
        foreach (var item in items ?? [])
        {
            // AddNode（而非 Add）：见 ToolResultText.AddNode——泛型 Add<T>(T) 的裁剪/AOT 注解会红。
            array.AddNode(item.Id);
        }

        return array;
    }

    private static JsonArray VisibilityUsers(List<AppVisibilityUser>? users)
    {
        var array = new JsonArray();
        foreach (var user in users ?? [])
        {
            array.AddNode(new JsonObject
            {
                ["user_id"] = user.UserId,
                ["open_id"] = user.OpenId,
            });
        }

        return array;
    }

    // ─────────────────────────── application.list_collaborators ───────────────────────────

    /// <summary>application.list_collaborators：列出应用协作者。</summary>
    [ToolHandler(typeof(IFeishuTenantApplicationListCollaboratorsTool))]
    public Task<ToolResult> ListCollaboratorsAsync(IReadOnlyDictionary<string, object?> arguments, CancellationToken cancellationToken)
    {
        var executor = new ToolExecutor(FeishuToolNames.ApplicationListCollaborators, _maxResultLength);
        if (Absent(executor.ToolName, _applicationClient) is { } unavailable)
        {
            return Task.FromResult(unavailable);
        }

        return executor.RunAsync(async () =>
        {
            var args = ApplicationListCollaboratorsArgs.Unpack(arguments);

            var outcome = FeishuApiResultReader.Read(await _applicationClient!
                .GetApplicationCollaboratorsAsync(args.AppId, cancellationToken: cancellationToken)
                .ConfigureAwait(false));

            return executor.FromApi(outcome, ProjectCollaborators);
        });
    }

    /// <summary>协作者投影：type + user_id（**只给标识符**，姓名走通讯录域）。</summary>
    private static JsonObject ProjectCollaborators(GetApplicationCollaboratorsResult result)
    {
        var items = new JsonArray();
        foreach (var collaborator in result.Collaborators ?? [])
        {
            items.AddNode(new JsonObject
            {
                ["type"] = collaborator.Type,
                ["user_id"] = collaborator.UserId,
            });
        }

        var payload = new JsonObject { ["items"] = items };

        if (items.Count == 0)
        {
            // 空结果哨兵（F-5）：这里"空"是有意义的结论（无人被授予后台权限），要说清。
            payload["found"] = false;
            payload["message"] = "该应用没有显式协作者——通常表示只有创建者/所有者能进开发者后台；"
                + "所有者信息请用 application.get_app 的 owner_id（再走通讯录域解析姓名）。";
        }

        return payload;
    }

    // ─────────────────────────── application.get_scopes ───────────────────────────

    /// <summary>application.get_scopes：查询本应用已申请的权限点。</summary>
    [ToolHandler(typeof(IFeishuTenantApplicationGetScopesTool))]
    public Task<ToolResult> GetScopesAsync(IReadOnlyDictionary<string, object?> arguments, CancellationToken cancellationToken)
    {
        var executor = new ToolExecutor(FeishuToolNames.ApplicationGetScopes, _maxResultLength);
        if (Absent(executor.ToolName, _scopeClient) is { } unavailable)
        {
            return Task.FromResult(unavailable);
        }

        return executor.RunAsync(async () =>
        {
            var outcome = FeishuApiResultReader.Read(await _scopeClient!
                .GetScopesAsync(cancellationToken)
                .ConfigureAwait(false));

            return executor.FromApi(outcome, ProjectScopes);
        });
    }

    /// <summary>权限点投影：scope_name / grant_status / scope_type（授权状态用文字解释，别让模型猜数字）。</summary>
    private static JsonObject ProjectScopes(GetScopesResult result)
    {
        var items = new JsonArray();
        foreach (var scope in result.Scopes ?? [])
        {
            items.AddNode(new JsonObject
            {
                ["scope_name"] = scope.ScopeName,
                ["grant_status"] = scope.GrantStatus,
                ["grant_status_text"] = GrantStatusText(scope.GrantStatus),
                ["scope_type"] = scope.ScopeType,
            });
        }

        var payload = new JsonObject { ["items"] = items };

        if (items.Count == 0)
        {
            payload["found"] = false;
            payload["message"] = "未返回任何权限点——本应用尚未申请任何 scope，或当前身份无权读取（需 application:application:readonly）。";
        }

        return payload;
    }

    /// <summary>授权状态闭集的可读文案（对照官方文档口径；未知值原样保留数字，不猜）。</summary>
    private static string? GrantStatusText(int? status)
        => status switch
        {
            1 => "已授权",
            2 => "已申请待授权",
            3 => "已拒绝",
            _ => null,
        };

    // ─────────────────────────── application.list_app_versions ───────────────────────────

    /// <summary>application.list_app_versions：分页列出应用版本。</summary>
    [ToolHandler(typeof(IFeishuTenantApplicationListAppVersionsTool))]
    public Task<ToolResult> ListAppVersionsAsync(IReadOnlyDictionary<string, object?> arguments, CancellationToken cancellationToken)
    {
        var executor = new ToolExecutor(FeishuToolNames.ApplicationListAppVersions, _maxResultLength);
        if (Absent(executor.ToolName, _appVersionClient) is { } unavailable)
        {
            return Task.FromResult(unavailable);
        }

        return executor.RunAsync(async () =>
        {
            var args = ApplicationListAppVersionsArgs.Unpack(arguments);

            // page_size 不下发（DP-B1-0）；lang / order 是 SDK 原生业务参数（可下发）。
            async Task<ToolApiOutcome<ApiPageListResult<AppVersionInfo>>> FetchPageAsync(string? token, CancellationToken ct)
                => FeishuApiResultReader.Read(await _appVersionClient!
                    .GetAppVersionsPageListAsync(
                        args.AppId,
                        lang: args.Lang ?? DefaultLang,
                        page_token: token,
                        order: args.Order,
                        cancellationToken: ct)
                    .ConfigureAwait(false));

            return await executor.FromPagedApiAsync(
                FetchPageAsync, ProjectVersions, args.FetchAll, args.MaxItems, args.PageToken,
                _maxAutoFetchItems, _maxAutoFetchPages, cancellationToken).ConfigureAwait(false);
        });
    }

    /// <summary>版本列表投影：版本号与时间线（备注正文留给详情，避免列表过胖）。</summary>
    private static JsonObject ProjectVersions(ApiPageListResult<AppVersionInfo> result)
    {
        var envelope = ToolResultJsons.PageEnvelope(result.HasMore, result.PageToken);
        var items = envelope["items"]!.AsArray();
        foreach (var version in result.Items ?? [])
        {
            items.AddNode(new JsonObject
            {
                ["version_id"] = version.VersionId,
                ["version"] = version.Version,
                ["app_name"] = version.AppName,
                ["status"] = version.Status,
                ["create_time"] = version.CreateTime,
                ["publish_time"] = version.PublishTime,
            });
        }

        return envelope;
    }

    // ─────────────────────────── application.get_app_version ───────────────────────────

    /// <summary>application.get_app_version：按 ID 查版本详情。</summary>
    [ToolHandler(typeof(IFeishuTenantApplicationGetAppVersionTool))]
    public Task<ToolResult> GetAppVersionAsync(IReadOnlyDictionary<string, object?> arguments, CancellationToken cancellationToken)
    {
        var executor = new ToolExecutor(FeishuToolNames.ApplicationGetAppVersion, _maxResultLength);
        if (Absent(executor.ToolName, _appVersionClient) is { } unavailable)
        {
            return Task.FromResult(unavailable);
        }

        return executor.RunAsync(async () =>
        {
            var args = ApplicationGetAppVersionArgs.Unpack(arguments);

            var outcome = FeishuApiResultReader.Read(await _appVersionClient!
                .GetAppVersionAsync(args.AppId, args.VersionId, lang: args.Lang ?? DefaultLang, cancellationToken: cancellationToken)
                .ConfigureAwait(false));

            return executor.FromApi(outcome, result => result.AppVersion is null
                ? new JsonObject
                {
                    ["found"] = false,
                    ["app_id"] = args.AppId,
                    ["version_id"] = args.VersionId,
                    ["message"] = "未查询到该版本。请确认 version_id 来自 application.list_app_versions 的返回（同一应用下才有效）。",
                }
                : ProjectVersion(result.AppVersion));
        });
    }

    /// <summary>版本详情投影：版本元信息 + 备注 + 该版本可用范围（标识符级）。</summary>
    private static JsonObject ProjectVersion(AppVersionInfo version)
    {
        var payload = new JsonObject
        {
            ["found"] = true,
            ["app_id"] = version.AppId,
            ["version_id"] = version.VersionId,
            ["version"] = version.Version,
            ["app_name"] = version.AppName,
            ["description"] = ToolResultText.Truncate(version.Description, DescriptionPreviewLength),
            ["status"] = version.Status,
            ["create_time"] = version.CreateTime,
            ["publish_time"] = version.PublishTime,
            ["back_home_url"] = version.BackHomeUrl,
            ["scope_names"] = ScopeNames(version.Scopes),
        };

        if (version.Remark is { } remark)
        {
            payload["remark"] = remark.Remark;
            payload["update_remark"] = remark.UpdateRemark;
            payload["is_all_visible"] = remark.Visibility?.IsAll;
        }

        return payload;
    }

    // ─────────────────────────── 内部工具 ───────────────────────────

    /// <summary>权限点名称数组（只取 scope 名——描述与等级对"开了哪些权限"无增量）。</summary>
    private static JsonArray ScopeNames(List<AppScope>? scopes)
    {
        var array = new JsonArray();
        foreach (var scope in scopes ?? [])
        {
            array.AddNode(scope.Scope);
        }

        return array;
    }

    /// <summary>
    /// 软缺席：客户端为空时给出<b>可执行的错误结果</b>而不是抛异常
    /// （异常会逃出执行链抛给宿主，而这里要的是"模型看到一条明确的不可用说明"）。
    /// </summary>
    private static ToolResult? Absent(string toolName, object? client)
        => client is null
            ? ToolResult.FromError(FeishuToolBinding.StructuredError(
                toolName,
                $"未注册 {ClientName} 客户端——应用管理能力需宿主启用应用管理 API（软缺席，不影响其它域工具）"))
            : null;

    // ─────────────────── 写面：可见性 / 应用管理开关 ───────────────────

    /// <summary>application.update_visibility：更新应用可见范围（增量语义）。</summary>
    [ToolHandler(typeof(IFeishuTenantApplicationUpdateVisibilityTool))]
    public Task<ToolResult> UpdateVisibilityAsync(IReadOnlyDictionary<string, object?> arguments, CancellationToken cancellationToken)
    {
        var executor = new ToolExecutor(FeishuToolNames.ApplicationUpdateVisibility, _maxResultLength);
        if (Absent(executor.ToolName, _adminClient) is { } unavailable)
        {
            return Task.FromResult(unavailable);
        }

        return executor.RunAsync(async () =>
        {
            var args = ApplicationUpdateVisibilityArgs.Unpack(arguments);

            var changed = CountIds(
                args.AddVisibleUserIds, args.AddVisibleDepartmentIds, args.AddVisibleGroupIds,
                args.DelVisibleUserIds, args.DelVisibleDepartmentIds, args.DelVisibleGroupIds,
                args.AddInvisibleUserIds, args.AddInvisibleDepartmentIds, args.AddInvisibleGroupIds,
                args.DelInvisibleUserIds, args.DelInvisibleDepartmentIds, args.DelInvisibleGroupIds);

            // 参数校验早于 dry_run：四类名单全空且没给 is_visible_to_all ⇒ 空更新。
            if (changed == 0 && args.IsVisibleToAll is null)
            {
                return Invalid(executor.ToolName,
                    "至少给一个改动项（is_visible_to_all 或任一 *_{user,department,group}_ids 名单）——"
                    + "全空等于空更新，平台不会改任何东西。");
            }

            if (ToolDryRun.IsRequested(args.DryRun))
            {
                return ToolResult.FromText(ToolDryRun.Describe(
                    executor.ToolName,
                    "PATCH",
                    "/open-apis/application/v6/applications/{app_id}/visibility",
                    ToolDryRun.IdempotencyNote(null),
                    ("app_id", args.AppId?.Length ?? 0),
                    ("changed_ids", changed),
                    ("is_visible_to_all", args.IsVisibleToAll is null ? 0 : 1)));
            }

            var result = await _adminClient!
                .UpdateApplicationVisibilityAsync(
                    args.AppId,
                    new UpdateApplicationVisibilityRequest
                    {
                        IsVisibleToAll = args.IsVisibleToAll,
                        AddVisibleList = BuildIdList(args.AddVisibleUserIds, args.AddVisibleDepartmentIds, args.AddVisibleGroupIds),
                        DelVisibleList = BuildIdList(args.DelVisibleUserIds, args.DelVisibleDepartmentIds, args.DelVisibleGroupIds),
                        AddInvisibleList = BuildIdList(args.AddInvisibleUserIds, args.AddInvisibleDepartmentIds, args.AddInvisibleGroupIds),
                        DelInvisibleList = BuildIdList(args.DelInvisibleUserIds, args.DelInvisibleDepartmentIds, args.DelInvisibleGroupIds),
                    },
                    department_id_type: args.DepartmentIdType,
                    user_id_type: args.UserIdType,
                    cancellationToken: cancellationToken)
                .ConfigureAwait(false);

            RequireNullDataSuccess(executor.ToolName, result);

            return ToolResultPipeline.OkReceipt(new JsonObject
            {
                ["app_id"] = args.AppId,
                ["is_visible_to_all"] = args.IsVisibleToAll,
                ["changed_ids"] = changed,
                ["updated"] = true,
                ["notice"] = "本次按**增量**语义提交：四类名单只改传入的那些，未传的一律不动"
                    + "（与 Lingo 更新词条的覆盖式语义不同）。",
            });
        });
    }

    /// <summary>application.update_management：启用 / 停用应用管理面。</summary>
    [ToolHandler(typeof(IFeishuTenantApplicationUpdateManagementTool))]
    public Task<ToolResult> UpdateManagementAsync(IReadOnlyDictionary<string, object?> arguments, CancellationToken cancellationToken)
    {
        var executor = new ToolExecutor(FeishuToolNames.ApplicationUpdateManagement, _maxResultLength);
        if (Absent(executor.ToolName, _applicationClient) is { } unavailable)
        {
            return Task.FromResult(unavailable);
        }

        return executor.RunAsync(async () =>
        {
            var args = ApplicationUpdateManagementArgs.Unpack(arguments);

            // enable 是 bool?（生成器的解包映射表只收可空标量），必填性在这里补——
            // 参数校验早于 dry_run，避免模型拿着"预演成功"的信心漏掉这个开关。
            if (args.Enable is null)
            {
                return Invalid(executor.ToolName,
                    "enable 必填（true=启用管理面 / false=停用）——漏掉它时平台会按原值处理，"
                    + "而「停用」是影响面极大的动作，不能靠默认值。");
            }

            if (ToolDryRun.IsRequested(args.DryRun))
            {
                return ToolResult.FromText(ToolDryRun.Describe(
                    executor.ToolName,
                    "PUT",
                    "/open-apis/application/v6/applications/{app_id}/management",
                    ToolDryRun.IdempotencyNote(null),
                    ("app_id", args.AppId?.Length ?? 0),
                    ("enable", args.Enable.Value ? 1 : 0)));
            }

            var result = await _applicationClient!
                .UpdateApplicationManagementAsync(
                    args.AppId,
                    new UpdateApplicationManagementRequest { Enable = args.Enable },
                    cancellationToken)
                .ConfigureAwait(false);

            RequireNullDataSuccess(executor.ToolName, result);

            return ToolResultPipeline.OkReceipt(new JsonObject
            {
                ["app_id"] = args.AppId,
                ["enable"] = args.Enable,
                ["updated"] = true,
                ["notice"] = args.Enable.Value
                    ? "已启用应用管理面。"
                    : "已**停用**应用管理面：管理员将无法在后台管理该应用，需要时用 enable=true 恢复。",
            });
        });
    }

    // ─────────────────── 写面内部工具 ───────────────────

    /// <summary>合成 <see cref="AppIdList"/>；三类全空返回 <c>null</c>（不下发空对象）。</summary>
    private static AppIdList? BuildIdList(string[]? users, string[]? departments, string[]? groups)
        => users is { Length: > 0 } || departments is { Length: > 0 } || groups is { Length: > 0 }
            ? new AppIdList
            {
                UserIds = users?.ToList(),
                DepartmentIds = departments?.ToList(),
                GroupIds = groups?.ToList(),
            }
            : null;

    /// <summary>数一下本次实际传了多少个 ID（dry_run 与回执只报条数，不回原文）。</summary>
    private static int CountIds(params string[]?[] lists)
        => lists.Sum(static list => list?.Length ?? 0);

    /// <summary>参数非法：结构化错误（不是异常——异常会逃出执行链，模型看不到可执行信息）。</summary>
    private static ToolResult Invalid(string toolName, string message)
        => ToolResult.FromError(FeishuToolBinding.StructuredError(toolName, message));

    /// <summary>
    /// 空数据结果的成功判定（写方法返回 <see cref="FeishuNullDataApiResult"/>，
    /// 其 Data 天然为 null，不能用 <c>FeishuApiResultReader.Read</c>——那会把"无载荷"误判为失败）。
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
}
