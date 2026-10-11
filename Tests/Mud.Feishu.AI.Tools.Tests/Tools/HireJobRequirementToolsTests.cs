// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.DataModels.Hire;

namespace Mud.Feishu.AI.Tools.Tests.Tools;

/// <summary>
/// hire 招聘需求用例（<c>hire.list_job_requirements</c>，非 PII）。
/// </summary>
/// <remarks>
/// 锁两件事：① <c>job_id</c> 过滤透传（职位 ↔ 需求的连接点）；
/// ② <b>裁剪有效</b>——负责人 / 用人经理 / 创建人 / 薪资区间不得出现（正因裁掉这些，本工具才能保持非 PII、
/// 不必让宿主为看几个需求而开 PII 白名单）。
/// </remarks>
public class HireJobRequirementToolsTests
{
    private static HireJobRequirementTools CreateTools(Mock<Mud.Feishu.IFeishuTenantV1HireJobRequirement>? client = null)
        => new(Options.Create(new FeishuAgentOptions { Instructions = "test" }), client?.Object);

    private static JobRequirement Sample() => new()
    {
        Id = "jr-001",
        ShortCode = "JR2026-007",
        Name = "高级后端（Q4 补员）",
        HeadCount = 3,
        Priority = 1,
        DisplayProgress = 2,
        Department = new IdNameObject { Id = "od-dept-1", Name = new I18nName { ZhCn = "研发中心", EnUs = "R&D Center" } },
        MinLevel = new IdNameObject { Id = "od-level-5", Name = new I18nName { ZhCn = "P5" } },
        MaxLevel = new IdNameObject { Id = "od-level-7", Name = new I18nName { ZhCn = "P7" } },
        StartTime = "2026-01-01",
        Deadline = "2026-03-31",
        ApprovalStatus = 2,
        JobIdList = ["job-001"],
        UpdateTime = "2026-01-05",

        // ⚠️ 下列字段必须被裁剪掉（与本工具目的无关）：
        MinSalary = "30000",
        MaxSalary = "50000",
        CreatorId = "ou_creator_should_not_leak",
        RecruiterList = [new IdNameObject { Id = "ou_recruiter", Name = new I18nName { ZhCn = "李雷" } }],
        JrHiringManagers = [new IdNameObject { Id = "ou_manager", Name = new I18nName { ZhCn = "韩梅梅" } }],
        DirectLeaderList = [new IdNameObject { Id = "ou_leader", Name = new I18nName { ZhCn = "王强" } }],
    };

    [Fact]
    public async Task ListJobRequirements_ShouldProjectPlan_AndDropPeopleAndSalary()
    {
        var client = new Mock<Mud.Feishu.IFeishuTenantV1HireJobRequirement>();
        client
            .Setup(c => c.GetJobRequirementListAsync(It.IsAny<JobRequirementListQuery?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FeishuApiResult<GetJobRequirementListResult>
            {
                Code = 0,
                Data = new GetJobRequirementListResult { Items = [Sample()], HasMore = false },
            });

        var result = await CreateTools(client).ListJobRequirementsAsync(
            new Dictionary<string, object?> { ["job_id"] = "job-001" },
            CancellationToken.None);

        result.Error.Should().BeNull();
        var text = result.ToString();
        text.Should().Contain("高级后端（Q4 补员）").And.Contain("\"head_count\":3").And.Contain("研发中心");
        text.Should().Contain("JR2026-007").And.Contain("P5").And.Contain("P7");
        text.Should().Contain("job-001", "关联职位 ID 是需求与职位的连接点");

        text.Should().NotContain("ou_creator_should_not_leak", "创建人属个人信息，裁剪掉");
        text.Should().NotContain("李雷", "招聘负责人裁剪掉");
        text.Should().NotContain("韩梅梅", "用人经理裁剪掉");
        text.Should().NotContain("王强", "直属上级裁剪掉");
        text.Should().NotContain("30000", "薪资区间裁剪掉——与本工具目的无关，且属组织薪酬信息");
        text.Should().NotContain("50000");
        text.Should().NotContain("salary", "连字段名都不出现（白名单投影，不是黑名单删除）");

        client.Verify(
            c => c.GetJobRequirementListAsync(
                It.Is<JobRequirementListQuery?>(q => q != null && q.JobId == "job-001"),
                It.IsAny<CancellationToken>()),
            Times.Once,
            "job_id 是业务过滤，必须透传");
    }

    [Fact]
    public async Task ListJobRequirements_ShouldFailFast_WhenClientIsAbsent()
    {
        var result = await CreateTools().ListJobRequirementsAsync(new Dictionary<string, object?>(), CancellationToken.None);

        result.Error.Should().BeNull("软缺席走文本错误出口（既有执行器同口径）");
        result.ToString().Should().Contain("IFeishuTenantV1HireJobRequirement").And.Contain("hire.list_job_requirements");
    }
}
