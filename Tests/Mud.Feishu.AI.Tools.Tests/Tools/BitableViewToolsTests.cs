// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Moq;

using Mud.Feishu.AI.Tools.Internal;
using Mud.Feishu.DataModels;
using Mud.Feishu.DataModels.Bitable;

namespace Mud.Feishu.AI.Tools.Tests.Tools;

/// <summary>
/// R5 / F-11：<c>bitable.list_views</c> / <c>bitable.get_view</c> 断言。
/// </summary>
/// <remarks>
/// 除正常路径外，重点锁住<b>软依赖</b>语义：视图客户端缺席时，
/// 这两个工具<b>仍在册但执行期报可执行错误</b>，而不是"整域注册器缺席"。
/// 后者会连带拖垮 bitable 的 4 个既有只读工具 —— 那是引入新客户端时最容易踩的坑。
/// </remarks>
public class BitableViewToolsTests
{
    private static Dictionary<string, object?> Args(params (string Key, object? Value)[] pairs)
        => pairs.ToDictionary(static p => p.Key, static p => p.Value, StringComparer.Ordinal);

    private static BitableTools CreateTools(Mud.Feishu.IFeishuTenantV1BitableView? viewClient)
        => new(
            new Mock<Mud.Feishu.IFeishuTenantV1BitableAppTable>().Object,
            new Mock<Mud.Feishu.IFeishuTenantV1BitableField>().Object,
            new Mock<Mud.Feishu.IFeishuTenantV1BitableRecord>().Object,
            // 必须显式给足预算：默认 0 会让结果被截断到 1 字符，断言将失去意义。
            Microsoft.Extensions.Options.Options.Create(
                new FeishuAgentOptions { Instructions = "test", MaxToolResultLength = 8000 }),
            viewClient);

    [Fact]
    public async Task ListViews_ShouldProjectViewIdentity()
    {
        var client = new Mock<Mud.Feishu.IFeishuTenantV1BitableView>();
        client
            .Setup(c => c.GetViewsPageListAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(),
                It.IsAny<string?>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FeishuApiPageListTotalResult<AppViewDetailInfo>
            {
                Code = 0,
                Data = new ApiPageListTotalResult<AppViewDetailInfo>
                {
                    Total = 1,
                    Items =
                    [
                        new AppViewDetailInfo { ViewId = "veiw1", ViewName = "全部", ViewType = "grid" },
                    ],
                },
            });

        var result = await CreateTools(client.Object).ListViewsAsync(
            Args(("app_token", "bas1"), ("table_id", "tbl1")),
            CancellationToken.None);

        result.Text.Should().Contain("veiw1");
        result.Text.Should().Contain("全部");
        result.Text.Should().Contain("grid");
    }

    /// <summary>视图未命中必须返回<b>显式哨兵</b>，而不是空对象让模型猜（F-5 纪律）。</summary>
    [Fact]
    public async Task GetView_ShouldReportNotFound_InsteadOfEmptyObject()
    {
        var client = new Mock<Mud.Feishu.IFeishuTenantV1BitableView>();
        client
            .Setup(c => c.GetViewAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FeishuApiResult<GetViewResult> { Code = 0, Data = new GetViewResult() });

        var result = await CreateTools(client.Object).GetViewAsync(
            Args(("app_token", "bas1"), ("table_id", "tbl1"), ("view_id", "veiwX")),
            CancellationToken.None);

        result.Text.Should().Contain("\"found\":false");
        result.Text.Should().Contain("未找到");
        result.Text.Should().Contain("bitable.list_views", "未找到时应指引下一步（F-8）");
    }

    /// <summary>
    /// <b>软依赖语义</b>：视图客户端缺席时报错，但错误必须<b>指出宿主该启用什么</b>，
    /// 且不得影响同一执行器的其它工具（这里用"构造成功"间接证明）。
    /// </summary>
    [Fact]
    public async Task ViewTools_ShouldReportActionableError_WhenClientAbsent()
    {
        var tools = CreateTools(viewClient: null);

        var result = await tools.ListViewsAsync(
            Args(("app_token", "bas1"), ("table_id", "tbl1")),
            CancellationToken.None);

        result.Text.Should().Contain("IFeishuTenantV1BitableView", "必须点名缺哪个客户端");
        result.Text.Should().Contain("AddBitableApi", "必须给出可执行的宿主侧动作");
        result.Text.Should().Contain("不影响", "应明确告知其它 bitable 工具不受影响");
    }
}
