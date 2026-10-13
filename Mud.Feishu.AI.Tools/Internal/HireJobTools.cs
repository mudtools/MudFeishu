// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.DataModels.Hire;

namespace Mud.Feishu.AI.Tools.Internal;

/// <summary>
/// hire「招聘岗位」只读工具执行器（F-1 P0 首批：<c>hire.list_jobs</c> / <c>hire.get_job</c>）。
/// </summary>
/// <remarks>
/// <para>
/// <b>非 PII</b>：只处理职位/岗位对象，不触达候选人；投影<b>丢弃</b>
/// <c>CreateUserId</c>（创建人属个人信息）与 <c>HighlightList</c> / 各嵌套引用对象的细枝，
/// 只留模型能据以作答的字段。
/// </para>
/// <para>
/// <b>list 精简 / get 完整</b>：岗位描述常达数千字，列表逐条带出会瞬间打满结果预算
/// （<c>MaxToolResultLength</c> 截断后反而丢掉其它条目）⇒ 列表只给定位字段，正文留给单条 get。
/// 这是"结果预算"在投影层的体现，不是偷懒。
/// </para>
/// <para>
/// <b>软依赖</b>：<c>IFeishuTenantV1HireJob</c> 可空可选，缺席时返回可执行错误文本。
/// </para>
/// </remarks>
internal sealed class HireJobTools(
    IOptions<FeishuAgentOptions> options,
    Mud.Feishu.IFeishuTenantV1HireJob? hireJobClient = null)
{
    private readonly Mud.Feishu.IFeishuTenantV1HireJob? _hireJobClient = hireJobClient;

    private readonly int _maxResultLength = ToolExecutor.Require(options).MaxToolResultLength;

    /// <summary>P-3（统一分页语义）：<c>fetch_all</c> 的条目/页数预算（来自宿主配置）。</summary>
    private readonly int _maxAutoFetchItems = ToolExecutor.Require(options).MaxAutoFetchItems;
    private readonly int _maxAutoFetchPages = ToolExecutor.Require(options).MaxAutoFetchPages;

    /// <summary>hire.list_jobs：分页列出招聘岗位（精简投影）。</summary>
    [FeishuToolHandler(typeof(IFeishuTenantHireListJobsTool))]
    public Task<FeishuToolResult> ListJobsAsync(IReadOnlyDictionary<string, object?> arguments, CancellationToken cancellationToken)
    {
        var executor = new ToolExecutor(FeishuToolNames.HireListJobs, _maxResultLength);
        if (_hireJobClient is null)
        {
            return Task.FromResult(FeishuToolResult.FromError(FeishuToolBinding.StructuredError(
                executor.ToolName,
                "未注册 IFeishuTenantV1HireJob 客户端——招聘岗位能力需宿主启用招聘（Hire）API（软缺席）")));
        }

        return executor.RunAsync(async () =>
        {
            var args = HireListJobsArgs.Unpack(arguments);

            // P-3：统一分页出口——聚合路径与单页路径共用**同一次调用定义**与**同一份投影**。
            async Task<FeishuApiOutcome<GetJobListResult>> FetchPageAsync(string? token, CancellationToken ct)
                => FeishuApiResultReader.Read(await _hireJobClient
                    .GetJobListAsync(
                        query: new JobListQuery { PageToken = token },
                        cancellationToken: ct)
                    .ConfigureAwait(false));

            return await executor.FromPagedApiAsync(
                FetchPageAsync, result => ProjectPage(result, brief: true), args.FetchAll, args.MaxItems, args.PageToken,
                _maxAutoFetchItems, _maxAutoFetchPages, cancellationToken).ConfigureAwait(false);
        });
    }

    /// <summary>hire.get_job：按 ID 查询单个岗位（完整投影）。</summary>
    [FeishuToolHandler(typeof(IFeishuTenantHireGetJobTool))]
    public Task<FeishuToolResult> GetJobAsync(IReadOnlyDictionary<string, object?> arguments, CancellationToken cancellationToken)
    {
        var executor = new ToolExecutor(FeishuToolNames.HireGetJob, _maxResultLength);
        if (_hireJobClient is null)
        {
            return Task.FromResult(FeishuToolResult.FromError(FeishuToolBinding.StructuredError(
                executor.ToolName,
                "未注册 IFeishuTenantV1HireJob 客户端——招聘岗位能力需宿主启用招聘（Hire）API（软缺席）")));
        }

        return executor.RunAsync(async () =>
        {
            var args = HireGetJobArgs.Unpack(arguments);

            var outcome = FeishuApiResultReader.Read(await _hireJobClient
                .GetJobAsync(args.JobId, cancellationToken: cancellationToken)
                .ConfigureAwait(false));

            return executor.FromApi(outcome, result => result.Job is null
                ? new JsonObject
                {
                    ["found"] = false,
                    ["message"] = "未查询到该职位 ID。请确认 job_id 是否来自 hire.list_jobs 返回的 id，或先列一次在招岗位核对。",
                }
                : ProjectJob(result.Job, brief: false));
        });
    }

    /// <summary>分页投影：信封单源构造 + 条目投影 + 空结果哨兵。</summary>
    private static JsonObject ProjectPage(ApiPageListResult<Job> result, bool brief)
    {
        var envelope = ToolResultJsons.PageEnvelope(result.HasMore, result.PageToken);
        var items = envelope["items"]!.AsArray();

        foreach (var job in result.Items ?? [])
        {
            items.AddNode(ProjectJob(job, brief));
        }

        if (items.Count == 0)
        {
            envelope["found"] = false;
            envelope["message"] = "当前没有可返回的岗位条目。常见原因：① 租户尚未创建招聘岗位；"
                + "② page_token 已到末页（可省略 page_token 从第一页重新确认）。";
        }

        return envelope;
    }

    /// <summary>
    /// 岗位投影：<paramref name="brief"/> 为真只给定位字段（列表），为假补上正文（详情）。
    /// 两种形态都<b>不带出 CreateUserId</b>（创建人属个人信息）。
    /// </summary>
    private static JsonObject ProjectJob(Job job, bool brief)
    {
        var node = new JsonObject
        {
            ["id"] = job.Id,
            ["title"] = job.Title,
            ["code"] = job.Code,
            ["active_status"] = job.ActiveStatus,
            ["update_time"] = job.UpdateTime,
        };

        if (!brief)
        {
            node["description"] = job.Description;
            node["requirement"] = job.Requirement;
            node["create_time"] = job.CreateTime;
        }

        return node;
    }
}
