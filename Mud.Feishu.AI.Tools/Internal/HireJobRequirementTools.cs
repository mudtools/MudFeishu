// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.AI.Tools.Tools;
using Mud.Feishu.DataModels.Hire;

namespace Mud.Feishu.AI.Tools.Internal;

/// <summary>
/// hire「招聘需求」只读工具执行器（<c>hire.list_job_requirements</c>，非 PII）。
/// </summary>
/// <remarks>
/// <para>
/// <b>需求 ≠ 职位</b>：「职位（job）」是对外发布的岗位，「招聘需求（job_requirement）」是内部用人计划
/// （需求人数 / 职级区间 / 部门 / 起止时间 / 审批状态 / 关联职位）——两套对象，互不替代。
/// </para>
/// <para>
/// <b>裁剪（"与本工具目的无关"判据）</b>：SDK 的需求对象里还带<b>招聘负责人 / 用人经理 / 直属上级</b>
/// （<c>RecruiterList</c> / <c>JrHiringManagers</c> / <c>DirectLeaderList</c>）、<c>CreatorId</c> 与
/// <c>MinSalary</c> / <c>MaxSalary</c>。这些<b>不是"更敏感所以更该保护"，而是"对定位与概览需求没有帮助"</b>，
/// 故一律不投影；结果是本工具保持<b>非 PII</b>，无需进入 PII 登记（也就不必让宿主为看几个需求而开 PII 白名单）。
/// </para>
/// <para><b>软依赖</b>：<c>IFeishuTenantV1HireJobRequirement</c> 可空可选，缺席返回可执行错误文本。</para>
/// </remarks>
internal sealed class HireJobRequirementTools(
    IOptions<FeishuAgentOptions> options,
    Mud.Feishu.IFeishuTenantV1HireJobRequirement? hireJobRequirementClient = null)
{
    private readonly Mud.Feishu.IFeishuTenantV1HireJobRequirement? _hireJobRequirementClient = hireJobRequirementClient;

    private readonly int _maxResultLength = ToolExecutor.Require(options).MaxToolResultLength;

    /// <summary>hire.list_job_requirements：分页列出招聘需求（可按职位 ID 过滤）。</summary>
    [FeishuToolHandler(typeof(IFeishuTenantHireListJobRequirementsTool))]
    public Task<FeishuToolResult> ListJobRequirementsAsync(IReadOnlyDictionary<string, object?> arguments, CancellationToken cancellationToken)
    {
        var executor = new ToolExecutor(FeishuToolNames.HireListJobRequirements, _maxResultLength);
        if (_hireJobRequirementClient is null)
        {
            return Task.FromResult(FeishuToolResult.FromError(FeishuToolBinding.StructuredError(
                executor.ToolName,
                "未注册 IFeishuTenantV1HireJobRequirement 客户端——招聘需求能力需宿主启用招聘（Hire）需求 API（软缺席）")));
        }

        return executor.RunAsync(async () =>
        {
            var args = HireListJobRequirementsArgs.Unpack(arguments);

            // job_id 是 SDK 原生业务过滤（可下发）；page_size 不下发（DP-B1-0）。
            var outcome = FeishuApiResultReader.Read(await _hireJobRequirementClient
                .GetJobRequirementListAsync(
                    query: new JobRequirementListQuery { JobId = args.JobId, PageToken = args.PageToken },
                    cancellationToken: cancellationToken)
                .ConfigureAwait(false));

            return executor.FromApi(outcome, ProjectPage);
        });
    }

    /// <summary>分页投影：信封单源 + 裁剪后的需求条目 + 空结果哨兵。</summary>
    private static JsonObject ProjectPage(ApiPageListResult<JobRequirement> result)
    {
        var envelope = ToolResultJsons.PageEnvelope(result.HasMore, result.PageToken);
        var items = envelope["items"]!.AsArray();

        foreach (var requirement in result.Items ?? [])
        {
            items.AddNode(ProjectRequirement(requirement));
        }

        if (items.Count == 0)
        {
            envelope["found"] = false;
            envelope["message"] = "没有可返回的招聘需求。常见原因：① 该职位未挂招聘需求（去掉 job_id 再试一次可区分）；"
                + "② 租户未启用招聘需求模块；③ page_token 已到末页。";
        }

        return envelope;
    }

    /// <summary>
    /// 引用对象投影（部门 / 职级）：`{ id, zh_cn, en_us }`——保留 ID 以便与 <c>contact.*</c> 的部门工具衔接，
    /// 名称取中英两个语种（飞书此处返回 <c>I18nName</c> 多语言对象，不展开会在上下文里留下一堆无意义语言字段）。
    /// </summary>
    private static JsonNode? ProjectIdName(IdNameObject? value)
        => value is null
            ? null
            : new JsonObject
            {
                ["id"] = value.Id,
                ["zh_cn"] = value.Name?.ZhCn,
                ["en_us"] = value.Name?.EnUs,
            };

    /// <summary>
    /// 需求条目投影（白名单）：只留组织用人计划本身；
    /// <b>不含</b>招聘负责人 / 用人经理 / 直属上级 / 创建人 / 薪资区间（见类注释）。
    /// </summary>
    private static JsonObject ProjectRequirement(JobRequirement requirement)
    {
        JsonNode? jobIds = null;
        if (requirement.JobIdList is { Length: > 0 })
        {
            var array = new JsonArray();
            foreach (var jobId in requirement.JobIdList)
            {
                array.Add(jobId);
            }

            jobIds = array;
        }

        return new JsonObject
        {
            ["id"] = requirement.Id,
            ["short_code"] = requirement.ShortCode,
            ["name"] = requirement.Name,
            ["head_count"] = requirement.HeadCount,
            ["priority"] = requirement.Priority,
            ["display_progress"] = requirement.DisplayProgress,
            ["department"] = ProjectIdName(requirement.Department),
            ["min_level"] = ProjectIdName(requirement.MinLevel),
            ["max_level"] = ProjectIdName(requirement.MaxLevel),
            ["start_time"] = requirement.StartTime,
            ["deadline"] = requirement.Deadline,
            ["approval_status"] = requirement.ApprovalStatus,
            ["job_id_list"] = jobIds,
            ["update_time"] = requirement.UpdateTime,
        };
    }
}
