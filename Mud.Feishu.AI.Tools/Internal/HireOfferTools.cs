// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.DataModels.Hire;

namespace Mud.Feishu.AI.Tools.Internal;

/// <summary>
/// hire「Offer」只读工具执行器（<c>hire.list_offers</c>，**PII 出口**：只给状态，不给条件）。
/// </summary>
/// <remarks>
/// <para>
/// <b>结构性最小集</b>：本接口返回的 <c>OfferListInfo</c> 只有
/// <c>Id</c> / <c>JobInfo</c> / <c>CreateTime</c> / <c>OfferStatus</c> / <c>OfferType</c> /
/// <c>ApplicationId</c>——<b>薪资条件在另一条结构里</b>（<c>ApplicationOfferSalaryPlan</c>，仅 Offer 详情返回），
/// 故本工具天然不会把金额带进模型上下文。这不是巧合而是挑选接口的结果：
/// 需要"到哪一步了"就只调"状态面"的接口，不调"条件面"的详情接口。
/// </para>
/// <para>
/// <b>PII</b>：某人是否获得 Offer 属个人信息 ⇒ 已在 <c>scope-authority.json</c> 的 <c>piiTools</c> 登记
/// （默认不启用），宿主须显式加入 <c>FeishuAgent:Tools</c>。
/// </para>
/// <para><b>软依赖</b>：客户端可空可选，缺席返回可执行错误文本。</para>
/// </remarks>
internal sealed class HireOfferTools(
    IOptions<FeishuAgentOptions> options,
    Mud.Feishu.IFeishuTenantV1HireCandidate? hireCandidateClient = null)
{
    private readonly Mud.Feishu.IFeishuTenantV1HireCandidate? _hireCandidateClient = hireCandidateClient;

    private readonly int _maxResultLength = ToolExecutor.Require(options).MaxToolResultLength;

    /// <summary>P-3（统一分页语义）：<c>fetch_all</c> 的条目/页数预算（来自宿主配置）。</summary>
    private readonly int _maxAutoFetchItems = ToolExecutor.Require(options).MaxAutoFetchItems;
    private readonly int _maxAutoFetchPages = ToolExecutor.Require(options).MaxAutoFetchPages;

    /// <summary>hire.list_offers：按人才 ID 查询其 Offer 列表（只回状态相关字段）。</summary>
    [FeishuToolHandler(typeof(IFeishuTenantHireListOffersTool))]
    public Task<FeishuToolResult> ListOffersAsync(IReadOnlyDictionary<string, object?> arguments, CancellationToken cancellationToken)
    {
        var executor = new ToolExecutor(FeishuToolNames.HireListOffers, _maxResultLength);
        if (_hireCandidateClient is null)
        {
            return Task.FromResult(FeishuToolResult.FromError(FeishuToolBinding.StructuredError(
                executor.ToolName,
                "未注册 IFeishuTenantV1HireCandidate 客户端——Offer 查询能力需宿主启用招聘（Hire）候选人 API（软缺席）")));
        }

        return executor.RunAsync(async () =>
        {
            var args = HireListOffersArgs.Unpack(arguments);

            // page_size 刻意不下发（保持 SDK 默认）——运维参数不进模型可见面（DP-B1-0）。
            // P-3：统一分页出口——聚合路径与单页路径共用**同一次调用定义**与**同一份投影**。
            async Task<FeishuApiOutcome<GetOfferListResult>> FetchPageAsync(string? token, CancellationToken ct)
                => FeishuApiResultReader.Read(await _hireCandidateClient
                    .GetOfferListAsync(args.TalentId, page_token: token, cancellationToken: ct)
                    .ConfigureAwait(false));

            return await executor.FromPagedApiAsync(
                FetchPageAsync, ProjectPage, args.FetchAll, args.MaxItems, args.PageToken,
                _maxAutoFetchItems, _maxAutoFetchPages, cancellationToken).ConfigureAwait(false);
        });
    }

    /// <summary>分页投影：信封单源 + 状态相关条目（无金额、无条件）+ 空结果哨兵。</summary>
    private static JsonObject ProjectPage(ApiPageListResult<OfferListInfo> result)
    {
        var envelope = ToolResultJsons.PageEnvelope(result.HasMore, result.PageToken);
        var items = envelope["items"]!.AsArray();

        foreach (var offer in result.Items ?? [])
        {
            items.AddNode(new JsonObject
            {
                ["offer_id"] = offer.Id,
                ["application_id"] = offer.ApplicationId,
                ["job_id"] = offer.JobInfo?.JobId,
                ["job_name"] = offer.JobInfo?.JobName,
                ["offer_status"] = offer.OfferStatus,
                ["offer_type"] = offer.OfferType,
                ["create_time"] = offer.CreateTime,
            });
        }

        if (items.Count == 0)
        {
            envelope["found"] = false;
            envelope["message"] = "该人才没有 Offer 记录（或不在当前分页）。常见原因：① 尚未进入 Offer 环节；"
                + "② talent_id 有误（请用 hire.list_talents 核对）；③ page_token 已到末页。";
        }

        return envelope;
    }
}
