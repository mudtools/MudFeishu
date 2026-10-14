// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.AI.Agent.Tests.Tools;

/// <summary>
/// 产品业务码读数载体（<see cref="ToolApiFailure"/>）的中立语义用例：中立层不持有码表，
/// 产品读数缺席时<b>回落中性分类</b>（不猜语义）。
/// </summary>
/// <remarks>
/// <b>为什么这些用例在本工程</b>：本工程只引用 <c>Mud.AI.Agent</c> / <c>Mud.AI.Abstractions</c>
/// （无 Mud 组件、无飞书工程），能过即证明读数路径不依赖任何产品码表。产品侧证据
/// （飞书码表 → 读数 → 载荷）留在飞书侧用例
/// （<c>ToolErrorContractGuards.ApiCodeReading_ShouldTravelFromReaderToPayload</c>）。
/// </remarks>
public class ToolApiFailureTests
{
    /// <summary>未给读数时的中性回落：<c>api/upstream_error</c>，且不带任何候选（不猜语义）。</summary>
    [Fact]
    public void Unclassified_ShouldBeNeutralApiUpstreamError()
    {
        ToolApiFailure.Unclassified.Category.Should().Be(ToolErrorCategory.Api);
        ToolApiFailure.Unclassified.Subtype.Should().Be(ToolErrorSubtype.UpstreamError);
        ToolApiFailure.Unclassified.Suggestions.Should().BeNull();
    }

    /// <summary>
    /// 产品未给读数 ⇒ 解包结果只携带 code（进载荷 <c>api_code</c>），分类交由中立默认值。
    /// </summary>
    [Fact]
    public void Fail_WithoutReading_ShouldCarryNoFailure()
    {
        var outcome = ToolApiOutcome<string>.Fail("下游失败", 999999);

        outcome.Ok.Should().BeFalse();
        outcome.Code.Should().Be(999999);
        outcome.Failure.Should().BeNull("产品未给读数时不得编造分类（回落 api/upstream_error）");
    }

    /// <summary>产品给了读数 ⇒ 原样携带（中立层只消费，不改写产品的码级事实）。</summary>
    [Fact]
    public void Fail_WithReading_ShouldCarryItVerbatim()
    {
        var reading = new ToolApiFailure(ToolErrorCategory.Authorization, ToolErrorSubtype.AuthorizationDenied, ["候选"]);
        var outcome = ToolApiOutcome<string>.Fail("无权限", 999999, reading);

        outcome.Failure.Should().BeSameAs(reading);
        outcome.Failure!.Suggestions.Should().ContainSingle().Which.Should().Be("候选");
    }
}
