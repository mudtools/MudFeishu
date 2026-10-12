// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.DataModels.MDM;

namespace Mud.Feishu.AI.Tools.Tests.Tools;

/// <summary>
/// F-1 首个补域用例：MDM 国家/地区（<c>mdm.get_countries</c>）执行链行为。
/// </summary>
/// <remarks>
/// <para>
/// <b>为什么单独成文件</b>：新域除"装配可解析"外，必须验证三件事——① 投影只回填白名单字段；
/// ② 越界批量<b>本地</b>拒绝且零下游调用；③ 客户端软缺席时给出可执行错误而不是空引用。
/// </para>
/// </remarks>
public class CountryRegionToolsTests
{
    private static CountryRegionTools CreateTools(Mock<Mud.Feishu.IFeishuTenantV3MDMCountryRegion>? client = null)
        => new(Options.Create(new FeishuAgentOptions { Instructions = "test" }), client?.Object);

    private static IReadOnlyDictionary<string, object?> Args() => new Dictionary<string, object?>
    {
        ["ids"] = new[] { "MDCT00000001" },
        ["fields"] = new[] { "name", "alpha_2_code" },
        ["languages"] = new[] { "zh-CN" },
    };

    private static Mock<Mud.Feishu.IFeishuTenantV3MDMCountryRegion> ClientWith(GetBatchCountryRegionResult data)
    {
        var client = new Mock<Mud.Feishu.IFeishuTenantV3MDMCountryRegion>();
        client
            .Setup(c => c.GetBatchCountryRegionAsync(
                It.IsAny<string[]>(), It.IsAny<string[]>(), It.IsAny<string[]>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FeishuApiResult<GetBatchCountryRegionResult> { Code = 0, Data = data });
        return client;
    }

    [Fact]
    public async Task GetCountries_ShouldProjectWhitelistedFields()
    {
        var client = ClientWith(new GetBatchCountryRegionResult
        {
            Data =
            [
                new MdmCountryRegion
                {
                    MdmCode = "MDCT00000001",
                    Name = new MdmI18nString { Value = "安道尔", ReturnLanguage = "zh-CN" },
                    FullName = new MdmI18nString { Value = "安道尔公国" },
                    Alpha2Code = "AD",
                    Alpha3Code = "AND",
                    NumericCode = "20",
                    GlobalCode = "+376",
                    Continents = new MdmEnumValue { Value = "2" },
                    Status = "1",
                },
            ],
        });
        var tools = CreateTools(client);

        var result = await tools.GetCountriesAsync(Args(), CancellationToken.None);

        result.Error.Should().BeNull("合法入参 + 客户端在场必须成功");
        var text = result.ToString();
        text.Should().Contain("MDCT00000001");
        text.Should().Contain("安道尔");
        text.Should().Contain("\"total\":1", "投影必须给出条目数");
    }

    [Fact]
    public async Task GetCountries_ShouldReturnNotFoundSentinel_WhenResultIsEmpty()
    {
        var tools = CreateTools(ClientWith(new GetBatchCountryRegionResult { Data = [] }));

        var result = await tools.GetCountriesAsync(Args(), CancellationToken.None);

        result.Error.Should().BeNull();
        var text = result.ToString();
        text.Should().Contain("\"found\":false", "空结果必须给显式哨兵，而不是让模型猜（F-5）");
        text.Should().Contain("未查询到任何国家/地区主数据");
    }

    [Fact]
    public async Task GetCountries_ShouldRejectOutOfRangeBatch_WithoutCallingDownstream()
    {
        var client = ClientWith(new GetBatchCountryRegionResult { Data = [] });
        var tools = CreateTools(client);
        var tooMany = Enumerable.Range(0, 101).Select(i => $"MDCT{i:D8}").ToArray();

        var result = await tools.GetCountriesAsync(
            new Dictionary<string, object?> { ["ids"] = tooMany, ["fields"] = new[] { "name" }, ["languages"] = new[] { "zh-CN" } },
            CancellationToken.None);

        result.Error.Should().NotBeNull("越界批量必须在本地拒绝（F-8：不消耗下游调用）");
        result.Error!.Subtype.Should().Be("invalid_args");
        client.Verify(
            c => c.GetBatchCountryRegionAsync(
                It.IsAny<string[]>(), It.IsAny<string[]>(), It.IsAny<string[]>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task GetCountries_ShouldFailFast_WhenClientIsAbsent()
    {
        // 软缺席：客户端不在场时给出**可执行错误文本**（而不是异常/空引用），并点名缺失的依赖。
        // 与"本地参数校验"的差别是有意的：校验走结构化 Error（invalid_args，模型可改参数重试），
        // 软缺席走文本出口（与 BoardTools / MailTools / DocxSheetsDriveWriteTools 同口径）——
        // 它不是"参数错了"，而是"该能力当前不可用"。
        var tools = CreateTools();

        var result = await tools.GetCountriesAsync(Args(), CancellationToken.None);

        result.Error.Should().BeNull("软缺席走文本错误出口（既有执行器同口径），不是结构化参数错误");
        result.ToString().Should().Contain("IFeishuTenantV3MDMCountryRegion",
            "错误必须点名缺失的依赖，宿主才知道要启用哪个 API");
        result.ToString().Should().Contain("mdm.get_countries", "错误必须点明是哪个工具不可用");
    }
}
