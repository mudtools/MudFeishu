// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using System.Text.Json.Nodes;
using Mud.Feishu.DataModels.Application;

namespace Mud.Feishu.AI.Tools.Tests.Tools;

/// <summary>
/// Application 飞书应用管理域级测试（专题一 B1 第二个补域，SOP 第 8 步）。
/// </summary>
/// <remarks>
/// <para>
/// 本域最要紧的一条是<b>出站净化</b>：应用对象自带 <c>secret</c>（应用凭据）、
/// <c>encryption</c>（加密密钥 + 校验令牌）与 <c>owner</c>（负责人姓名 / 邮箱 / 手机号）。
/// 测试用"脏数据"（把敏感字段全填上）来证明投影确实把它们丢掉了——
/// 只测"正常数据能出字段面"是测不出泄露的。
/// </para>
/// <para>
/// policy 拒绝零调用与审计三类投递由 <c>ToolPolicyContractGuards</c> /
/// <c>FeishuToolBindingAuditTests</c> 通用覆盖，本类不重复。
/// </para>
/// </remarks>
public class ApplicationToolsTests
{
    private static IReadOnlyDictionary<string, object?> Args(params (string Key, object? Value)[] items)
        => items.ToDictionary(p => p.Key, p => p.Value);

    private static ApplicationTools CreateTools(
        Mock<Mud.Feishu.IFeishuTenantV6ApplicationAdmin>? admin = null,
        Mock<Mud.Feishu.IFeishuTenantV6ApplicationAppInfo>? appInfo = null,
        Mock<Mud.Feishu.IFeishuTenantV6Application>? application = null,
        Mock<Mud.Feishu.IFeishuTenantV6ApplicationScope>? scope = null,
        Mock<Mud.Feishu.IFeishuTenantV6ApplicationAppVersion>? appVersion = null)
        => new(
            Options.Create(new FeishuAgentOptions { Instructions = "test" }),
            admin?.Object,
            appInfo?.Object,
            application?.Object,
            scope?.Object,
            appVersion?.Object);

    private static JsonObject Payload(ToolResult result)
        => JsonNode.Parse(result.ToString())!.AsObject();

    private static FeishuApiResult<T> Ok<T>(T data)
        where T : class
        => new() { Code = 0, Data = data };

    /// <summary>带"脏数据"的应用对象：敏感字段全部填上，用于验证投影真的丢掉了它们。</summary>
    private static ApplicationInfo DirtyApp()
        => new()
        {
            AppId = "cli_1",
            AppName = "审批助手",
            Description = "内部审批机器人",
            Status = 1,
            SceneType = 0,
            PaymentType = 0,
            CreateSource = "create_from_developer_console",
            PrimaryLanguage = "zh_cn",
            OnlineVersionId = "o_1",
            UnauditVersionId = "u_1",
            BackHomeUrl = "https://example/back",
            MobileDefaultAbility = "gadget",
            PcDefaultAbility = "gadget",
            AllowRefreshToken = true,
            Scopes = [new AppScope { Scope = "im:message", Level = 1 }],

            // ⚠️ 下列字段都必须被投影丢掉（本测试逐个断言它们不出现在出站 JSON 里）。
            Secret = "s3cr3t-app-secret",
            Encryption = new ApplicationEncryptionConfig { EncryptionKey = "enc-key", VerificationToken = "vt" },
            Security = new ApplicationSecurityConfig { RedirectUrls = ["https://evil/cb"] },
            CreatorId = "ou_creator",
            Owner = new ApplicationOwner
            {
                OwnerId = "ou_owner",
                Name = "张三",
                Email = "zhangsan@example.com",
                Phone = "13800000000",
            },
        };

    // ─────────────────────────── happy path ───────────────────────────

    [Fact]
    public async Task ListApps_Should_Project_Summary_Without_Secret_Or_Owner_Contact()
    {
        var admin = new Mock<Mud.Feishu.IFeishuTenantV6ApplicationAdmin>();
        admin.Setup(c => c.GetApplicationsPageListAsync(
                It.IsAny<string>(), It.IsAny<int?>(), It.IsAny<string?>(), It.IsAny<int?>(),
                It.IsAny<int?>(), It.IsAny<int?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Ok(new GetApplicationsResult
            {
                AppList = [DirtyApp()],
                HasMore = false,
                TotalCount = 1,
            }));

        var payload = Payload(await CreateTools(admin: admin).ListAppsAsync(Args(), CancellationToken.None));

        var item = payload["items"]!.AsArray()[0]!.AsObject();
        item["app_id"]!.GetValue<string>().Should().Be("cli_1");
        item["app_name"]!.GetValue<string>().Should().Be("审批助手");
        item["online_version_id"]!.GetValue<string>().Should().Be("o_1");
        payload["total"]!.GetValue<int>().Should().Be(1);

        AssertNoSensitiveFields(item);
    }

    [Fact]
    public async Task ListApps_FetchAll_Should_Aggregate_Pages()
    {
        var calls = 0;
        var admin = new Mock<Mud.Feishu.IFeishuTenantV6ApplicationAdmin>();
        admin.Setup(c => c.GetApplicationsPageListAsync(
                It.IsAny<string>(), It.IsAny<int?>(), It.IsAny<string?>(), It.IsAny<int?>(),
                It.IsAny<int?>(), It.IsAny<int?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string _, int? __, string? pageToken, int? ___, int? ____, int? _____, string? ______, CancellationToken _) =>
            {
                calls++;
                var app = DirtyApp();
                app.AppId = pageToken is null ? "cli_1" : "cli_2";
                return Ok(new GetApplicationsResult
                {
                    AppList = [app],
                    HasMore = pageToken is null,
                    PageToken = pageToken is null ? "t2" : null,
                });
            });

        var payload = Payload(await CreateTools(admin: admin).ListAppsAsync(
            Args(("fetch_all", true)), CancellationToken.None));

        calls.Should().Be(2, "fetch_all 必须循环到 has_more=false（页状态显式重载的真实消费者）");
        payload["items"]!.AsArray().Count.Should().Be(2);
        payload["truncated"]!.GetValue<bool>().Should().BeFalse();
    }

    [Fact]
    public async Task GetApp_Should_Return_Detail_With_Owner_Id_But_No_Secrets()
    {
        var appInfo = new Mock<Mud.Feishu.IFeishuTenantV6ApplicationAppInfo>();
        appInfo.Setup(c => c.GetApplicationAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Ok(new GetApplicationResult { App = DirtyApp() }));

        var payload = Payload(await CreateTools(appInfo: appInfo).GetAppAsync(
            Args(("app_id", "cli_1")), CancellationToken.None));

        payload["found"]!.GetValue<bool>().Should().BeTrue();
        payload["app_id"]!.GetValue<string>().Should().Be("cli_1");
        payload["owner_id"]!.GetValue<string>().Should().Be("ou_owner", "只保留标识符，联系方式不出站");
        payload["scope_names"]!.AsArray()[0]!.GetValue<string>().Should().Be("im:message");

        AssertNoSensitiveFields(payload);
    }

    [Fact]
    public async Task GetAppVisibility_Should_Project_Identifiers_And_Explain_Two_Lists()
    {
        var admin = new Mock<Mud.Feishu.IFeishuTenantV6ApplicationAdmin>();
        admin.Setup(c => c.GetAppVisibilityAsync(
                It.IsAny<string>(), It.IsAny<int?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Ok(new GetAppVisibilityResult
            {
                IsVisibleToAll = 0,
                Departments = [new AppVisibilityIdItem { Id = "od_1" }],
                InvisibleDepartments = [new AppVisibilityIdItem { Id = "od_2" }],
                Users = [new AppVisibilityUser { UserId = "u_1", OpenId = "ou_1" }],
            }));

        var payload = Payload(await CreateTools(admin: admin).GetAppVisibilityAsync(
            Args(("app_id", "cli_1")), CancellationToken.None));

        payload["is_visible_to_all"]!.GetValue<int>().Should().Be(0);
        payload["departments"]!.AsArray()[0]!.GetValue<string>().Should().Be("od_1");
        payload["invisible_departments"]!.AsArray()[0]!.GetValue<string>().Should().Be("od_2");
        payload["users"]!.AsArray()[0]!["open_id"]!.GetValue<string>().Should().Be("ou_1");
        payload["notice"]!.GetValue<string>().Should().Contain("不可见优先",
            "提示两条清单的生效关系，避免模型把不可见清单误读成'无权限'");
    }

    [Fact]
    public async Task ListCollaborators_Should_Explain_Empty_Result()
    {
        var application = new Mock<Mud.Feishu.IFeishuTenantV6Application>();
        application.Setup(c => c.GetApplicationCollaboratorsAsync(
                It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Ok(new GetApplicationCollaboratorsResult { Collaborators = [] }));

        var payload = Payload(await CreateTools(application: application).ListCollaboratorsAsync(
            Args(("app_id", "cli_1")), CancellationToken.None));

        payload["found"]!.GetValue<bool>().Should().BeFalse();
        payload["message"]!.GetValue<string>().Should().Contain("所有者",
            "空协作者是有意义的结论（只有所有者能进后台），哨兵要说清");
    }

    [Fact]
    public async Task GetScopes_Should_Map_Grant_Status_To_Text()
    {
        var scope = new Mock<Mud.Feishu.IFeishuTenantV6ApplicationScope>();
        scope.Setup(c => c.GetScopesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Ok(new GetScopesResult
            {
                Scopes =
                [
                    new ScopeGrantStatus { ScopeName = "im:message", GrantStatus = 1, ScopeType = "tenant" },
                    new ScopeGrantStatus { ScopeName = "contact:user.base:readonly", GrantStatus = 2 },
                    new ScopeGrantStatus { ScopeName = "unknown:scope", GrantStatus = 99 },
                ],
            }));

        var payload = Payload(await CreateTools(scope: scope).GetScopesAsync(Args(), CancellationToken.None));

        var items = payload["items"]!.AsArray();
        items[0]!["grant_status_text"]!.GetValue<string>().Should().Be("已授权");
        items[1]!["grant_status_text"]!.GetValue<string>().Should().Be("已申请待授权");
        items[2]!["grant_status_text"]!.Should().BeNull("未知状态不猜文案，保留数字由模型如实报告");
    }

    [Fact]
    public async Task ListAppVersions_Should_Pass_Through_Paging_Envelope()
    {
        var appVersion = new Mock<Mud.Feishu.IFeishuTenantV6ApplicationAppVersion>();
        appVersion.Setup(c => c.GetAppVersionsPageListAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int?>(), It.IsAny<string?>(),
                It.IsAny<int?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FeishuApiPageListResult<AppVersionInfo>
            {
                Code = 0,
                Data = new ApiPageListResult<AppVersionInfo>
                {
                    HasMore = false,
                    Items =
                    [
                        new AppVersionInfo
                        {
                            AppId = "cli_1",
                            VersionId = "o_1",
                            Version = "1.2.0",
                            AppName = "审批助手",
                            Status = 1,
                            CreateTime = "2026-10-01T10:00:00Z",
                            PublishTime = "2026-10-02T10:00:00Z",
                        },
                    ],
                },
            });

        var payload = Payload(await CreateTools(appVersion: appVersion).ListAppVersionsAsync(
            Args(("app_id", "cli_1")), CancellationToken.None));

        var item = payload["items"]!.AsArray()[0]!.AsObject();
        item["version_id"]!.GetValue<string>().Should().Be("o_1");
        item["version"]!.GetValue<string>().Should().Be("1.2.0");
        payload["has_more"]!.GetValue<bool>().Should().BeFalse();
    }

    // ─────────────────────────── 软缺席 ───────────────────────────

    [Fact]
    public async Task AllTools_Should_SoftlyDegrade_When_Clients_Are_Absent()
    {
        var tools = CreateTools();

        var results = new[]
        {
            await tools.ListAppsAsync(Args(), CancellationToken.None),
            await tools.GetAppAsync(Args(("app_id", "cli_1")), CancellationToken.None),
            await tools.GetAppVisibilityAsync(Args(("app_id", "cli_1")), CancellationToken.None),
            await tools.ListCollaboratorsAsync(Args(("app_id", "cli_1")), CancellationToken.None),
            await tools.GetScopesAsync(Args(), CancellationToken.None),
            await tools.ListAppVersionsAsync(Args(("app_id", "cli_1")), CancellationToken.None),
            await tools.GetAppVersionAsync(Args(("app_id", "cli_1"), ("version_id", "o_1")), CancellationToken.None),
        };

        foreach (var result in results)
        {
            // 软缺席走文本形态的结构化错误（不进 result.Error——那是宿主侧异常语义）。
            result.Text.Should().NotBeNull("软缺席必须给可执行说明，而不是抛异常");
            result.Text.Should().Contain("未注册", "文案要说清是宿主未启用该 API，而不是模型参数错");
            result.Text.Should().Contain("tool_error", "必须是结构化错误（第一行紧凑 JSON）");
        }
    }

    /// <summary>
    /// 出站净化的公共断言：凭据 / 加密 / 安全配置 / 创建人 / 负责人联系方式一个都不许出现。
    /// </summary>
    private static void AssertNoSensitiveFields(JsonObject payload)
    {
        foreach (var key in new[]
        {
            "secret", "encryption", "security", "event", "callback", "callback_info",
            "redirect_urls", "creator_id", "owner",
        })
        {
            payload.ContainsKey(key).Should().BeFalse($"出站 JSON 不得含 {key}（应用凭据 / 安全配置 / 身份信息）");
        }

        // 兜底：连值都不该出现（防"换个键名照样泄露"）。
        var text = payload.ToJsonString();
        text.Should().NotContain("s3cr3t-app-secret").And.NotContain("zhangsan@example.com").And.NotContain("13800000000");
    }
}
