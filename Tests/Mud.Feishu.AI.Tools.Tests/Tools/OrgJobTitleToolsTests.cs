// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.DataModels.JobTitles;

namespace Mud.Feishu.AI.Tools.Tests.Tools;

/// <summary>
/// F-1 用例：org 组织元数据（<c>org.list_job_titles</c> / <c>org.get_job_title</c>）执行链行为。
/// </summary>
/// <remarks>
/// 除常规四条（投影 / 空哨兵 / 软缺席 / 折叠）外，本组额外锁两件本域特有的事：
/// ① <b>页大小不下发</b>——执行器不传 <c>page_size</c>（走 SDK 声明默认），模型只控 <c>page_token</c>（DP-B1-0：页不进）；
/// ② <b>多语言名称折叠</b>为 <c>locale → value</c> 对象，不透出原始 <c>List&lt;I18nContent&gt;</c>（含冗余 id）。
/// </remarks>
public class OrgJobTitleToolsTests
{
    private static OrgJobTitleTools CreateTools(Mock<Mud.Feishu.IFeishuTenantV3JobTitle>? client = null)
        => new(Options.Create(new FeishuAgentOptions { Instructions = "test" }), client?.Object);

    private static Mock<Mud.Feishu.IFeishuTenantV3JobTitle> ClientWith(
        ApiPageListResult<JobTitle>? list = null,
        JobTitleResult? detail = null)
    {
        var client = new Mock<Mud.Feishu.IFeishuTenantV3JobTitle>();
        if (list is not null)
        {
            client
                .Setup(c => c.GetJobTitlesListAsync(It.IsAny<int?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new FeishuApiPageListResult<JobTitle> { Code = 0, Data = list });
        }

        if (detail is not null)
        {
            client
                .Setup(c => c.GetJobTitleByIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new FeishuApiResult<JobTitleResult> { Code = 0, Data = detail });
        }

        return client;
    }

    private static JobTitle Sample() => new()
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

    [Fact]
    public async Task ListJobTitles_ShouldProjectFoldedI18n_AndKeepPageSizeUnset()
    {
        var client = ClientWith(list: new ApiPageListResult<JobTitle>
        {
            Items = [Sample()],
            HasMore = true,
            PageToken = "next-token",
        });
        var tools = CreateTools(client);

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
        var tools = CreateTools(ClientWith(detail: new JobTitleResult()));

        var result = await tools.GetJobTitleAsync(
            new Dictionary<string, object?> { ["job_title_id"] = "od-not-exist" },
            CancellationToken.None);

        result.Error.Should().BeNull();
        result.ToString().Should().Contain("\"found\":false", "查不到必须给显式哨兵（F-5）");
        result.ToString().Should().Contain("org.list_job_titles", "哨兵要给出可执行的下一步（先列目录核对）");
    }

    [Fact]
    public async Task GetJobTitle_ShouldProjectDetail()
    {
        var tools = CreateTools(ClientWith(detail: new JobTitleResult { JobTitle = Sample() }));

        var result = await tools.GetJobTitleAsync(
            new Dictionary<string, object?> { ["job_title_id"] = "od-8ba9f0f2e2b04a4bbbbbbbbbbbbbbbbb" },
            CancellationToken.None);

        result.Error.Should().BeNull();
        result.ToString().Should().Contain("Product Manager");
        result.ToString().Should().Contain("\"status\":true");
    }

    [Fact]
    public async Task BothTools_ShouldFailFast_WhenClientIsAbsent()
    {
        var tools = CreateTools();

        var list = await tools.ListJobTitlesAsync(new Dictionary<string, object?>(), CancellationToken.None);
        var detail = await tools.GetJobTitleAsync(
            new Dictionary<string, object?> { ["job_title_id"] = "od-x" },
            CancellationToken.None);

        list.Error.Should().BeNull("软缺席走文本错误出口（既有执行器同口径）");
        list.ToString().Should().Contain("IFeishuTenantV3JobTitle").And.Contain("org.list_job_titles");
        detail.ToString().Should().Contain("IFeishuTenantV3JobTitle").And.Contain("org.get_job_title");
        detail.Error.Should().BeNull();
    }
}
