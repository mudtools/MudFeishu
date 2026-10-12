// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.DataModels.JobFamilies;
using Mud.Feishu.DataModels.JobLevel;
using Mud.Feishu.DataModels.JobTitles;
using Mud.Feishu.DataModels.WorkCites;

namespace Mud.Feishu.AI.Tools.Tests.Tools;

/// <summary>
/// F-1 用例：org 组织元数据（职务 / 职级 / 职务族 / 工作城市，共 8 个只读工具）执行链行为。
/// </summary>
/// <remarks>
/// 除常规投影/哨兵外，本组额外锁四件本域特有的事：
/// ① <b>页大小不下发</b>——执行器不传 <c>page_size</c>（走 SDK 声明默认），模型只控 <c>page_token</c>（DP-B1-0：页不进）；
/// ② <b><c>name</c> 是业务过滤可下发</b>——职级/职务族要把 <c>name</c> 透传到 SDK（与页大小不是一类东西）；
/// ③ <b>多语言名称折叠</b>为 <c>locale → value</c> 对象，不透出原始 <c>List&lt;I18nContent&gt;</c>（含冗余 id）；
/// ④ <b>软缺席按客户端隔离</b>——一个客户端缺席不得让整个 org 域消失（只影响用它那几条工具）。
/// </remarks>
public class OrgMetadataToolsTests
{
    private static OrgMetadataTools CreateTools(
        Mock<Mud.Feishu.IFeishuTenantV3JobTitle>? jobTitle = null,
        Mock<Mud.Feishu.IFeishuTenantV3JobLevel>? jobLevel = null,
        Mock<Mud.Feishu.IFeishuTenantV3JobFamilies>? jobFamilies = null,
        Mock<Mud.Feishu.IFeishuTenantV3WorkCity>? workCity = null)
        => new(
            Options.Create(new FeishuAgentOptions { Instructions = "test" }),
            jobTitle?.Object,
            jobLevel?.Object,
            jobFamilies?.Object,
            workCity?.Object);

    private static JobTitle SampleJobTitle() => new()
    {
        JobTitleId = "od-8ba9f0f2e2b04a4bbbbbbbbbbbbbbbbb",
        Name = "产品经理",
        Status = true,
        I18nName =
        [
            new I18nContent { Locale = "zh_cn", Value = "产品经理", Id = "723123123123123213" },
            new I18nContent { Locale = "en_us", Value = "Product Manager" },
        ],
    };

    private static JobLevelInfo SampleJobLevel() => new()
    {
        JobLevelId = "od-level-0001",
        Name = "P6",
        Description = "资深",
        Order = 6,
        Status = true,
        I18nName = [new I18nContent { Locale = "zh_cn", Value = "P6 资深" }],
    };

    private static JobFamilyInfo SampleJobFamily() => new()
    {
        JobFamilyId = "od-family-0001",
        Name = "研发",
        Description = "技术序列",
        ParentJobFamilyId = "od-family-root",
        Status = true,
        I18nName = [new I18nContent { Locale = "en_us", Value = "Engineering" }],
    };

    private static WorkCity SampleWorkCity() => new()
    {
        WorkCityId = "od-city-0001",
        Name = "上海",
        Status = true,
        I18nName = [new I18nContent { Locale = "en_us", Value = "Shanghai" }],
    };

    // ───────────────────────────── 职务 ─────────────────────────────

    [Fact]
    public async Task ListJobTitles_ShouldProjectFoldedI18n_AndKeepPageSizeUnset()
    {
        var client = new Mock<Mud.Feishu.IFeishuTenantV3JobTitle>();
        client
            .Setup(c => c.GetJobTitlesListAsync(It.IsAny<int?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FeishuApiPageListResult<JobTitle>
            {
                Code = 0,
                Data = new ApiPageListResult<JobTitle>
                {
                    Items = [SampleJobTitle()],
                    HasMore = true,
                    PageToken = "next-token",
                },
            });
        var tools = CreateTools(jobTitle: client);

        var result = await tools.ListJobTitlesAsync(
            new Dictionary<string, object?> { ["page_token"] = "t0" },
            CancellationToken.None);

        result.Error.Should().BeNull();
        var text = result.ToString();
        text.Should().Contain("产品经理");
        text.Should().Contain("\"zh_cn\":\"产品经理\"", "多语言名称必须折叠为 locale → value");
        text.Should().NotContain("723123123123123213", "折叠后不得带出 I18nContent 的冗余 id");
        text.Should().Contain("\"has_more\":true");
        text.Should().Contain("next-token");

        // DP-B1-0：页大小**不由模型控制**——执行器不传 page_size（SDK 声明的默认值生效：10），
        // 模型可见的只有 page_token（"Schema 里不得出现 page_size"由 PageSizeSchemaLeakContractGuards 全局锁）。
        client.Verify(
            c => c.GetJobTitlesListAsync(It.IsAny<int?>(), "t0", It.IsAny<CancellationToken>()),
            Times.Once,
            "必须真的调用下游且透传 page_token；page_size 的取值来自 SDK 默认而非入参");
    }

    [Fact]
    public async Task GetJobTitle_ShouldReturnNotFoundSentinel_WhenAbsent()
    {
        var client = new Mock<Mud.Feishu.IFeishuTenantV3JobTitle>();
        client
            .Setup(c => c.GetJobTitleByIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FeishuApiResult<JobTitleResult> { Code = 0, Data = new JobTitleResult() });

        var result = await CreateTools(jobTitle: client).GetJobTitleAsync(
            new Dictionary<string, object?> { ["job_title_id"] = "od-not-exist" },
            CancellationToken.None);

        result.Error.Should().BeNull();
        result.ToString().Should().Contain("\"found\":false", "查不到必须给显式哨兵（F-5）");
        result.ToString().Should().Contain("org", "哨兵要给出可执行的下一步（先列目录核对）");
    }

    // ──────────────────────── 职级（业务过滤可下发） ────────────────────────

    [Fact]
    public async Task ListJobLevels_ShouldForwardNameFilter_AndProjectOrder()
    {
        var client = new Mock<Mud.Feishu.IFeishuTenantV3JobLevel>();
        client
            .Setup(c => c.GetJobLevelListAsync(It.IsAny<string?>(), It.IsAny<int?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FeishuApiPageListResult<JobLevelInfo>
            {
                Code = 0,
                Data = new ApiPageListResult<JobLevelInfo> { Items = [SampleJobLevel()] },
            });

        var result = await CreateTools(jobLevel: client).ListJobLevelsAsync(
            new Dictionary<string, object?> { ["name"] = "P", ["page_token"] = "t1" },
            CancellationToken.None);

        result.Error.Should().BeNull();
        result.ToString().Should().Contain("\"order\":6", "职级必须带出排序序号（可用于排序，不得当作薪酬等级解读）");
        result.ToString().Should().Contain("P6 资深");

        client.Verify(
            c => c.GetJobLevelListAsync("P", It.IsAny<int?>(), "t1", It.IsAny<CancellationToken>()),
            Times.Once,
            "name 是 SDK 原生业务过滤——必须透传（与 page_size 不是一类东西）");
    }

    // ──────────────────────── 职务族（层级） ────────────────────────

    [Fact]
    public async Task ListJobFamilies_ShouldExposeParentChain()
    {
        var client = new Mock<Mud.Feishu.IFeishuTenantV3JobFamilies>();
        client
            .Setup(c => c.GetJobFamilesListAsync(It.IsAny<string?>(), It.IsAny<int?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FeishuApiPageListResult<JobFamilyInfo>
            {
                Code = 0,
                Data = new ApiPageListResult<JobFamilyInfo> { Items = [SampleJobFamily()] },
            });

        var result = await CreateTools(jobFamilies: client).ListJobFamiliesAsync(
            new Dictionary<string, object?>(),
            CancellationToken.None);

        result.Error.Should().BeNull();
        result.ToString().Should().Contain("\"parent_job_family_id\":\"od-family-root\"", "层级必须带出，模型不得臆造");
        result.ToString().Should().Contain("\"en_us\":\"Engineering\"");
    }

    // ──────────────────────── 工作城市 ────────────────────────

    [Fact]
    public async Task GetWorkCity_ShouldProjectDetail()
    {
        var client = new Mock<Mud.Feishu.IFeishuTenantV3WorkCity>();
        client
            .Setup(c => c.GetWorkCityByIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FeishuApiResult<WorkCityResult> { Code = 0, Data = new WorkCityResult { WorkCity = SampleWorkCity() } });

        var result = await CreateTools(workCity: client).GetWorkCityAsync(
            new Dictionary<string, object?> { ["work_city_id"] = "od-city-0001" },
            CancellationToken.None);

        result.Error.Should().BeNull();
        result.ToString().Should().Contain("上海").And.Contain("\"work_city_id\":\"od-city-0001\"");
    }

    // ──────────────────────── 软缺席隔离 ────────────────────────

    [Fact]
    public async Task MissingClient_ShouldOnlyDisableItsOwnTools_NotWholeDomain()
    {
        // 只注入工作城市客户端：本域其它三类应软缺席，工作城市仍可用。
        var tools = CreateTools(workCity: new Mock<Mud.Feishu.IFeishuTenantV3WorkCity>());

        var title = await tools.ListJobTitlesAsync(new Dictionary<string, object?>(), CancellationToken.None);
        var level = await tools.GetJobLevelAsync(
            new Dictionary<string, object?> { ["job_level_id"] = "od-x" },
            CancellationToken.None);

        title.Error.Should().BeNull("软缺席走文本错误出口（既有执行器同口径）");
        title.ToString().Should().Contain("IFeishuTenantV3JobTitle").And.Contain("org.list_job_titles");
        level.ToString().Should().Contain("IFeishuTenantV3JobLevel").And.Contain("org.get_job_level");
        level.ToString().Should().Contain("不影响 org 域其它工具", "缺席文案要说明隔离范围，避免模型以为整域不可用");
    }
}
