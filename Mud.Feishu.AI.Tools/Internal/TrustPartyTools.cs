// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using System.Globalization;
using Mud.Feishu.DataModels.TrustParty;

namespace Mud.Feishu.AI.Tools.Internal;

/// <summary>
/// TrustParty「信任方 / 跨组织互通」工具执行器（B1 收官域：8 个只读 + 3 个写）。
/// </summary>
/// <remarks>
/// <para>
/// <b>软依赖</b>：四个客户端全部可空注入——宿主未启用互通 API 时本执行器软缺席
/// （执行期给可执行的错误说明），不影响其它域工具。
/// </para>
/// <para>
/// <b>PII 纪律（本域的边界线）</b>：跨组织数据天然带身份，投影的口径是
/// <b>「标识 + 姓名 + 组织角色」给，联系方式与人事属性不给</b>：
/// <list type="bullet">
/// <item>给：tenant_key / open_id / user_id / union_id / department_id / group_id、
/// 组织名与短名、成员与部门名、<c>job_title</c>（职务，与 org 域同口径）、部门负责人标识；</item>
/// <item><b>不给</b>：<c>mobile</c>（手机号）、<c>employee_no</c>（工号）、
/// <c>custom_attrs</c>（自定义属性，内容不可控）、<c>avatar</c>（头像 URL 是可追踪资源）、
/// <c>status</c>（在职/离职细分状态属人事事实）。</item>
/// </list>
/// </para>
/// <para>
/// <b>分页</b>：本域<b>没有</b>继承 <c>ApiPageListResult</c> 的结果类型——八份结果全部自带
/// <c>has_more</c>（<c>bool?</c>）与 <c>page_token</c>，故一律走<b>页状态显式</b>重载
/// （<c>null</c> 视为"未知"，停在第 N 页而不是无限翻）。
/// </para>
/// <para>
/// <b>命名口径</b>：工具名前缀 <c>trustparty</c> ⇒ 常量 <c>FeishuToolNames.Trustparty*</c>、
/// 参数类 <c>Trustparty*Args</c>；执行器类 <c>TrustPartyTools</c> ⇒ DI 入口
/// <c>AddFeishuTrustPartyToolsCore()</c>；SDK 接口名 <c>IFeishuTenantV1TrustParty*</c> / <c>...V1Directory*</c>。
/// </para>
/// </remarks>
internal sealed class TrustPartyTools(
    IOptions<FeishuAgentOptions> options,
    Mud.Feishu.IFeishuTenantV1TrustPartyCollaborationTenant? collaborationTenantClient = null,
    Mud.Feishu.IFeishuTenantV1DirectoryShareEntity? shareEntityClient = null,
    Mud.Feishu.IFeishuTenantV1DirectoryCollaborationTenant? directoryTenantClient = null,
    Mud.Feishu.IFeishuTenantV1DirectoryCollaborationRule? collaborationRuleClient = null)
{
    /// <summary>组织名 / 姓名的预览截断长度（防个别超长自定义名挤占上下文）。</summary>
    private const int NamePreviewLength = 64;

    /// <summary>客户端缺席时的统一说明。</summary>
    private const string ClientName = "IFeishuTenantV1TrustParty* / IFeishuTenantV1Directory*（互通 API）";

    private readonly Mud.Feishu.IFeishuTenantV1TrustPartyCollaborationTenant? _collaborationTenantClient = collaborationTenantClient;
    private readonly Mud.Feishu.IFeishuTenantV1DirectoryShareEntity? _shareEntityClient = shareEntityClient;
    private readonly Mud.Feishu.IFeishuTenantV1DirectoryCollaborationTenant? _directoryTenantClient = directoryTenantClient;
    private readonly Mud.Feishu.IFeishuTenantV1DirectoryCollaborationRule? _collaborationRuleClient = collaborationRuleClient;

    private readonly int _maxResultLength = ToolExecutor.Require(options).MaxToolResultLength;

    /// <summary>统一分页语义（专题四）：<c>fetch_all</c> 的条目 / 页数预算。</summary>
    private readonly int _maxAutoFetchItems = ToolExecutor.Require(options).MaxAutoFetchItems;
    private readonly int _maxAutoFetchPages = ToolExecutor.Require(options).MaxAutoFetchPages;

    // ─────────────────── trustparty.list_collaboration_tenants ───────────────────

    /// <summary>trustparty.list_collaboration_tenants：列出已互通关联组织（本域第一跳）。</summary>
    [ToolHandler(typeof(IFeishuTenantTrustpartyListCollaborationTenantsTool))]
    public Task<ToolResult> ListCollaborationTenantsAsync(IReadOnlyDictionary<string, object?> arguments, CancellationToken cancellationToken)
    {
        var executor = new ToolExecutor(FeishuToolNames.TrustpartyListCollaborationTenants, _maxResultLength);
        if (Absent(executor.ToolName, _collaborationTenantClient) is { } unavailable)
        {
            return Task.FromResult(unavailable);
        }

        return executor.RunAsync(async () =>
        {
            var args = TrustpartyListCollaborationTenantsArgs.Unpack(arguments);

            async Task<ToolApiOutcome<GetCollaborationTenantListResult>> FetchPageAsync(string? token, CancellationToken ct)
                => FeishuApiResultReader.Read(await _collaborationTenantClient!
                    .GetCollaborationTenantListAsync(page_token: token, cancellationToken: ct)
                    .ConfigureAwait(false));

            return await executor.FromPagedApiAsync(
                FetchPageAsync,
                ProjectTenantList,
                static page => (page.HasMore == true, page.PageToken),
                args.FetchAll,
                args.MaxItems,
                args.PageToken,
                _maxAutoFetchItems,
                _maxAutoFetchPages,
                cancellationToken).ConfigureAwait(false);
        });
    }

    /// <summary>关联组织列表投影：标识 + 名称 + 连接时间 + 标签（i18n 多语言副本不铺开）。</summary>
    private static JsonObject ProjectTenantList(GetCollaborationTenantListResult result)
    {
        var envelope = ToolResultJsons.PageEnvelope(result.HasMore, result.PageToken);
        var items = envelope["items"]!.AsArray();
        foreach (var tenant in result.TargetTenantList ?? [])
        {
            items.AddNode(TenantSummary(tenant));
        }

        if (items.Count == 0)
        {
            MarkEmpty(envelope, "本租户没有已建立互通关系的关联组织。请先在飞书管理后台 → 组织与人员 → 关联组织 中建立互通关系；"
                + "若已有关系仍为空，多为当前身份缺 trust_party:collaboration.tenant:readonly。");
        }

        return envelope;
    }

    private static JsonObject TenantSummary(CollaborationTenant tenant)
        => new()
        {
            // 平台给 int（秒级时间戳）；不臆造单位，键名不加后缀。
            ["target_tenant_key"] = tenant.TenantKey,
            ["tenant_name"] = ToolResultText.Truncate(tenant.TenantName, NamePreviewLength),
            ["tenant_short_name"] = ToolResultText.Truncate(tenant.TenantShortName, NamePreviewLength),
            ["tenant_tag"] = tenant.TenantTag,
            ["connect_time"] = tenant.ConnectTime,
            ["brand"] = tenant.Brand,
        };

    // ─────────────────── trustparty.get_collaboration_tenant ───────────────────

    /// <summary>trustparty.get_collaboration_tenant：查询单个关联组织详情。</summary>
    [ToolHandler(typeof(IFeishuTenantTrustpartyGetCollaborationTenantTool))]
    public Task<ToolResult> GetCollaborationTenantAsync(IReadOnlyDictionary<string, object?> arguments, CancellationToken cancellationToken)
    {
        var executor = new ToolExecutor(FeishuToolNames.TrustpartyGetCollaborationTenant, _maxResultLength);
        if (Absent(executor.ToolName, _collaborationTenantClient) is { } unavailable)
        {
            return Task.FromResult(unavailable);
        }

        return executor.RunAsync(async () =>
        {
            var args = TrustpartyGetCollaborationTenantArgs.Unpack(arguments);

            var outcome = FeishuApiResultReader.Read(await _collaborationTenantClient!
                .GetCollaborationTenantAsync(args.TargetTenantKey, cancellationToken)
                .ConfigureAwait(false));

            return executor.FromApi(outcome, result => result.TargetTenant is null
                ? NotFound(executor.ToolName, "target_tenant_key", args.TargetTenantKey,
                    "请确认该 key 来自 trustparty.list_collaboration_tenants 的返回（互通关系可能已被对方解除）。")
                : WithFound(TenantSummary(result.TargetTenant)));
        });
    }

    // ─────────────────── trustparty.list_visible_organization ───────────────────

    /// <summary>trustparty.list_visible_organization：查询对端可见组织实体。</summary>
    [ToolHandler(typeof(IFeishuTenantTrustpartyListVisibleOrganizationTool))]
    public Task<ToolResult> ListVisibleOrganizationAsync(IReadOnlyDictionary<string, object?> arguments, CancellationToken cancellationToken)
    {
        var executor = new ToolExecutor(FeishuToolNames.TrustpartyListVisibleOrganization, _maxResultLength);
        if (Absent(executor.ToolName, _collaborationTenantClient) is { } unavailable)
        {
            return Task.FromResult(unavailable);
        }

        return executor.RunAsync(async () =>
        {
            var args = TrustpartyListVisibleOrganizationArgs.Unpack(arguments);

            async Task<ToolApiOutcome<GetVisibleOrganizationResult>> FetchPageAsync(string? token, CancellationToken ct)
                => FeishuApiResultReader.Read(await _collaborationTenantClient!
                    .GetVisibleOrganizationAsync(
                        args.TargetTenantKey,
                        new VisibleOrganizationQuery
                        {
                            TargetDepartmentId = args.TargetDepartmentId,
                            DepartmentIdType = args.DepartmentIdType,
                            TargetGroupId = args.TargetGroupId,
                            GroupIdType = args.GroupIdType,
                            PageToken = token,

                            // page_size 刻意不下发（保持 SDK 默认）——运维参数不进模型可见面（DP-B1-0）。
                        },
                        ct)
                    .ConfigureAwait(false));

            return await executor.FromPagedApiAsync(
                FetchPageAsync,
                ProjectVisibleOrganization,
                static page => (page.HasMore == true, page.PageToken),
                args.FetchAll,
                args.MaxItems,
                args.PageToken,
                _maxAutoFetchItems,
                _maxAutoFetchPages,
                cancellationToken).ConfigureAwait(false);
        });
    }

    /// <summary>可见组织投影：统一形状（实体类型 + 部门/用户/群三组键，不适用的为 null）。</summary>
    private static JsonObject ProjectVisibleOrganization(GetVisibleOrganizationResult result)
    {
        var envelope = ToolResultJsons.PageEnvelope(result.HasMore, result.PageToken);
        var items = envelope["items"]!.AsArray();
        foreach (var entity in result.CollaborationEntityList ?? [])
        {
            items.AddNode(new JsonObject
            {
                ["entity_type"] = entity.CollaborationEntityType,
                ["department_id"] = entity.DepartmentId,
                ["open_department_id"] = entity.OpenDepartmentId,
                ["department_name"] = ToolResultText.Truncate(entity.DepartmentName, NamePreviewLength),
                ["user_id"] = entity.UserId,
                ["open_user_id"] = entity.OpenUserId,
                ["union_user_id"] = entity.UnionUserId,
                ["user_name"] = ToolResultText.Truncate(entity.UserName, NamePreviewLength),
                ["group_id"] = entity.GroupId,
                ["open_group_id"] = entity.OpenGroupId,
                ["group_name"] = ToolResultText.Truncate(entity.GroupName, NamePreviewLength),
            });
        }

        if (items.Count == 0)
        {
            MarkEmpty(envelope, "对端当前没有对本租户开放任何组织实体。可能是互通范围本就为空，"
                + "也可能被 target_department_id / target_group_id 收窄掉了——去掉收窄参数重试一次再判断。");
        }

        return envelope;
    }

    // ─────────────────── trustparty.get_collaboration_department ───────────────────

    /// <summary>trustparty.get_collaboration_department：查询外部部门详情。</summary>
    [ToolHandler(typeof(IFeishuTenantTrustpartyGetCollaborationDepartmentTool))]
    public Task<ToolResult> GetCollaborationDepartmentAsync(IReadOnlyDictionary<string, object?> arguments, CancellationToken cancellationToken)
    {
        var executor = new ToolExecutor(FeishuToolNames.TrustpartyGetCollaborationDepartment, _maxResultLength);
        if (Absent(executor.ToolName, _collaborationTenantClient) is { } unavailable)
        {
            return Task.FromResult(unavailable);
        }

        return executor.RunAsync(async () =>
        {
            var args = TrustpartyGetCollaborationDepartmentArgs.Unpack(arguments);

            var outcome = FeishuApiResultReader.Read(await _collaborationTenantClient!
                .GetCollaborationDepartmentAsync(
                    args.TargetTenantKey,
                    args.TargetDepartmentId,
                    target_department_id_type: args.TargetDepartmentIdType,
                    cancellationToken: cancellationToken)
                .ConfigureAwait(false));

            return executor.FromApi(outcome, result => result.TargetDepartment is null
                ? NotFound(executor.ToolName, "target_department_id", args.TargetDepartmentId,
                    "请确认部门 ID 与 target_department_id_type 口径一致（open_department_id 与 department_id 不可混用），"
                    + "并确认该部门在对端开放范围内。")
                : WithFound(ProjectDepartment(result.TargetDepartment)));
        });
    }

    /// <summary>外部部门投影：名称 + 排序 + 负责人标识 + 上级部门标识（负责人只给标识不给姓名）。</summary>
    private static JsonObject ProjectDepartment(CollaborationDepartment department)
    {
        var leaders = new JsonArray();
        foreach (var leader in department.Leaders ?? [])
        {
            leaders.AddNode(new JsonObject
            {
                ["leader_type"] = leader.LeaderType,
                ["user_id"] = leader.Id?.UserId,
                ["open_id"] = leader.Id?.OpenId,
                ["union_id"] = leader.Id?.UnionId,
            });
        }

        return new JsonObject
        {
            ["department_id"] = department.DepartmentId,
            ["open_department_id"] = department.OpenDepartmentId,
            ["name"] = ToolResultText.Truncate(department.Name, NamePreviewLength),
            ["order"] = department.Order,
            ["parent_department_id"] = department.ParentDepartmentId?.DepartmentId,
            ["parent_open_department_id"] = department.ParentDepartmentId?.OpenDepartmentId,
            ["leaders"] = leaders,
            ["notice"] = "负责人只给标识（leader_type + user_id/open_id）：要姓名请用 trustparty.get_collaboration_user。",
        };
    }

    // ─────────────────── trustparty.get_collaboration_user ───────────────────

    /// <summary>trustparty.get_collaboration_user：查询外部成员详情（强 PII 净化）。</summary>
    [ToolHandler(typeof(IFeishuTenantTrustpartyGetCollaborationUserTool))]
    public Task<ToolResult> GetCollaborationUserAsync(IReadOnlyDictionary<string, object?> arguments, CancellationToken cancellationToken)
    {
        var executor = new ToolExecutor(FeishuToolNames.TrustpartyGetCollaborationUser, _maxResultLength);
        if (Absent(executor.ToolName, _collaborationTenantClient) is { } unavailable)
        {
            return Task.FromResult(unavailable);
        }

        return executor.RunAsync(async () =>
        {
            var args = TrustpartyGetCollaborationUserArgs.Unpack(arguments);

            var outcome = FeishuApiResultReader.Read(await _collaborationTenantClient!
                .GetCollaborationUserAsync(
                    args.TargetTenantKey,
                    args.TargetUserId,
                    target_user_id_type: args.TargetUserIdType,
                    cancellationToken: cancellationToken)
                .ConfigureAwait(false));

            return executor.FromApi(outcome, result => result.TargetUser is null
                ? NotFound(executor.ToolName, "target_user_id", args.TargetUserId,
                    "请确认成员 ID 与 target_user_id_type 口径一致，并确认该成员在对端开放范围内"
                    + "（成员已离职或被移出互通范围时也会查不到）。")
                : WithFound(ProjectUser(result.TargetUser)));
        });
    }

    /// <summary>
    /// 外部成员投影：<b>标识 + 姓名 + 职务 + 部门/上级标识</b>。
    /// </summary>
    /// <remarks>
    /// 出站净化（本域最硬的一条）：<c>mobile</c>（手机号）、<c>employee_no</c>（工号）、
    /// <c>custom_attrs</c>（自定义属性，内容不可控）、<c>avatar</c>（可追踪资源）、
    /// <c>status</c>（在职/离职细分状态）<b>一律不写出</b>。
    /// </remarks>
    private static JsonObject ProjectUser(CollaborationUser user)
    {
        var departments = new JsonArray();
        foreach (var department in user.ParentDepartmentIds ?? [])
        {
            departments.AddNode(new JsonObject
            {
                ["department_id"] = department.DepartmentId,
                ["open_department_id"] = department.OpenDepartmentId,
            });
        }

        return new JsonObject
        {
            ["open_id"] = user.OpenId,
            ["user_id"] = user.UserId,
            ["union_id"] = user.UnionId,
            ["name"] = ToolResultText.Truncate(user.Name, NamePreviewLength),
            ["job_title"] = user.JobTitle,
            ["parent_department_ids"] = departments,
            ["leader_id"] = user.LeaderId?.UserId,
            ["leader_open_id"] = user.LeaderId?.OpenId,
            ["notice"] = "本工具不返回手机号 / 工号 / 自定义属性 / 在职状态（出站净化）——需要联系方式请走对端自有渠道。",
        };
    }

    // ─────────────────── trustparty.list_share_entities ───────────────────

    /// <summary>trustparty.list_share_entities：查询被共享的目录实体（三份清单）。</summary>
    [ToolHandler(typeof(IFeishuTenantTrustpartyListShareEntitiesTool))]
    public Task<ToolResult> ListShareEntitiesAsync(IReadOnlyDictionary<string, object?> arguments, CancellationToken cancellationToken)
    {
        var executor = new ToolExecutor(FeishuToolNames.TrustpartyListShareEntities, _maxResultLength);
        if (Absent(executor.ToolName, _shareEntityClient) is { } unavailable)
        {
            return Task.FromResult(unavailable);
        }

        return executor.RunAsync(async () =>
        {
            var args = TrustpartyListShareEntitiesArgs.Unpack(arguments);

            async Task<ToolApiOutcome<GetShareEntityListResult>> FetchPageAsync(string? token, CancellationToken ct)
                => FeishuApiResultReader.Read(await _shareEntityClient!
                    .GetShareEntityListAsync(
                        new ShareEntityListQuery
                        {
                            TargetTenantKey = args.TargetTenantKey,
                            TargetDepartmentId = args.TargetDepartmentId,
                            TargetGroupId = args.TargetGroupId,
                            IsSelectSubject = args.IsSelectSubject,
                            PageToken = token,
                        },
                        ct)
                    .ConfigureAwait(false));

            return await executor.FromPagedApiAsync(
                FetchPageAsync,
                ProjectShareEntities,
                static page => (page.HasMore == true, page.PageToken),
                args.FetchAll,
                args.MaxItems,
                args.PageToken,
                _maxAutoFetchItems,
                _maxAutoFetchPages,
                cancellationToken).ConfigureAwait(false);
        });
    }

    /// <summary>
    /// 共享实体投影：三份分类清单 + 一份合并 <c>items</c>（带 <c>kind</c>）。
    /// </summary>
    /// <remarks>
    /// ⚠️ 为什么同时给"分类清单"和"合并 items"：统一分页出口按 <c>items</c> 聚合，
    /// 若只给三份分类清单，<c>fetch_all</c> 翻页时无法拼接（每页的 <c>items</c> 为空 ⇒ 聚合结果丢数据）。
    /// 合并项是<b>聚合用的规范形态</b>，分类清单是<b>给人读的原生形态</b>——两者同源同一份投影，不会漂移。
    /// </remarks>
    private static JsonObject ProjectShareEntities(GetShareEntityListResult result)
    {
        var envelope = ToolResultJsons.PageEnvelope(result.HasMore, result.PageToken);
        var items = envelope["items"]!.AsArray();

        var departments = new JsonArray();
        foreach (var department in result.ShareDepartments ?? [])
        {
            departments.AddNode(new JsonObject
            {
                ["open_department_id"] = department.OpenDepartmentId,
                ["name"] = ToolResultText.Truncate(department.Name?.Value, NamePreviewLength),
            });

            items.AddNode(new JsonObject
            {
                ["kind"] = "department",
                ["id"] = department.OpenDepartmentId,
                ["name"] = ToolResultText.Truncate(department.Name?.Value, NamePreviewLength),
            });
        }

        var groups = new JsonArray();
        foreach (var group in result.ShareGroups ?? [])
        {
            groups.AddNode(new JsonObject
            {
                ["open_group_id"] = group.OpenGroupId,
                ["name"] = ToolResultText.Truncate(group.Name?.Value, NamePreviewLength),
            });

            items.AddNode(new JsonObject
            {
                ["kind"] = "group",
                ["id"] = group.OpenGroupId,
                ["name"] = ToolResultText.Truncate(group.Name?.Value, NamePreviewLength),
            });
        }

        var users = new JsonArray();
        foreach (var user in result.ShareUsers ?? [])
        {
            users.AddNode(new JsonObject
            {
                ["open_user_id"] = user.OpenUserId,
                ["name"] = ToolResultText.Truncate(user.Name?.Value, NamePreviewLength),
            });

            items.AddNode(new JsonObject
            {
                ["kind"] = "user",
                ["id"] = user.OpenUserId,
                ["name"] = ToolResultText.Truncate(user.Name?.Value, NamePreviewLength),
            });
        }

        envelope["departments"] = departments;
        envelope["groups"] = groups;
        envelope["users"] = users;

        if (items.Count == 0)
        {
            MarkEmpty(envelope, "没有查询到被共享的目录实体。可能是本租户尚未配置共享范围，"
                + "或 is_select_subject / target_tenant_key 的取值口径与预期相反——先去掉收窄条件重试。");
        }

        return envelope;
    }

    // ─────────────────── trustparty.list_all_collaboration_tenants ───────────────────

    /// <summary>trustparty.list_all_collaboration_tenants：目录侧关联组织列表。</summary>
    [ToolHandler(typeof(IFeishuTenantTrustpartyListAllCollaborationTenantsTool))]
    public Task<ToolResult> ListAllCollaborationTenantsAsync(IReadOnlyDictionary<string, object?> arguments, CancellationToken cancellationToken)
    {
        var executor = new ToolExecutor(FeishuToolNames.TrustpartyListAllCollaborationTenants, _maxResultLength);
        if (Absent(executor.ToolName, _directoryTenantClient) is { } unavailable)
        {
            return Task.FromResult(unavailable);
        }

        return executor.RunAsync(async () =>
        {
            var args = TrustpartyListAllCollaborationTenantsArgs.Unpack(arguments);

            async Task<ToolApiOutcome<GetAllCollaborationTenantListResult>> FetchPageAsync(string? token, CancellationToken ct)
                => FeishuApiResultReader.Read(await _directoryTenantClient!
                    .GetAllCollaborationTenantListAsync(page_token: token, cancellationToken: ct)
                    .ConfigureAwait(false));

            return await executor.FromPagedApiAsync(
                FetchPageAsync,
                ProjectAllTenants,
                static page => (page.HasMore == true, page.PageToken),
                args.FetchAll,
                args.MaxItems,
                args.PageToken,
                _maxAutoFetchItems,
                _maxAutoFetchPages,
                cancellationToken).ConfigureAwait(false);
        });
    }

    /// <summary>目录侧关联组织投影：标识 + 名称 + 短名 + 连接时间 + 品牌。</summary>
    private static JsonObject ProjectAllTenants(GetAllCollaborationTenantListResult result)
    {
        var envelope = ToolResultJsons.PageEnvelope(result.HasMore, result.PageToken);
        var items = envelope["items"]!.AsArray();
        foreach (var tenant in result.Items ?? [])
        {
            items.AddNode(new JsonObject
            {
                ["target_tenant_key"] = tenant.TenantKey,
                ["name"] = ToolResultText.Truncate(tenant.Name?.Value, NamePreviewLength),
                ["short_name"] = ToolResultText.Truncate(tenant.ShortName?.Value, NamePreviewLength),
                ["connect_time"] = tenant.ConnectTime,
                ["brand"] = tenant.Brand,
            });
        }

        if (items.Count == 0)
        {
            MarkEmpty(envelope, "目录侧没有任何关联组织。注意本工具与 trustparty.list_collaboration_tenants 是"
                + "**不同接口面**（directory vs trust_party）：若那边有数据、这边为空，说明目录未启用对应能力"
                + "（常见于应用商店应用或权限不足）。");
        }

        return envelope;
    }

    // ─────────────────── trustparty.list_collaboration_rules ───────────────────

    /// <summary>trustparty.list_collaboration_rules：互通规则列表。</summary>
    [ToolHandler(typeof(IFeishuTenantTrustpartyListCollaborationRulesTool))]
    public Task<ToolResult> ListCollaborationRulesAsync(IReadOnlyDictionary<string, object?> arguments, CancellationToken cancellationToken)
    {
        var executor = new ToolExecutor(FeishuToolNames.TrustpartyListCollaborationRules, _maxResultLength);
        if (Absent(executor.ToolName, _collaborationRuleClient) is { } unavailable)
        {
            return Task.FromResult(unavailable);
        }

        return executor.RunAsync(async () =>
        {
            var args = TrustpartyListCollaborationRulesArgs.Unpack(arguments);

            async Task<ToolApiOutcome<GetCollaborationRuleListResult>> FetchPageAsync(string? token, CancellationToken ct)
                => FeishuApiResultReader.Read(await _collaborationRuleClient!
                    .GetCollaborationRuleListAsync(
                        args.TargetTenantKey,
                        page_token: token,
                        cancellationToken: ct)
                    .ConfigureAwait(false));

            return await executor.FromPagedApiAsync(
                FetchPageAsync,
                ProjectRules,
                static page => (page.HasMore == true, page.PageToken),
                args.FetchAll,
                args.MaxItems,
                args.PageToken,
                _maxAutoFetchItems,
                _maxAutoFetchPages,
                cancellationToken).ConfigureAwait(false);
        });
    }

    /// <summary>规则投影：主体 / 客体实体标识集合 + 两侧有效性（排查"配了不生效"的关键信号）。</summary>
    private static JsonObject ProjectRules(GetCollaborationRuleListResult result)
    {
        var envelope = ToolResultJsons.PageEnvelope(result.HasMore, result.PageToken);
        var items = envelope["items"]!.AsArray();
        var invalidCount = 0;

        foreach (var rule in result.Items ?? [])
        {
            if (rule.SubjectIsValid == false || rule.ObjectIsValid == false)
            {
                invalidCount++;
            }

            items.AddNode(new JsonObject
            {
                ["rule_id"] = rule.RuleId,
                ["subject_is_valid"] = rule.SubjectIsValid,
                ["object_is_valid"] = rule.ObjectIsValid,
                ["subjects"] = RuleEntities(rule.Subjects),
                ["objects"] = RuleEntities(rule.Objects),
            });
        }

        if (items.Count == 0)
        {
            MarkEmpty(envelope, "该关联组织没有任何互通规则。若预期有规则，请确认 target_tenant_key 取自"
                + " trustparty.list_all_collaboration_tenants（directory 面），并确认当前身份有"
                + " trust_party:collaboration_rule:read。");
        }
        else if (invalidCount > 0)
        {
            // 把"为什么不生效"直接算出来给模型，而不是让它逐条比对布尔值。
            envelope["invalid_rule_count"] = invalidCount;
            envelope["notice"] = $"有 {invalidCount} 条规则的主体或客体已失效"
                + "（对应成员离职 / 部门被删 / 用户组解散）——这类规则存在但不会生效，需重建或更新。";
        }

        return envelope;
    }

    private static JsonObject? RuleEntities(CollaborationRuleEntities? entities)
        => entities is null
            ? null
            : new JsonObject
            {
                ["open_user_ids"] = StringArray(entities.OpenUserIds),
                ["open_department_ids"] = StringArray(entities.OpenDepartmentIds),
                ["open_group_ids"] = StringArray(entities.OpenGroupIds),
            };

    // ─────────────────── 内部工具 ───────────────────

    private static JsonArray StringArray(IEnumerable<string>? values)
    {
        var array = new JsonArray();
        foreach (var value in values ?? [])
        {
            // AddNode（而非 Add）：见 ToolResultText.AddNode——泛型 Add<T>(T) 的裁剪/AOT 注解会红。
            array.AddNode(value);
        }

        return array;
    }

    /// <summary>空结果哨兵（F-5）：写入 <c>found=false</c> + 可执行的下一步提示。</summary>
    private static void MarkEmpty(JsonObject envelope, string message)
    {
        envelope["found"] = false;
        envelope["message"] = message;
    }

    /// <summary>单对象"未找到"哨兵（把"ID 不对"与"没权限/关系已解除"分开讲）。</summary>
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
    /// 软缺席：客户端为空时给出<b>可执行的错误结果</b>而不是抛异常
    /// （异常会逃出执行链抛给宿主，而这里要的是"模型看到一条明确的不可用说明"）。
    /// </summary>
    private static ToolResult? Absent(string toolName, object? client)
        => client is null
            ? ToolResult.FromError(FeishuToolBinding.StructuredError(
                toolName,
                $"未注册 {ClientName} 客户端——跨组织互通能力需宿主启用互通 API（软缺席，不影响其它域工具）"))
            : null;

    // ─────────────────── 写面：互通规则 CRUD ───────────────────

    /// <summary>trustparty.create_collaboration_rule：新建互通规则。</summary>
    [ToolHandler(typeof(IFeishuTenantTrustpartyCreateCollaborationRuleTool))]
    public Task<ToolResult> CreateCollaborationRuleAsync(IReadOnlyDictionary<string, object?> arguments, CancellationToken cancellationToken)
    {
        var executor = new ToolExecutor(FeishuToolNames.TrustpartyCreateCollaborationRule, _maxResultLength);
        if (Absent(executor.ToolName, _collaborationRuleClient) is { } unavailable)
        {
            return Task.FromResult(unavailable);
        }

        return executor.RunAsync(async () =>
        {
            var args = TrustpartyCreateCollaborationRuleArgs.Unpack(arguments);

            // 参数校验只做一次，且**先于 dry_run**：非法形状连预演都不该放行
            // （否则模型会拿着"预演成功"的信心去改参数，反而多绕一轮）。
            var subjects = BuildEntities(args.SubjectOpenUserIds, args.SubjectOpenDepartmentIds, args.SubjectOpenGroupIds);
            var objects = BuildEntities(args.ObjectOpenUserIds, args.ObjectOpenDepartmentIds, args.ObjectOpenGroupIds);
            if (subjects is null || objects is null)
            {
                return Invalid(executor.ToolName,
                    "主体（subject_*）与客体（object_*）两侧都必须至少填一类 open_id 列表"
                    + "——平台会因空侧直接报错，且错误信息对模型不可执行。");
            }

            var total = CountEntities(args.SubjectOpenUserIds, args.SubjectOpenDepartmentIds, args.SubjectOpenGroupIds)
                + CountEntities(args.ObjectOpenUserIds, args.ObjectOpenDepartmentIds, args.ObjectOpenGroupIds);
            if (total >= EntityBudget)
            {
                return Invalid(executor.ToolName,
                    $"主客体实体数之和须 < {EntityBudget}（当前 {total}）——平台硬约束，超了必被拒。");
            }

            if (ToolDryRun.IsRequested(args.DryRun))
            {
                return ToolResult.FromText(ToolDryRun.Describe(
                    executor.ToolName,
                    "POST",
                    "/open-apis/directory/v1/collaboration_rules",
                    ToolDryRun.IdempotencyNote(null),
                    ("target_tenant_key", args.TargetTenantKey?.Length ?? 0),
                    ("subject_entities", CountEntities(args.SubjectOpenUserIds, args.SubjectOpenDepartmentIds, args.SubjectOpenGroupIds)),
                    ("object_entities", CountEntities(args.ObjectOpenUserIds, args.ObjectOpenDepartmentIds, args.ObjectOpenGroupIds))));
            }

            var outcome = FeishuApiResultReader.Read(await _collaborationRuleClient!
                .CreateCollaborationRuleAsync(
                    new CreateCollaborationRuleRequest { Subjects = subjects, Objects = objects },
                    args.TargetTenantKey,
                    cancellationToken)
                .ConfigureAwait(false));

            return executor.FromApi(outcome, data => new JsonObject
            {
                ["rule_id"] = data.AddRuleId,
                ["target_tenant_key"] = args.TargetTenantKey,
                ["subject_entities"] = CountEntities(args.SubjectOpenUserIds, args.SubjectOpenDepartmentIds, args.SubjectOpenGroupIds),
                ["object_entities"] = CountEntities(args.ObjectOpenUserIds, args.ObjectOpenDepartmentIds, args.ObjectOpenGroupIds),
                ["created"] = true,
            });
        });
    }

    /// <summary>trustparty.update_collaboration_rule：覆盖式更新互通规则。</summary>
    [ToolHandler(typeof(IFeishuTenantTrustpartyUpdateCollaborationRuleTool))]
    public Task<ToolResult> UpdateCollaborationRuleAsync(IReadOnlyDictionary<string, object?> arguments, CancellationToken cancellationToken)
    {
        var executor = new ToolExecutor(FeishuToolNames.TrustpartyUpdateCollaborationRule, _maxResultLength);
        if (Absent(executor.ToolName, _collaborationRuleClient) is { } unavailable)
        {
            return Task.FromResult(unavailable);
        }

        return executor.RunAsync(async () =>
        {
            var args = TrustpartyUpdateCollaborationRuleArgs.Unpack(arguments);

            var subjects = BuildEntities(args.SubjectOpenUserIds, args.SubjectOpenDepartmentIds, args.SubjectOpenGroupIds);
            var objects = BuildEntities(args.ObjectOpenUserIds, args.ObjectOpenDepartmentIds, args.ObjectOpenGroupIds);
            var total = CountEntities(args.SubjectOpenUserIds, args.SubjectOpenDepartmentIds, args.SubjectOpenGroupIds)
                + CountEntities(args.ObjectOpenUserIds, args.ObjectOpenDepartmentIds, args.ObjectOpenGroupIds);

            if (subjects is null && objects is null)
            {
                return Invalid(executor.ToolName,
                    "至少填一侧的 open_id 列表（subject_* 或 object_*）——两侧都空等于把规则清成空规则，"
                    + "平台会因空侧报错。若确实要作废规则，请用 delete_collaboration_rule。");
            }

            if (total >= EntityBudget)
            {
                return Invalid(executor.ToolName,
                    $"主客体实体数之和须 < {EntityBudget}（当前 {total}）——平台硬约束，超了必被拒。");
            }

            if (ToolDryRun.IsRequested(args.DryRun))
            {
                return ToolResult.FromText(ToolDryRun.Describe(
                    executor.ToolName,
                    "PUT",
                    "/open-apis/directory/v1/collaboration_rules/{collaboration_rule_id}",
                    ToolDryRun.IdempotencyNote(null),
                    ("target_tenant_key", args.TargetTenantKey?.Length ?? 0),
                    ("collaboration_rule_id", args.CollaborationRuleId?.Length ?? 0),
                    ("subject_entities", CountEntities(args.SubjectOpenUserIds, args.SubjectOpenDepartmentIds, args.SubjectOpenGroupIds)),
                    ("object_entities", CountEntities(args.ObjectOpenUserIds, args.ObjectOpenDepartmentIds, args.ObjectOpenGroupIds))));
            }

            var result = await _collaborationRuleClient!
                .UpdateCollaborationRuleAsync(
                    args.CollaborationRuleId,
                    new UpdateCollaborationRuleRequest { Subjects = subjects, Objects = objects },
                    args.TargetTenantKey,
                    cancellationToken)
                .ConfigureAwait(false);

            RequireNullDataSuccess(executor.ToolName, result);

            return ToolResultPipeline.OkReceipt(new JsonObject
            {
                ["rule_id"] = args.CollaborationRuleId,
                ["target_tenant_key"] = args.TargetTenantKey,
                ["subject_entities"] = CountEntities(args.SubjectOpenUserIds, args.SubjectOpenDepartmentIds, args.SubjectOpenGroupIds),
                ["object_entities"] = CountEntities(args.ObjectOpenUserIds, args.ObjectOpenDepartmentIds, args.ObjectOpenGroupIds),
                ["updated"] = true,
                ["notice"] = "已按覆盖式语义提交：未传的一侧会被清空。若只想追加成员，请先 list_collaboration_rules"
                    + "读出原主体/客体，合并后再整份提交。",
            });
        });
    }

    /// <summary>trustparty.delete_collaboration_rule：删除互通规则（high-risk-write）。</summary>
    [ToolHandler(typeof(IFeishuTenantTrustpartyDeleteCollaborationRuleTool))]
    public Task<ToolResult> DeleteCollaborationRuleAsync(IReadOnlyDictionary<string, object?> arguments, CancellationToken cancellationToken)
    {
        var executor = new ToolExecutor(FeishuToolNames.TrustpartyDeleteCollaborationRule, _maxResultLength);
        if (Absent(executor.ToolName, _collaborationRuleClient) is { } unavailable)
        {
            return Task.FromResult(unavailable);
        }

        return executor.RunAsync(async () =>
        {
            var args = TrustpartyDeleteCollaborationRuleArgs.Unpack(arguments);

            if (ToolDryRun.IsRequested(args.DryRun))
            {
                return ToolResult.FromText(ToolDryRun.Describe(
                    executor.ToolName,
                    "DELETE",
                    "/open-apis/directory/v1/collaboration_rules/{collaboration_rule_id}",
                    ToolDryRun.IdempotencyNote(null),
                    ("target_tenant_key", args.TargetTenantKey?.Length ?? 0),
                    ("collaboration_rule_id", args.CollaborationRuleId?.Length ?? 0)));
            }

            var result = await _collaborationRuleClient!
                .DeleteCollaborationRuleAsync(
                    args.CollaborationRuleId,
                    args.TargetTenantKey,
                    cancellationToken)
                .ConfigureAwait(false);

            RequireNullDataSuccess(executor.ToolName, result);

            return ToolResultPipeline.OkReceipt(new JsonObject
            {
                ["rule_id"] = args.CollaborationRuleId,
                ["target_tenant_key"] = args.TargetTenantKey,
                ["deleted"] = true,
                ["notice"] = "删除不可撤销：对端已失去该规则下的互通能力。如需恢复，只能按原主体/客体重新创建"
                    + "（新规则会有新的 rule_id）。",
            });
        });
    }

    // ─────────────────── 写面内部工具 ───────────────────

    /// <summary>平台约束：主客体实体数之和须 &lt; 100（SDK 注释口径）。</summary>
    private const int EntityBudget = 100;

    /// <summary>三类 open_id 清单合成一个规则侧；全空返回 <c>null</c>（表示"该侧未提供"）。</summary>
    private static CollaborationRuleEntities? BuildEntities(string[]? users, string[]? departments, string[]? groups)
        => users is { Length: > 0 } || departments is { Length: > 0 } || groups is { Length: > 0 }
            ? new CollaborationRuleEntities
            {
                OpenUserIds = users,
                OpenDepartmentIds = departments,
                OpenGroupIds = groups,
            }
            : null;

    /// <summary>数一侧的实体条数（dry_run 与回执只报条数，不回原文 ID）。</summary>
    private static int CountEntities(string[]? users, string[]? departments, string[]? groups)
        => (users?.Length ?? 0) + (departments?.Length ?? 0) + (groups?.Length ?? 0);

    /// <summary>参数非法：结构化错误（不是异常——异常会逃出执行链，模型看不到可执行信息）。</summary>
    private static ToolResult Invalid(string toolName, string message)
        => ToolResult.FromError(FeishuToolBinding.StructuredError(toolName, message));

    /// <summary>
    /// 空数据结果的成功判定（update / delete 返回 <see cref="FeishuNullDataApiResult"/>，
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
