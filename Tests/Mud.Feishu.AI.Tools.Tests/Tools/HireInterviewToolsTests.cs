// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.DataModels.Hire;

namespace Mud.Feishu.AI.Tools.Tests.Tools;

/// <summary>
/// hire 面试安排用例（<c>hire.list_interviews</c>，<b>PII 出口</b>）。
/// </summary>
/// <remarks>
/// 锁三件事：① 过滤透传（<c>application_id</c> + 时间窗——这是"某候选人/某周"两种问法的入口）；
/// ② 轮次 ID 被换成可读名称（<c>round_type.zh_cn</c>）；
/// ③ <b>评价正文 / 备注 / 联系人手机 / 面试官 / 会议室一律不出现</b>——面试安排只需"何时、哪轮、什么状态"。
/// </remarks>
public class HireInterviewToolsTests
{
    private static HireInterviewTools CreateTools(Mock<Mud.Feishu.IFeishuTenantV1HireCandidate>? client = null)
        => new(Options.Create(new FeishuAgentOptions { Instructions = "test" }), client?.Object);

    private static InterviewExtend Sample() => new()
    {
        Id = "interview-001",
        ApplicationId = "app-001",
        Round = 2,
        InterviewRoundType = new IdNameObject { Id = "rt-2", Name = new I18nName { ZhCn = "专业面", EnUs = "Tech Round" } },
        Stage = new IdNameObject { Id = "stage-1", Name = new I18nName { ZhCn = "面试中" } },
        BeginTime = 1704067200000,
        EndTime = 1704070800000,
        ArrangementStatus = 1,

        // ⚠️ 下列字段必须被裁剪掉（与"知不知道面试安排"无关）：
        Remark = "REMARK-SHOULD-NOT-LEAK",
        ContactMobile = "13900000000",
        Creator = new IdNameObject { Id = "ou_creator", Name = new I18nName { ZhCn = "创建人姓名" } },
        InterviewRecordList = [new InterviewRecord { Id = "rec-1", Content = "EVALUATION-SHOULD-NOT-LEAK" }],
    };

    [Fact]
    public async Task ListInterviews_ShouldReturnScheduleOnly_AndForwardFilters()
    {
        var client = new Mock<Mud.Feishu.IFeishuTenantV1HireCandidate>();
        client
            .Setup(c => c.GetInterviewListAsync(It.IsAny<InterviewListQuery?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FeishuApiResult<GetInterviewListResult>
            {
                Code = 0,
                Data = new GetInterviewListResult { Items = [Sample()], HasMore = true, PageToken = "next" },
            });

        var result = await CreateTools(client).ListInterviewsAsync(
            new Dictionary<string, object?>
            {
                ["application_id"] = "app-001",
                ["start_time"] = "1704067200000",
                ["end_time"] = "1704153600000",
            },
            CancellationToken.None);

        result.Error.Should().BeNull();
        var text = result.ToString();
        text.Should().Contain("interview-001").And.Contain("app-001");
        text.Should().Contain("\"round\":2").And.Contain("专业面", "轮次 ID 必须被换成可读名称");
        text.Should().Contain("面试中").And.Contain("1704067200000");

        text.Should().NotContain("EVALUATION-SHOULD-NOT-LEAK", "面试评价正文不得返回");
        text.Should().NotContain("REMARK-SHOULD-NOT-LEAK", "备注不得返回");
        text.Should().NotContain("13900000000", "联系人手机不得返回");
        text.Should().NotContain("创建人姓名", "创建人不得返回");

        client.Verify(
            c => c.GetInterviewListAsync(
                It.Is<InterviewListQuery?>(q => q != null
                    && q.ApplicationId == "app-001"
                    && q.StartTime == "1704067200000"
                    && q.EndTime == "1704153600000"),
                It.IsAny<CancellationToken>()),
            Times.Once,
            "application_id 与时间窗是业务过滤，必须透传");
    }

    [Fact]
    public async Task ListInterviews_ShouldFailFast_WhenClientIsAbsent()
    {
        var result = await CreateTools().ListInterviewsAsync(new Dictionary<string, object?>(), CancellationToken.None);

        result.Error.Should().BeNull("软缺席走文本错误出口（既有执行器同口径）");
        result.ToString().Should().Contain("IFeishuTenantV1HireCandidate").And.Contain("hire.list_interviews");
    }
}
