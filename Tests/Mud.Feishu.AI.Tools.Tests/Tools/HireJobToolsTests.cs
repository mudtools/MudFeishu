// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.DataModels.Hire;

namespace Mud.Feishu.AI.Tools.Tests.Tools;

/// <summary>
/// F-1 P0 首批用例：hire 招聘岗位（<c>hire.list_jobs</c> / <c>hire.get_job</c>）。
/// </summary>
/// <remarks>
/// 锁三件事：
/// ① <b>list 精简 / get 完整</b>——岗位正文（JD）常达数千字，列表逐条带出会打满结果预算，
///    故列表只给定位字段、正文留给单条 get；
/// ② <b>不带出创建人</b>（<c>CreateUserId</c> 属个人信息）；
/// ③ 查不到给显式哨兵（F-5）。
/// </remarks>
public class HireJobToolsTests
{
    private static HireJobTools CreateTools(Mock<Mud.Feishu.IFeishuTenantV1HireJob>? client = null)
        => new(Options.Create(new FeishuAgentOptions { Instructions = "test" }), client?.Object);

    private static Job Sample() => new()
    {
        Id = "job-001",
        Title = "高级后端工程师",
        Code = "ENG-007",
        Description = "负责核心服务的设计与开发（很长的 JD 正文……）",
        Requirement = "五年以上后端经验",
        ActiveStatus = 1,
        CreateTime = 1700000000,
        UpdateTime = 1705312800,
        CreateUserId = "ou_creator_should_not_leak",
    };

    [Fact]
    public async Task ListJobs_ShouldProjectBriefOnly()
    {
        var client = new Mock<Mud.Feishu.IFeishuTenantV1HireJob>();
        client
            .Setup(c => c.GetJobListAsync(It.IsAny<JobListQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FeishuApiResult<GetJobListResult>
            {
                Code = 0,
                Data = new GetJobListResult { Items = [Sample()], HasMore = false },
            });

        var result = await CreateTools(client).ListJobsAsync(new Dictionary<string, object?>(), CancellationToken.None);

        result.Error.Should().BeNull();
        var text = result.ToString();
        text.Should().Contain("高级后端工程师").And.Contain("ENG-007");
        text.Should().NotContain("负责核心服务的设计与开发", "列表不带 JD 正文——否则一条就吃满结果预算");
        text.Should().NotContain("ou_creator_should_not_leak", "创建人属个人信息，proj 一律不带出");
    }

    [Fact]
    public async Task GetJob_ShouldIncludeFullText_AndSentinelWhenMissing()
    {
        var client = new Mock<Mud.Feishu.IFeishuTenantV1HireJob>();
        client
            .Setup(c => c.GetJobAsync(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FeishuApiResult<GetJobResult>
            {
                Code = 0,
                Data = new GetJobResult { Job = Sample() },
            });

        var found = await CreateTools(client).GetJobAsync(
            new Dictionary<string, object?> { ["job_id"] = "job-001" },
            CancellationToken.None);

        found.Error.Should().BeNull();
        found.ToString().Should().Contain("负责核心服务的设计与开发", "详情必须给完整正文（列表不给）");
        found.ToString().Should().Contain("五年以上后端经验");
        found.ToString().Should().NotContain("ou_creator_should_not_leak");

        // 查不到 → 显式哨兵。
        var emptyClient = new Mock<Mud.Feishu.IFeishuTenantV1HireJob>();
        emptyClient
            .Setup(c => c.GetJobAsync(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FeishuApiResult<GetJobResult> { Code = 0, Data = new GetJobResult() });

        var missing = await CreateTools(emptyClient).GetJobAsync(
            new Dictionary<string, object?> { ["job_id"] = "job-nope" },
            CancellationToken.None);

        missing.ToString().Should().Contain("\"found\":false", "查不到必须给显式哨兵（F-5）");
    }
}
