// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.DataModels.Hire;

namespace Mud.Feishu.AI.Tools.Internal;

/// <summary>
/// hire「候选人（人才）」只读工具执行器（<c>hire.list_talents</c> / <c>hire.get_talent</c>，**PII 出口**）。
/// </summary>
/// <remarks>
/// <para>
/// <b>PII 边界（本执行器的唯一要点）</b>：候选人档案里既有"联系候选人"必需的字段（姓名/手机/邮箱），
/// 也有大量<b>与本工具目的无关、却显著放大泄露面</b>的字段——身份证号、性别、年龄、国籍、籍贯、
/// 学历、工作经历、项目、作品、获奖、简历附件、自定义字段。投影按<b>白名单</b>只放前者：
/// 需要档案请走招聘系统或宿主的合规导出通道，而不是把整份简历灌进模型上下文。
/// </para>
/// <para>
/// <b>默认不启用</b>：两个工具都在 <c>scope-authority.json</c> 的 <c>piiTools</c> 登记（DP-A5-1 档位 ①），
/// 不出现在任何默认白名单——宿主必须显式加入 <c>FeishuAgent:Tools</c>。
/// </para>
/// <para>
/// <b>软依赖</b>：<c>IFeishuTenantV1HireCandidate</c> 可空可选，缺席时返回可执行错误文本。
/// </para>
/// </remarks>
internal sealed class HireTalentTools(
    IOptions<FeishuAgentOptions> options,
    Mud.Feishu.IFeishuTenantV1HireCandidate? hireCandidateClient = null)
{
    private readonly Mud.Feishu.IFeishuTenantV1HireCandidate? _hireCandidateClient = hireCandidateClient;

    private readonly int _maxResultLength = ToolExecutor.Require(options).MaxToolResultLength;

    /// <summary>P-3（统一分页语义）：<c>fetch_all</c> 的条目/页数预算（来自宿主配置）。</summary>
    private readonly int _maxAutoFetchItems = ToolExecutor.Require(options).MaxAutoFetchItems;
    private readonly int _maxAutoFetchPages = ToolExecutor.Require(options).MaxAutoFetchPages;

    /// <summary>hire.list_talents：分页检索候选人（最小投影）。</summary>
    [FeishuToolHandler(typeof(IFeishuTenantHireListTalentsTool))]
    public Task<FeishuToolResult> ListTalentsAsync(IReadOnlyDictionary<string, object?> arguments, CancellationToken cancellationToken)
    {
        var executor = new ToolExecutor(FeishuToolNames.HireListTalents, _maxResultLength);
        if (_hireCandidateClient is null)
        {
            return Task.FromResult(FeishuToolResult.FromError(FeishuToolBinding.StructuredError(
                executor.ToolName,
                "未注册 IFeishuTenantV1HireCandidate 客户端——候选人检索能力需宿主启用招聘（Hire）候选人 API（软缺席）")));
        }

        return executor.RunAsync(async () =>
        {
            var args = HireListTalentsArgs.Unpack(arguments);

            // keyword 是 SDK 原生业务过滤（可下发）；page_size / sort_by 不下发（DP-B1-0）。
            // P-3：统一分页出口——聚合路径与单页路径共用**同一次调用定义**与**同一份投影**。
            async Task<FeishuApiOutcome<GetTalentListResult>> FetchPageAsync(string? token, CancellationToken ct)
                => FeishuApiResultReader.Read(await _hireCandidateClient
                    .GetTalentListAsync(
                        query: new TalentListQuery { Keyword = args.Keyword, PageToken = token },
                        cancellationToken: ct)
                    .ConfigureAwait(false));

            return await executor.FromPagedApiAsync(
                FetchPageAsync, ProjectPage, args.FetchAll, args.MaxItems, args.PageToken,
                _maxAutoFetchItems, _maxAutoFetchPages, cancellationToken).ConfigureAwait(false);
        });
    }

    /// <summary>hire.get_talent：按 ID 查询单个候选人的联系与在职字段。</summary>
    [FeishuToolHandler(typeof(IFeishuTenantHireGetTalentTool))]
    public Task<FeishuToolResult> GetTalentAsync(IReadOnlyDictionary<string, object?> arguments, CancellationToken cancellationToken)
    {
        var executor = new ToolExecutor(FeishuToolNames.HireGetTalent, _maxResultLength);
        if (_hireCandidateClient is null)
        {
            return Task.FromResult(FeishuToolResult.FromError(FeishuToolBinding.StructuredError(
                executor.ToolName,
                "未注册 IFeishuTenantV1HireCandidate 客户端——候选人检索能力需宿主启用招聘（Hire）候选人 API（软缺席）")));
        }

        return executor.RunAsync(async () =>
        {
            var args = HireGetTalentArgs.Unpack(arguments);

            var outcome = FeishuApiResultReader.Read(await _hireCandidateClient
                .GetTalentAsync(args.TalentId, cancellationToken: cancellationToken)
                .ConfigureAwait(false));

            return executor.FromApi(outcome, result => result.Talent is null
                ? new JsonObject
                {
                    ["found"] = false,
                    ["message"] = "未查询到该人才 ID。请确认 talent_id 是否来自 hire.list_talents 返回的 talent_id，"
                        + "或先检索一次候选人对齐 ID。",
                }
                : ProjectTalent(result.Talent, withContact: true));
        });
    }

    /// <summary>列表投影：信封单源 + 最小条目（只给定位字段，不带任何联系方式与档案）。</summary>
    private static JsonObject ProjectPage(ApiPageListResult<Talent> result)
    {
        var envelope = ToolResultJsons.PageEnvelope(result.HasMore, result.PageToken);
        var items = envelope["items"]!.AsArray();

        foreach (var talent in result.Items ?? [])
        {
            items.AddNode(ProjectTalent(talent, withContact: false));
        }

        if (items.Count == 0)
        {
            envelope["found"] = false;
            envelope["message"] = "没有匹配的候选人。可换更短的关键字重试，或省略 keyword 按更新时间翻页；"
                + "若仍为空，确认该租户在招聘系统里确有候选人数据。";
        }

        return envelope;
    }

    /// <summary>
    /// 候选人投影：<paramref name="withContact"/> 为假只给定位字段（列表），为真补上联系方式（详情）。
    /// <b>两种形态都不含</b>身份证号 / 性别 / 年龄 / 国籍 / 籍贯 / 学历 / 经历 / 作品 / 获奖 / 附件等档案与敏感字段。
    /// </summary>
    private static JsonObject ProjectTalent(Talent talent, bool withContact)
    {
        var basic = talent.BasicInfo;

        var node = new JsonObject
        {
            ["talent_id"] = talent.Id,
            ["name"] = basic?.Name,
            ["is_onboarded"] = talent.IsOnboarded,
        };

        if (withContact)
        {
            node["mobile"] = basic?.Mobile;
            node["email"] = basic?.Email;
            node["experience_years"] = basic?.ExperienceYears;
        }

        return node;
    }
}
