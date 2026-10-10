// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.AI.Tools.Tools;
using Mud.Feishu.DataModels.JobFamilies;
using Mud.Feishu.DataModels.JobLevel;
using Mud.Feishu.DataModels.JobTitles;
using Mud.Feishu.DataModels.WorkCites;

namespace Mud.Feishu.AI.Tools.Internal;

/// <summary>
/// org「组织元数据」只读工具执行器（F-1 第三切片：职务 / 职级 / 职务族 / 工作城市共 8 个工具）。
/// </summary>
/// <remarks>
/// <para>
/// <b>存在理由（跨域断链）</b>：通讯录（<c>contact.*</c>）返回的用户对象里 <c>job_title</c> / <c>job_level_id</c> /
/// <c>job_family_id</c> / <c>city</c> 全是 <b>ID</b>；本域是"ID → 可读组织元数据"的唯一通道。
/// 与 <c>mdm.get_countries</c>（地区编码 → 名称）同类，但对象不同：mdm 是国家参考表，本域是租户组织定义。
/// </para>
/// <para>
/// <b>软依赖</b>：四个 SDK 客户端全部可空可选——宿主未启用对应通讯录元数据 API 时，只用<b>该客户端</b>的工具软缺席
/// （返回可执行错误文本），不牵连同域其它工具（一个客户端缺席不该让整个 org 域消失）。
/// </para>
/// <para>
/// <b>投影纪律</b>：目录属小体积参考数据 ⇒ 回填完整可判断事实；多语言名称统一折叠为 <c>locale → value</c> 对象
/// （不透出 <c>I18nContent</c> 的冗余 <c>id</c>）；<c>description</c> 原样单语言返回（多语言说明属长文本，收益低）。
/// </para>
/// <para>
/// <b>分页口径</b>：只接受 <c>page_token</c>，页大小走 SDK 默认（DP-B1-0 页不进）；信封一律经
/// <see cref="ToolResultJsons.PageEnvelope"/> 单源构造（空 token 不写入）。
/// <c>name</c> 过滤是<b>业务参数</b>（SDK 原生模糊匹配），可以下发；它与"页大小"不是一类东西，勿混为一谈。
/// </para>
/// </remarks>
internal sealed class OrgMetadataTools(
    IOptions<FeishuAgentOptions> options,
    Mud.Feishu.IFeishuTenantV3JobTitle? jobTitleClient = null,
    Mud.Feishu.IFeishuTenantV3JobLevel? jobLevelClient = null,
    Mud.Feishu.IFeishuTenantV3JobFamilies? jobFamiliesClient = null,
    Mud.Feishu.IFeishuTenantV3WorkCity? workCityClient = null)
{
    private readonly Mud.Feishu.IFeishuTenantV3JobTitle? _jobTitleClient = jobTitleClient;
    private readonly Mud.Feishu.IFeishuTenantV3JobLevel? _jobLevelClient = jobLevelClient;
    private readonly Mud.Feishu.IFeishuTenantV3JobFamilies? _jobFamiliesClient = jobFamiliesClient;
    private readonly Mud.Feishu.IFeishuTenantV3WorkCity? _workCityClient = workCityClient;

    private readonly int _maxResultLength = ToolExecutor.Require(options).MaxToolResultLength;

    // ───────────────────────────── 职务（job_title） ─────────────────────────────

    /// <summary>org.list_job_titles：分页列出职务目录。</summary>
    [FeishuToolHandler(typeof(IFeishuTenantOrgListJobTitlesTool))]
    public Task<FeishuToolResult> ListJobTitlesAsync(IReadOnlyDictionary<string, object?> arguments, CancellationToken cancellationToken)
    {
        var executor = new ToolExecutor(FeishuToolNames.OrgListJobTitles, _maxResultLength);
        if (Absent(_jobTitleClient, executor.ToolName, "IFeishuTenantV3JobTitle") is { } unavailable)
        {
            return Task.FromResult(unavailable);
        }

        return executor.RunAsync(async () =>
        {
            var args = OrgListJobTitlesArgs.Unpack(arguments);

            // page_size 刻意不下发（保持 SDK 默认）——运维参数不进模型可见面（DP-B1-0）。
            var outcome = FeishuApiResultReader.Read(await _jobTitleClient!
                .GetJobTitlesListAsync(page_token: args.PageToken, cancellationToken: cancellationToken)
                .ConfigureAwait(false));

            return executor.FromApi(outcome, result => ProjectPage(
                result,
                item => new JsonObject
                {
                    ["job_title_id"] = item.JobTitleId,
                    ["name"] = item.Name,
                    ["i18n_name"] = ProjectI18n(item.I18nName),
                    ["status"] = item.Status,
                },
                "职务",
                "① 租户尚未在管理后台配置职务；② page_token 已到末页。可省略 page_token 从第一页重新确认。"));
        });
    }

    /// <summary>org.get_job_title：按 ID 查询单个职务。</summary>
    [FeishuToolHandler(typeof(IFeishuTenantOrgGetJobTitleTool))]
    public Task<FeishuToolResult> GetJobTitleAsync(IReadOnlyDictionary<string, object?> arguments, CancellationToken cancellationToken)
    {
        var executor = new ToolExecutor(FeishuToolNames.OrgGetJobTitle, _maxResultLength);
        if (Absent(_jobTitleClient, executor.ToolName, "IFeishuTenantV3JobTitle") is { } unavailable)
        {
            return Task.FromResult(unavailable);
        }

        return executor.RunAsync(async () =>
        {
            var args = OrgGetJobTitleArgs.Unpack(arguments);

            var outcome = FeishuApiResultReader.Read(await _jobTitleClient!
                .GetJobTitleByIdAsync(args.JobTitleId, cancellationToken)
                .ConfigureAwait(false));

            return executor.FromApi(outcome, result => ProjectDetail(
                result.JobTitle,
                "职务",
                "job_title_id",
                item => new JsonObject
                {
                    ["job_title_id"] = item.JobTitleId,
                    ["name"] = item.Name,
                    ["i18n_name"] = ProjectI18n(item.I18nName),
                    ["status"] = item.Status,
                }));
        });
    }

    // ───────────────────────────── 职级（job_level） ─────────────────────────────

    /// <summary>org.list_job_levels：分页列出职务族目录（支持 name 模糊过滤）。</summary>
    [FeishuToolHandler(typeof(IFeishuTenantOrgListJobLevelsTool))]
    public Task<FeishuToolResult> ListJobLevelsAsync(IReadOnlyDictionary<string, object?> arguments, CancellationToken cancellationToken)
    {
        var executor = new ToolExecutor(FeishuToolNames.OrgListJobLevels, _maxResultLength);
        if (Absent(_jobLevelClient, executor.ToolName, "IFeishuTenantV3JobLevel") is { } unavailable)
        {
            return Task.FromResult(unavailable);
        }

        return executor.RunAsync(async () =>
        {
            var args = OrgListJobLevelsArgs.Unpack(arguments);

            // name 是 SDK 原生业务过滤（可下发）；page_size 不下发（DP-B1-0）。
            var outcome = FeishuApiResultReader.Read(await _jobLevelClient!
                .GetJobLevelListAsync(name: args.Name, page_token: args.PageToken, cancellationToken: cancellationToken)
                .ConfigureAwait(false));

            return executor.FromApi(outcome, result => ProjectPage(
                result,
                item => new JsonObject
                {
                    ["job_level_id"] = item.JobLevelId,
                    ["name"] = item.Name,
                    ["description"] = item.Description,
                    ["order"] = item.Order,
                    ["status"] = item.Status,
                    ["i18n_name"] = ProjectI18n(item.I18nName),
                },
                "职级",
                "① 租户尚未配置职级；② name 过滤过窄（可去掉 name 重试）；③ page_token 已到末页。"));
        });
    }

    /// <summary>org.get_job_level：按 ID 查询单个职级。</summary>
    [FeishuToolHandler(typeof(IFeishuTenantOrgGetJobLevelTool))]
    public Task<FeishuToolResult> GetJobLevelAsync(IReadOnlyDictionary<string, object?> arguments, CancellationToken cancellationToken)
    {
        var executor = new ToolExecutor(FeishuToolNames.OrgGetJobLevel, _maxResultLength);
        if (Absent(_jobLevelClient, executor.ToolName, "IFeishuTenantV3JobLevel") is { } unavailable)
        {
            return Task.FromResult(unavailable);
        }

        return executor.RunAsync(async () =>
        {
            var args = OrgGetJobLevelArgs.Unpack(arguments);

            var outcome = FeishuApiResultReader.Read(await _jobLevelClient!
                .GetJobLevelByIdAsync(args.JobLevelId, cancellationToken)
                .ConfigureAwait(false));

            return executor.FromApi(outcome, result => ProjectDetail(
                result.JobLevel,
                "职级",
                "job_level_id",
                item => new JsonObject
                {
                    ["job_level_id"] = item.JobLevelId,
                    ["name"] = item.Name,
                    ["description"] = item.Description,
                    ["order"] = item.Order,
                    ["status"] = item.Status,
                    ["i18n_name"] = ProjectI18n(item.I18nName),
                }));
        });
    }

    // ───────────────────────────── 职务族（job_family） ─────────────────────────────

    /// <summary>org.list_job_families：分页列出职务族目录（支持 name 模糊过滤）。</summary>
    [FeishuToolHandler(typeof(IFeishuTenantOrgListJobFamiliesTool))]
    public Task<FeishuToolResult> ListJobFamiliesAsync(IReadOnlyDictionary<string, object?> arguments, CancellationToken cancellationToken)
    {
        var executor = new ToolExecutor(FeishuToolNames.OrgListJobFamilies, _maxResultLength);
        if (Absent(_jobFamiliesClient, executor.ToolName, "IFeishuTenantV3JobFamilies") is { } unavailable)
        {
            return Task.FromResult(unavailable);
        }

        return executor.RunAsync(async () =>
        {
            var args = OrgListJobFamiliesArgs.Unpack(arguments);

            var outcome = FeishuApiResultReader.Read(await _jobFamiliesClient!
                .GetJobFamilesListAsync(name: args.Name, page_token: args.PageToken, cancellationToken: cancellationToken)
                .ConfigureAwait(false));

            return executor.FromApi(outcome, result => ProjectPage(
                result,
                item => new JsonObject
                {
                    ["job_family_id"] = item.JobFamilyId,
                    ["name"] = item.Name,
                    ["description"] = item.Description,
                    ["parent_job_family_id"] = item.ParentJobFamilyId,
                    ["status"] = item.Status,
                    ["i18n_name"] = ProjectI18n(item.I18nName),
                },
                "职务族",
                "① 租户尚未配置职务族；② name 过滤过窄（可去掉 name 重试）；③ page_token 已到末页。"));
        });
    }

    /// <summary>org.get_job_family：按 ID 查询单个职务族。</summary>
    [FeishuToolHandler(typeof(IFeishuTenantOrgGetJobFamilyTool))]
    public Task<FeishuToolResult> GetJobFamilyAsync(IReadOnlyDictionary<string, object?> arguments, CancellationToken cancellationToken)
    {
        var executor = new ToolExecutor(FeishuToolNames.OrgGetJobFamily, _maxResultLength);
        if (Absent(_jobFamiliesClient, executor.ToolName, "IFeishuTenantV3JobFamilies") is { } unavailable)
        {
            return Task.FromResult(unavailable);
        }

        return executor.RunAsync(async () =>
        {
            var args = OrgGetJobFamilyArgs.Unpack(arguments);

            var outcome = FeishuApiResultReader.Read(await _jobFamiliesClient!
                .GetJobFamilyByIdAsync(args.JobFamilyId, cancellationToken)
                .ConfigureAwait(false));

            return executor.FromApi(outcome, result => ProjectDetail(
                result.JobFamily,
                "职务族",
                "job_family_id",
                item => new JsonObject
                {
                    ["job_family_id"] = item.JobFamilyId,
                    ["name"] = item.Name,
                    ["description"] = item.Description,
                    ["parent_job_family_id"] = item.ParentJobFamilyId,
                    ["status"] = item.Status,
                    ["i18n_name"] = ProjectI18n(item.I18nName),
                }));
        });
    }

    // ───────────────────────────── 工作城市（work_city） ─────────────────────────────

    /// <summary>org.list_work_cities：分页列出工作城市目录。</summary>
    [FeishuToolHandler(typeof(IFeishuTenantOrgListWorkCitiesTool))]
    public Task<FeishuToolResult> ListWorkCitiesAsync(IReadOnlyDictionary<string, object?> arguments, CancellationToken cancellationToken)
    {
        var executor = new ToolExecutor(FeishuToolNames.OrgListWorkCities, _maxResultLength);
        if (Absent(_workCityClient, executor.ToolName, "IFeishuTenantV3WorkCity") is { } unavailable)
        {
            return Task.FromResult(unavailable);
        }

        return executor.RunAsync(async () =>
        {
            var args = OrgListWorkCitiesArgs.Unpack(arguments);

            var outcome = FeishuApiResultReader.Read(await _workCityClient!
                .GetWorkCitesListAsync(page_token: args.PageToken, cancellationToken: cancellationToken)
                .ConfigureAwait(false));

            return executor.FromApi(outcome, result => ProjectPage(
                result,
                item => new JsonObject
                {
                    ["work_city_id"] = item.WorkCityId,
                    ["name"] = item.Name,
                    ["status"] = item.Status,
                    ["i18n_name"] = ProjectI18n(item.I18nName),
                },
                "工作城市",
                "① 租户尚未启用工作城市；② page_token 已到末页。可省略 page_token 从第一页重新确认。"));
        });
    }

    /// <summary>org.get_work_city：按 ID 查询单个工作城市。</summary>
    [FeishuToolHandler(typeof(IFeishuTenantOrgGetWorkCityTool))]
    public Task<FeishuToolResult> GetWorkCityAsync(IReadOnlyDictionary<string, object?> arguments, CancellationToken cancellationToken)
    {
        var executor = new ToolExecutor(FeishuToolNames.OrgGetWorkCity, _maxResultLength);
        if (Absent(_workCityClient, executor.ToolName, "IFeishuTenantV3WorkCity") is { } unavailable)
        {
            return Task.FromResult(unavailable);
        }

        return executor.RunAsync(async () =>
        {
            var args = OrgGetWorkCityArgs.Unpack(arguments);

            var outcome = FeishuApiResultReader.Read(await _workCityClient!
                .GetWorkCityByIdAsync(args.WorkCityId, cancellationToken)
                .ConfigureAwait(false));

            return executor.FromApi(outcome, result => ProjectDetail(
                result.WorkCity,
                "工作城市",
                "work_city_id",
                item => new JsonObject
                {
                    ["work_city_id"] = item.WorkCityId,
                    ["name"] = item.Name,
                    ["status"] = item.Status,
                    ["i18n_name"] = ProjectI18n(item.I18nName),
                }));
        });
    }

    // ───────────────────────────── 公共投影 ─────────────────────────────

    /// <summary>软缺席（与 BoardTools / CountryRegionTools / SecurityAuditLogTools 同口径）：可执行错误文本，不抛异常。</summary>
    private static FeishuToolResult? Absent(object? client, string toolName, string clientName)
        => client is null
            ? FeishuToolResult.FromError(FeishuToolBinding.StructuredError(
                toolName,
                $"未注册 {clientName} 客户端——该组织元数据能力需宿主启用对应通讯录 API（软缺席，不影响 org 域其它工具）"))
            : null;

    /// <summary>分页投影：信封单源构造 + 条目逐条投影 + 空结果哨兵。</summary>
    private static JsonObject ProjectPage<T>(ApiPageListResult<T> result, Func<T, JsonObject> project, string label, string emptyReason)
    {
        var envelope = ToolResultJsons.PageEnvelope(result.HasMore, result.PageToken);
        var items = envelope["items"]!.AsArray();
        foreach (var item in result.Items ?? [])
        {
            // 条目直接写进信封 items（JsonNode 单亲约束：不能先建临时数组再搬）。
            items.AddNode(project(item));
        }

        if (items.Count == 0)
        {
            envelope["found"] = false;
            envelope["message"] = $"当前租户没有可返回的{label}条目。常见原因：{emptyReason}";
        }

        return envelope;
    }

    /// <summary>详情投影：单条 + "查不到"哨兵（哨兵文案带可执行下一步）。</summary>
    private static JsonObject ProjectDetail<T>(T? item, string label, string idFieldName, Func<T, JsonObject> project)
        where T : class
        => item is null
            ? new JsonObject
            {
                ["found"] = false,
                ["message"] = $"未查询到该{label} ID。请确认 {idFieldName} 是否来自通讯录用户对象的对应字段，"
                    + "或先用 org 域对应 list 工具列出本租户目录核对。",
            }
            : project(item);

    /// <summary>多语言名称折叠为 <c>locale → value</c> 对象（无有效条目时返回 null，不写空对象）。</summary>
    private static JsonNode? ProjectI18n(List<I18nContent>? contents)
    {
        if (contents is not { Count: > 0 })
        {
            return null;
        }

        var map = new JsonObject();
        foreach (var content in contents)
        {
            if (string.IsNullOrEmpty(content.Locale))
            {
                continue;
            }

            map[content.Locale!] = content.Value;
        }

        return map.Count > 0 ? map : null;
    }
}
