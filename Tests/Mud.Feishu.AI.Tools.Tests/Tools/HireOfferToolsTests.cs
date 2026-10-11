// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.DataModels.Hire;

namespace Mud.Feishu.AI.Tools.Tests.Tools;

/// <summary>
/// hire Offer 用例（<c>hire.list_offers</c>，<b>PII 出口</b>：只给状态，不给条件）。
/// </summary>
/// <remarks>
/// 锁三件事：① <c>talent_id</c> 透传（人才维度的唯一入口）；
/// ② 状态相关字段齐全（`offer_status` / 岗位名 / 创建时间）；
/// ③ 输出里**不出现任何金额或条件字段**——本接口结构上就不含薪资（薪资在 Offer 详情结构），
/// 这里把该事实固化成断言，防止将来有人把详情接口接进来而不自觉。
/// </remarks>
public class HireOfferToolsTests
{
    private static HireOfferTools CreateTools(Mock<Mud.Feishu.IFeishuTenantV1HireCandidate>? client = null)
        => new(Options.Create(new FeishuAgentOptions { Instructions = "test" }), client?.Object);

    private static OfferListInfo Sample() => new()
    {
        Id = "offer-001",
        ApplicationId = "app-001",
        JobInfo = new OfferJobInfo { JobId = "job-001", JobName = "高级后端工程师" },
        OfferStatus = 6,
        OfferType = 1,
        CreateTime = 1704067200000,
        EmployeeType = new BaseBilingualWithId { Id = "et-1" },
    };

    [Fact]
    public async Task ListOffers_ShouldReturnStatusFieldsOnly()
    {
        var client = new Mock<Mud.Feishu.IFeishuTenantV1HireCandidate>();
        client
            .Setup(c => c.GetOfferListAsync(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<int?>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FeishuApiResult<GetOfferListResult>
            {
                Code = 0,
                Data = new GetOfferListResult { Items = [Sample()], HasMore = true, PageToken = "next" },
            });

        var result = await CreateTools(client).ListOffersAsync(
            new Dictionary<string, object?> { ["talent_id"] = "talent-001" },
            CancellationToken.None);

        result.Error.Should().BeNull();
        var text = result.ToString();
        text.Should().Contain("offer-001").And.Contain("\"offer_status\":6").And.Contain("高级后端工程师");
        text.Should().Contain("app-001").And.Contain("1704067200000");

        text.Should().NotContain("salary", "Offer 列表不含薪资条件——金额只在 Offer 详情结构里");
        text.Should().NotContain("salary_plan");
        text.Should().NotContain("et-1", "人员类型等与'到哪一步'无关的引用一并裁掉");

        client.Verify(
            c => c.GetOfferListAsync("talent-001", It.IsAny<string?>(), It.IsAny<int?>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()),
            Times.Once,
            "talent_id 必填且必须透传（人才维度是唯一入口）");
    }

    [Fact]
    public async Task ListOffers_ShouldGiveSentinel_AndFailFastWithoutClient()
    {
        var emptyClient = new Mock<Mud.Feishu.IFeishuTenantV1HireCandidate>();
        emptyClient
            .Setup(c => c.GetOfferListAsync(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<int?>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FeishuApiResult<GetOfferListResult>
            {
                Code = 0,
                Data = new GetOfferListResult { Items = [] },
            });

        var empty = await CreateTools(emptyClient).ListOffersAsync(
            new Dictionary<string, object?> { ["talent_id"] = "talent-001" },
            CancellationToken.None);
        empty.ToString().Should().Contain("\"found\":false", "无 Offer 必须给显式哨兵（F-5）");

        var absent = await CreateTools().ListOffersAsync(
            new Dictionary<string, object?> { ["talent_id"] = "t" },
            CancellationToken.None);
        absent.Error.Should().BeNull("软缺席走文本错误出口（既有执行器同口径）");
        absent.ToString().Should().Contain("IFeishuTenantV1HireCandidate").And.Contain("hire.list_offers");
    }
}
