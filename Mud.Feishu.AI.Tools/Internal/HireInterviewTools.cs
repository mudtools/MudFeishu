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
/// hire「面试安排」只读工具执行器（<c>hire.list_interviews</c>，**PII 出口**）。
/// </summary>
/// <remarks>
/// <para>
/// <b>投影纪律</b>：只保留"安排三要素"——时间 / 轮次 / 阶段。<b>不返回</b>
/// <c>InterviewRecordList</c>（面试记录与评价正文）、<c>FeedbackSubmitTime</c>、<c>Remark</c>、
/// <c>ContactUser</c> / <c>ContactMobile</c>（联系人）、<c>Creator</c>、<c>Address</c> / <c>MeetingRoomList</c>：
/// 评价是对人的主观判断，地址与手机则与"知不知道面试安排"无关——按纪律 26 的"目的判据"一律裁掉。
/// </para>
/// <para>
/// <b>PII</b>：面试安排即"某人正在应聘某职位"的事实 ⇒ 已在 <c>scope-authority.json</c> 的 <c>piiTools</c>
/// 登记（默认不启用），宿主须显式加入 <c>FeishuAgent:Tools</c>。
/// </para>
/// <para><b>软依赖</b>：客户端可空可选，缺席返回可执行错误文本。</para>
/// </remarks>
internal sealed class HireInterviewTools(
    IOptions<FeishuAgentOptions> options,
    Mud.Feishu.IFeishuTenantV1HireCandidate? hireCandidateClient = null)
{
    private readonly Mud.Feishu.IFeishuTenantV1HireCandidate? _hireCandidateClient = hireCandidateClient;

    private readonly int _maxResultLength = ToolExecutor.Require(options).MaxToolResultLength;

    /// <summary>hire.list_interviews：分页查询面试安排（可限定投递与时间窗）。</summary>
    [FeishuToolHandler(typeof(IFeishuTenantHireListInterviewsTool))]
    public Task<FeishuToolResult> ListInterviewsAsync(IReadOnlyDictionary<string, object?> arguments, CancellationToken cancellationToken)
    {
        var executor = new ToolExecutor(FeishuToolNames.HireListInterviews, _maxResultLength);
        if (_hireCandidateClient is null)
        {
            return Task.FromResult(FeishuToolResult.FromError(FeishuToolBinding.StructuredError(
                executor.ToolName,
                "未注册 IFeishuTenantV1HireCandidate 客户端——面试安排能力需宿主启用招聘（Hire）候选人 API（软缺席）")));
        }

        return executor.RunAsync(async () =>
        {
            var args = HireListInterviewsArgs.Unpack(arguments);

            // application_id / 时间窗是 SDK 原生业务过滤（可下发）；page_size 不下发（DP-B1-0）。
            var outcome = FeishuApiResultReader.Read(await _hireCandidateClient
                .GetInterviewListAsync(
                    query: new InterviewListQuery
                    {
                        ApplicationId = args.ApplicationId,
                        StartTime = args.StartTime,
                        EndTime = args.EndTime,
                        PageToken = args.PageToken,
                    },
                    cancellationToken: cancellationToken)
                .ConfigureAwait(false));

            return executor.FromApi(outcome, ProjectPage);
        });
    }

    /// <summary>分页投影：信封单源 + 安排三要素（时间 / 轮次 / 阶段）+ 空结果哨兵。</summary>
    private static JsonObject ProjectPage(ApiPageListResult<InterviewExtend> result)
    {
        var envelope = ToolResultJsons.PageEnvelope(result.HasMore, result.PageToken);
        var items = envelope["items"]!.AsArray();

        foreach (var interview in result.Items ?? [])
        {
            items.AddNode(new JsonObject
            {
                ["interview_id"] = interview.Id,
                ["application_id"] = interview.ApplicationId,
                ["round"] = interview.Round,
                ["round_type"] = ProjectIdName(interview.InterviewRoundType),
                ["stage"] = ProjectIdName(interview.Stage),
                ["begin_time"] = interview.BeginTime,
                ["end_time"] = interview.EndTime,
                ["arrangement_status"] = interview.ArrangementStatus,
            });
        }

        if (items.Count == 0)
        {
            envelope["found"] = false;
            envelope["message"] = "该条件下没有面试安排。常见原因：① 时间窗/投递过滤过窄（去掉过滤重试一次可区分）；"
                + "② 该投递尚未安排面试；③ page_token 已到末页。";
        }

        return envelope;
    }

    /// <summary>引用对象投影（轮次类型 / 阶段）：`{ id, zh_cn, en_us }`——顺带把"轮次 ID → 可读名称"补上。</summary>
    private static JsonNode? ProjectIdName(IdNameObject? value)
        => value is null
            ? null
            : new JsonObject
            {
                ["id"] = value.Id,
                ["zh_cn"] = value.Name?.ZhCn,
                ["en_us"] = value.Name?.EnUs,
            };
}
