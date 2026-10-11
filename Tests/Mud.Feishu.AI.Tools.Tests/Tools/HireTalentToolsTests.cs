// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.DataModels.Hire;

namespace Mud.Feishu.AI.Tools.Tests.Tools;

/// <summary>
/// hire 候选人用例（<c>hire.list_talents</c> / <c>hire.get_talent</c>，<b>PII 出口</b>）。
/// </summary>
/// <remarks>
/// 本组最重要的不是"能不能取到数据"，而是<b>取不到不该取的数据</b>：
/// ① 列表只给定位字段（无手机/邮箱）；
/// ② 详情给联系方式，但身份证号 / 档案类字段（学历、经历…）一律不得出现——
///    投影是白名单实现的，本用例就是这条纪律的机械证据。
/// </remarks>
public class HireTalentToolsTests
{
    private static HireTalentTools CreateTools(Mock<Mud.Feishu.IFeishuTenantV1HireCandidate>? client = null)
        => new(Options.Create(new FeishuAgentOptions { Instructions = "test" }), client?.Object);

    private static Talent Sample() => new()
    {
        Id = "talent-001",
        IsOnboarded = false,
        BasicInfo = new TalentBasicInfo
        {
            Name = "张伟",
            Mobile = "13800000000",
            Email = "zhangwei@example.com",
            ExperienceYears = 5,

            // ⚠️ 下面两项**必须**被投影丢弃：身份证号属敏感标识，档案类字段与本工具目的无关。
            IdentificationNumber = "110101199001011234",
            IdentificationType = 1,
        },
    };

    [Fact]
    public async Task ListTalents_ShouldReturnLocatorFieldsOnly()
    {
        var client = new Mock<Mud.Feishu.IFeishuTenantV1HireCandidate>();
        client
            .Setup(c => c.GetTalentListAsync(It.IsAny<TalentListQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FeishuApiResult<GetTalentListResult>
            {
                Code = 0,
                Data = new GetTalentListResult { Items = [Sample()], HasMore = true, PageToken = "next" },
            });

        var result = await CreateTools(client).ListTalentsAsync(
            new Dictionary<string, object?> { ["keyword"] = "张伟" },
            CancellationToken.None);

        result.Error.Should().BeNull();
        var text = result.ToString();
        text.Should().Contain("张伟").And.Contain("talent-001");
        text.Should().NotContain("13800000000", "列表不带联系方式——翻页找人是索引，不是导出通讯录");
        text.Should().NotContain("110101199001011234", "身份证号任何形态都不得返回");
        text.Should().NotContain("identification");

        client.Verify(
            c => c.GetTalentListAsync(It.Is<TalentListQuery>(q => q.Keyword == "张伟"), It.IsAny<CancellationToken>()),
            Times.Once,
            "keyword 是业务过滤，必须透传");
    }

    [Fact]
    public async Task GetTalent_ShouldReturnContactButDropIdentityAndArchive()
    {
        var client = new Mock<Mud.Feishu.IFeishuTenantV1HireCandidate>();
        client
            .Setup(c => c.GetTalentAsync(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FeishuApiResult<GetTalentResult>
            {
                Code = 0,
                Data = new GetTalentResult { Talent = Sample() },
            });

        var result = await CreateTools(client).GetTalentAsync(
            new Dictionary<string, object?> { ["talent_id"] = "talent-001" },
            CancellationToken.None);

        var text = result.ToString();
        text.Should().Contain("13800000000", "「联系候选人」是正当用途，联系方式在详情里返回");
        text.Should().Contain("zhangwei@example.com").And.Contain("\"experience_years\":5");
        text.Should().NotContain("110101199001011234", "身份证号必须被投影丢弃");
        text.Should().NotContain("education", "学历/经历等档案字段不返回");
        text.Should().NotContain("career");
    }

    [Fact]
    public async Task GetTalent_ShouldGiveSentinel_WhenMissing()
    {
        var client = new Mock<Mud.Feishu.IFeishuTenantV1HireCandidate>();
        client
            .Setup(c => c.GetTalentAsync(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FeishuApiResult<GetTalentResult> { Code = 0, Data = new GetTalentResult() });

        var result = await CreateTools(client).GetTalentAsync(
            new Dictionary<string, object?> { ["talent_id"] = "talent-nope" },
            CancellationToken.None);

        result.ToString().Should().Contain("\"found\":false", "查不到必须给显式哨兵（F-5）");
    }

    [Fact]
    public async Task BothTools_ShouldFailFast_WhenClientIsAbsent()
    {
        var tools = CreateTools();

        var list = await tools.ListTalentsAsync(new Dictionary<string, object?>(), CancellationToken.None);
        var detail = await tools.GetTalentAsync(
            new Dictionary<string, object?> { ["talent_id"] = "t" },
            CancellationToken.None);

        list.Error.Should().BeNull("软缺席走文本错误出口（既有执行器同口径）");
        list.ToString().Should().Contain("IFeishuTenantV1HireCandidate").And.Contain("hire.list_talents");
        detail.ToString().Should().Contain("IFeishuTenantV1HireCandidate").And.Contain("hire.get_talent");
    }
}
