// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.AI.Tools.Tools;
using Mud.Feishu.DataModels.JobTitles;

namespace Mud.Feishu.AI.Tools.Internal;

/// <summary>
/// org「组织元数据 · 职务」只读工具执行器（F-1 第三个补域：<c>org.list_job_titles</c> / <c>org.get_job_title</c>）。
/// </summary>
/// <remarks>
/// <para>
/// <b>软依赖</b>：<c>IFeishuTenantV3JobTitle</c> 可空可选——宿主未启用通讯录职务 API 时本域软缺席，
/// 两个工具都返回"能力不可用"的可执行错误（不是异常，异常会逃出执行链抛给宿主）。
/// </para>
/// <para>
/// <b>投影纪律</b>：职务目录是<b>小体积参考数据</b>，故回填完整可判断事实（ID / 名称 / 多语言名称 / 启用状态）；
/// 但多语言名称按 <c>locale → value</c> 折叠成对象（而非 <c>List&lt;I18nContent&gt;</c> 原样透出），
/// 避免把 <c>id</c> 等冗余字段带进模型上下文。
/// </para>
/// <para>
/// <b>分页口径</b>：只接受 <c>page_token</c>；页大小不下发（DP-B1-0），信封经
/// <see cref="ToolResultJsons.PageEnvelope"/> 单源构造（空 token 不写入）。
/// </para>
/// </remarks>
internal sealed class OrgJobTitleTools(
    IOptions<FeishuAgentOptions> options,
    Mud.Feishu.IFeishuTenantV3JobTitle? jobTitleClient = null)
{
    private readonly Mud.Feishu.IFeishuTenantV3JobTitle? _jobTitleClient = jobTitleClient;

    private readonly int _maxResultLength = ToolExecutor.Require(options).MaxToolResultLength;

    /// <summary>org.list_job_titles：分页列出职务目录。</summary>
    [FeishuToolHandler(typeof(IFeishuTenantOrgListJobTitlesTool))]
    public Task<FeishuToolResult> ListJobTitlesAsync(IReadOnlyDictionary<string, object?> arguments, CancellationToken cancellationToken)
    {
        var executor = new ToolExecutor(FeishuToolNames.OrgListJobTitles, _maxResultLength);
        if (Absent(executor.ToolName) is { } unavailable)
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

            return executor.FromApi(outcome, ProjectList);
        });
    }

    /// <summary>org.get_job_title：按 ID 查询单个职务。</summary>
    [FeishuToolHandler(typeof(IFeishuTenantOrgGetJobTitleTool))]
    public Task<FeishuToolResult> GetJobTitleAsync(IReadOnlyDictionary<string, object?> arguments, CancellationToken cancellationToken)
    {
        var executor = new ToolExecutor(FeishuToolNames.OrgGetJobTitle, _maxResultLength);
        if (Absent(executor.ToolName) is { } unavailable)
        {
            return Task.FromResult(unavailable);
        }

        return executor.RunAsync(async () =>
        {
            var args = OrgGetJobTitleArgs.Unpack(arguments);

            var outcome = FeishuApiResultReader.Read(await _jobTitleClient!
                .GetJobTitleByIdAsync(args.JobTitleId, cancellationToken)
                .ConfigureAwait(false));

            return executor.FromApi(outcome, ProjectDetail);
        });
    }

    /// <summary>软缺席（与 BoardTools / CountryRegionTools / SecurityAuditLogTools 同口径）：返回可执行错误文本，不抛异常。</summary>
    private FeishuToolResult? Absent(string toolName)
        => _jobTitleClient is null
            ? FeishuToolResult.FromError(FeishuToolBinding.StructuredError(
                toolName,
                "未注册 IFeishuTenantV3JobTitle 客户端——职务目录能力需宿主启用通讯录职务 API（软缺席，不影响其它域工具）"))
            : null;

    /// <summary>列表投影：分页信封（单源构造）+ 折叠后的职务条目。</summary>
    private static JsonObject ProjectList(ApiPageListResult<JobTitle> result)
    {
        var envelope = ToolResultJsons.PageEnvelope(result.HasMore, result.PageToken);
        var items = envelope["items"]!.AsArray();
        foreach (var jobTitle in result.Items ?? [])
        {
            // 条目直接写进信封 items（JsonNode 单亲约束：不能先建临时数组再搬）。
            items.AddNode(ProjectJobTitle(jobTitle));
        }

        if (items.Count == 0)
        {
            envelope["found"] = false;
            envelope["message"] = "当前租户没有可返回的职务条目。常见原因：① 租户尚未在管理后台配置职务；"
                + "② page_token 已到末页。可省略 page_token 从第一页重新确认。";
        }

        return envelope;
    }

    /// <summary>详情投影：单条职务 + "查不到"哨兵。</summary>
    private static JsonObject ProjectDetail(JobTitleResult result)
    {
        if (result.JobTitle is null)
        {
            return new JsonObject
            {
                ["found"] = false,
                ["message"] = "未查询到该职务 ID。请确认 job_title_id 是否来自通讯录用户对象的 job_title 字段，"
                    + "或先用 org.list_job_titles 列出本租户的职务目录核对。",
            };
        }

        return ProjectJobTitle(result.JobTitle);
    }

    /// <summary>职务条目 → 白名单投影（多语言名称折叠为 locale → value 对象）。</summary>
    private static JsonObject ProjectJobTitle(JobTitle jobTitle)
    {
        JsonNode? i18n = null;
        if (jobTitle.I18nName is { Count: > 0 })
        {
            var map = new JsonObject();
            foreach (var content in jobTitle.I18nName)
            {
                if (string.IsNullOrEmpty(content.Locale))
                {
                    continue;
                }

                map[content.Locale!] = content.Value;
            }

            if (map.Count > 0)
            {
                i18n = map;
            }
        }

        return new JsonObject
        {
            ["job_title_id"] = jobTitle.JobTitleId,
            ["name"] = jobTitle.Name,
            ["i18n_name"] = i18n,
            ["status"] = jobTitle.Status,
        };
    }
}
