// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.DataModels.Departments;

namespace Mud.Feishu.AI.Tools.Tests.Tools;

/// <summary>
/// F-1 用例：contact 部门面补齐（<c>contact.get_department_parents</c> / <c>contact.search_departments</c>）。
/// </summary>
/// <remarks>
/// 锁三件事：① 上级链按 ID 透传并给出层级字段；② 搜索把 <c>query</c> 透传给 SDK（这是"按名字找部门"的关键）；
/// ③ 投影<b>不带出人员字段</b>（leader / HRBP）——"部门归谁管"要经 <c>contact.*</c> 的用户侧授权面，不由本工具顺手给出。
/// </remarks>
public class ContactDepartmentQueryToolsTests
{
    private static ContactDepartmentQueryTools CreateTools(Mock<Mud.Feishu.IFeishuTenantV3Departments>? client = null)
        => new(Options.Create(new FeishuAgentOptions { Instructions = "test" }), client?.Object);

    private static GetDepartmentInfo Sample(string id, string name, string? parentId) => new()
    {
        DepartmentId = id,
        OpenDepartmentId = $"od-{id}",
        Name = name,
        ParentDepartmentId = parentId,
        MemberCount = 42,
    };

    [Fact]
    public async Task GetDepartmentParents_ShouldProjectHierarchy()
    {
        var client = new Mock<Mud.Feishu.IFeishuTenantV3Departments>();
        client
            .Setup(c => c.GetParentDepartmentsByIdAsync(
                It.IsAny<string>(), It.IsAny<int?>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FeishuApiPageListResult<GetDepartmentInfo>
            {
                Code = 0,
                Data = new ApiPageListResult<GetDepartmentInfo> { Items = [Sample("d-parent", "研发中心", "d-root")] },
            });

        var result = await CreateTools(client).GetDepartmentParentsAsync(
            new Dictionary<string, object?> { ["department_id"] = "d-child" },
            CancellationToken.None);

        result.Error.Should().BeNull();
        result.ToString().Should().Contain("研发中心").And.Contain("\"parent_department_id\":\"d-root\"");

        client.Verify(
            c => c.GetParentDepartmentsByIdAsync("d-child", It.IsAny<int?>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task SearchDepartments_ShouldForwardQuery()
    {
        var client = new Mock<Mud.Feishu.IFeishuTenantV3Departments>();
        client
            .Setup(c => c.SearchDepartmentsAsync(
                It.IsAny<SearchDepartmentRequest>(), It.IsAny<int?>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FeishuApiPageListResult<GetDepartmentInfo>
            {
                Code = 0,
                Data = new ApiPageListResult<GetDepartmentInfo> { Items = [Sample("d-1", "研发一部", "d-parent")] },
            });

        var result = await CreateTools(client).SearchDepartmentsAsync(
            new Dictionary<string, object?> { ["query"] = "研发" },
            CancellationToken.None);

        result.Error.Should().BeNull();
        result.ToString().Should().Contain("研发一部");

        client.Verify(
            c => c.SearchDepartmentsAsync(
                It.Is<SearchDepartmentRequest>(r => r.Query == "研发"),
                It.IsAny<int?>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()),
            Times.Once,
            "query 必须透传——否则模型拿不到 department_id，后续取成员/上级链全断");
    }

    [Fact]
    public async Task Projections_ShouldNotCarryPeopleFields()
    {
        var client = new Mock<Mud.Feishu.IFeishuTenantV3Departments>();
        client
            .Setup(c => c.GetParentDepartmentsByIdAsync(
                It.IsAny<string>(), It.IsAny<int?>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FeishuApiPageListResult<GetDepartmentInfo>
            {
                Code = 0,
                Data = new ApiPageListResult<GetDepartmentInfo> { Items = [Sample("d-parent", "研发中心", null)] },
            });

        var result = await CreateTools(client).GetDepartmentParentsAsync(
            new Dictionary<string, object?> { ["department_id"] = "d-1" },
            CancellationToken.None);

        var text = result.ToString();
        text.Should().NotContain("leader", "部门投影不得顺手带出负责人字段（非 PII 边界）");
        text.Should().NotContain("hrbp");
    }
}
