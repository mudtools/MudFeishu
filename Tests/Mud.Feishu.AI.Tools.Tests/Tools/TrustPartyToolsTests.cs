// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using System.Text.Json.Nodes;
using Mud.Feishu.DataModels.TrustParty;

namespace Mud.Feishu.AI.Tools.Tests.Tools;

/// <summary>
/// TrustParty「跨组织互通」域级测试（专题一 B1 收官第五域，SOP 第 8 步）。
/// </summary>
/// <remarks>
/// <para>
/// 本域的边界比前面四域更硬，故测试的**重点不在"能不能取到"，而在"什么不许出去"**：
/// <list type="number">
/// <item><b>PII 净化</b>：外部成员的手机号 / 工号 / 自定义属性 / 头像 / 在职状态一个都不得出站
/// （<see cref="GetCollaborationUser_Should_Strip_SensitiveFields"/>）；</item>
/// <item><b>两个接口面不混</b>：互通面（<c>trust_party</c>）与 directory 面（<c>directory</c>）的
/// <c>target_tenant_key</c> 来源不同，空结果的提示文案必须讲清是哪一面；</item>
/// <item><b>统一分页</b>：八份结果都不继承 <c>ApiPageListResult</c>，聚合必须真的翻页
/// （页状态显式重载，<c>has_more</c> 为 <c>bool?</c>）。</item>
/// </list>
/// </para>
/// <para>
/// policy 拒绝零调用与审计三类投递由 <c>ToolPolicyContractGuards</c> / <c>FeishuToolBindingAuditTests</c>
/// 通用覆盖，本类不重复。
/// </para>
/// </remarks>
public class TrustPartyToolsTests
{
    private static IReadOnlyDictionary<string, object?> Args(params (string Key, object? Value)[] items)
        => items.ToDictionary(p => p.Key, p => p.Value);

    private static TrustPartyTools CreateTools(
        Mock<Mud.Feishu.IFeishuTenantV1TrustPartyCollaborationTenant>? collaborationTenant = null,
        Mock<Mud.Feishu.IFeishuTenantV1DirectoryShareEntity>? shareEntity = null,
        Mock<Mud.Feishu.IFeishuTenantV1DirectoryCollaborationTenant>? directoryTenant = null,
        Mock<Mud.Feishu.IFeishuTenantV1DirectoryCollaborationRule>? collaborationRule = null)
        => new(
            Options.Create(new FeishuAgentOptions { Instructions = "test" }),
            collaborationTenant?.Object,
            shareEntity?.Object,
            directoryTenant?.Object,
            collaborationRule?.Object);

    private static JsonObject Payload(FeishuToolResult result)
        => JsonNode.Parse(result.ToString())!.AsObject();

    // FeishuApiResult<T> 带 where T : class 约束（FeishuApiOutcome<T> 承载 T? 数据）。
    private static FeishuApiResult<T> Ok<T>(T data) where T : class => new() { Code = 0, Data = data };

    // ─────────────────── 1. 关联组织列表（互通面第一跳） ───────────────────

    /// <summary>fetch_all 必须真的翻页，且投影走白名单（i18n 多语言副本不铺开）。</summary>
    [Fact]
    public async Task ListCollaborationTenants_FetchAll_Should_Aggregate_And_Project_Whitelist()
    {
        var calls = 0;
        var client = new Mock<Mud.Feishu.IFeishuTenantV1TrustPartyCollaborationTenant>();
        client.Setup(c => c.GetCollaborationTenantListAsync(
                It.IsAny<int?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((int? _, string? pageToken, CancellationToken __) =>
            {
                calls++;
                return pageToken is null
                    ? Ok(new GetCollaborationTenantListResult
                    {
                        TargetTenantList =
                        [
                            new CollaborationTenant
                            {
                                TenantKey = "tk_a",
                                TenantName = "合作方 A",
                                TenantShortName = "A",
                                TenantTag = "supplier",
                                ConnectTime = 1760000000,
                                Brand = "Feishu",
                                I18nTenantName = new I18nName { ZhCn = "合作方 A", EnUs = "Partner A" },
                            },
                        ],
                        HasMore = true,
                        PageToken = "t2",
                    })
                    : Ok(new GetCollaborationTenantListResult
                    {
                        TargetTenantList =
                        [
                            new CollaborationTenant { TenantKey = "tk_b", TenantName = "合作方 B" },
                        ],
                        HasMore = false,
                    });
            });

        var result = await CreateTools(client).ListCollaborationTenantsAsync(
            Args(("fetch_all", true)), CancellationToken.None);

        calls.Should().Be(2, "has_more=true 时聚合路径必须继续翻页（页状态显式重载读该 DTO 自己的字段）");

        var payload = Payload(result);
        var items = payload["items"]!.AsArray();
        items.Count.Should().Be(2);
        items[0]!["target_tenant_key"]!.GetValue<string>().Should().Be("tk_a");
        items[0]!["tenant_name"]!.GetValue<string>().Should().Be("合作方 A");
        items[0]!["tenant_tag"]!.GetValue<string>().Should().Be("supplier");
        items[1]!["target_tenant_key"]!.GetValue<string>().Should().Be("tk_b");
        payload["pages_fetched"]!.GetValue<int>().Should().Be(2);
        payload["truncated"]!.GetValue<bool>().Should().BeFalse();

        result.Text.Should().NotContain("Partner A", "i18n 副本不铺开（组织名给默认语言即可）");
    }

    /// <summary>未请求 fetch_all 时只允许一次下游调用——翻页是模型的显式选择。</summary>
    [Fact]
    public async Task ListCollaborationTenants_SinglePage_Should_Call_Downstream_Once()
    {
        var calls = 0;
        var client = new Mock<Mud.Feishu.IFeishuTenantV1TrustPartyCollaborationTenant>();
        client.Setup(c => c.GetCollaborationTenantListAsync(
                It.IsAny<int?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(() =>
            {
                calls++;
                return Ok(new GetCollaborationTenantListResult { HasMore = true, PageToken = "next" });
            });

        var payload = Payload(await CreateTools(client).ListCollaborationTenantsAsync(
            Args(), CancellationToken.None));

        calls.Should().Be(1);
        payload["has_more"]!.GetValue<bool>().Should().BeTrue("单页路径必须原样回传翻页契约，让模型自己决定是否续取");
    }

    /// <summary>空结果给 found=false + 可执行提示（而不是空数组）。</summary>
    [Fact]
    public async Task ListCollaborationTenants_Empty_Should_Return_FoundSentinel()
    {
        var client = new Mock<Mud.Feishu.IFeishuTenantV1TrustPartyCollaborationTenant>();
        client.Setup(c => c.GetCollaborationTenantListAsync(
                It.IsAny<int?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Ok(new GetCollaborationTenantListResult { HasMore = false }));

        var payload = Payload(await CreateTools(client).ListCollaborationTenantsAsync(
            Args(), CancellationToken.None));

        payload["found"]!.GetValue<bool>().Should().BeFalse();
        payload["message"]!.GetValue<string>().Should().Contain("管理后台", "空结果要说清怎么建立互通关系");
    }

    // ─────────────────── 2. 关联组织详情 ───────────────────

    [Fact]
    public async Task GetCollaborationTenant_Should_Project_Summary()
    {
        var client = new Mock<Mud.Feishu.IFeishuTenantV1TrustPartyCollaborationTenant>();
        client.Setup(c => c.GetCollaborationTenantAsync(
                It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Ok(new GetCollaborationTenantResult
            {
                TargetTenant = new CollaborationTenant { TenantKey = "tk_a", TenantName = "合作方 A", Brand = "Feishu" },
            }));

        var payload = Payload(await CreateTools(client).GetCollaborationTenantAsync(
            Args(("target_tenant_key", "tk_a")), CancellationToken.None));

        payload["found"]!.GetValue<bool>().Should().BeTrue();
        payload["target_tenant_key"]!.GetValue<string>().Should().Be("tk_a");
    }

    [Fact]
    public async Task GetCollaborationTenant_Missing_Should_Say_KeyMayBeInvalid()
    {
        var client = new Mock<Mud.Feishu.IFeishuTenantV1TrustPartyCollaborationTenant>();
        client.Setup(c => c.GetCollaborationTenantAsync(
                It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Ok(new GetCollaborationTenantResult { TargetTenant = null }));

        var payload = Payload(await CreateTools(client).GetCollaborationTenantAsync(
            Args(("target_tenant_key", "tk_ghost")), CancellationToken.None));

        payload["found"]!.GetValue<bool>().Should().BeFalse();
        payload["message"]!.GetValue<string>()
            .Should().Contain("list_collaboration_tenants", "必须指回取 key 的那个工具");
    }

    // ─────────────────── 3. 对端可见组织 ───────────────────

    /// <summary>统一三组键形状：按 entity_type 读，不适用的键为 null。</summary>
    [Fact]
    public async Task ListVisibleOrganization_Should_Use_Unified_Shape()
    {
        var client = new Mock<Mud.Feishu.IFeishuTenantV1TrustPartyCollaborationTenant>();
        client.Setup(c => c.GetVisibleOrganizationAsync(
                It.IsAny<string>(), It.IsAny<VisibleOrganizationQuery?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Ok(new GetVisibleOrganizationResult
            {
                CollaborationEntityList =
                [
                    new CollaborationEntity
                    {
                        CollaborationEntityType = "department",
                        DepartmentId = "9f00a1",
                        OpenDepartmentId = "od_9f00a1",
                        DepartmentName = "外部研发部",
                    },
                ],
                HasMore = false,
            }));

        var payload = Payload(await CreateTools(client).ListVisibleOrganizationAsync(
            Args(("target_tenant_key", "tk_a")), CancellationToken.None));

        var item = payload["items"]!.AsArray()[0]!;
        item!["entity_type"]!.GetValue<string>().Should().Be("department");
        item["department_name"]!.GetValue<string>().Should().Be("外部研发部");
        item["user_id"].Should().BeNull("不适用的键给 null——统一形状便于模型按 entity_type 分流");
    }

    /// <summary>对端可见范围为空：文案要说清"可能是被收窄了"，而不是"对端没有开放"。</summary>
    [Fact]
    public async Task ListVisibleOrganization_Empty_Should_Hint_About_Narrowing()
    {
        var client = new Mock<Mud.Feishu.IFeishuTenantV1TrustPartyCollaborationTenant>();
        client.Setup(c => c.GetVisibleOrganizationAsync(
                It.IsAny<string>(), It.IsAny<VisibleOrganizationQuery?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Ok(new GetVisibleOrganizationResult { HasMore = false }));

        var payload = Payload(await CreateTools(client).ListVisibleOrganizationAsync(
            Args(("target_tenant_key", "tk_a")), CancellationToken.None));

        payload["found"]!.GetValue<bool>().Should().BeFalse();
        payload["message"]!.GetValue<string>()
            .Should().Contain("target_department_id", "要提示去掉收窄参数重试");
    }

    // ─────────────────── 4. 外部成员（PII 净化核心用例） ───────────────────

    /// <summary>
    /// 出站净化：手机号 / 工号 / 自定义属性 / 头像 / 在职状态一个都不许出现。
    /// </summary>
    /// <remarks>
    /// 这条用例钉的是<b>域级边界**而不是字段名**：断言同时覆盖"键名不出现"与"值不出现"，
    /// 防止将来有人改投影时把 `mobile` 换成 `phone` 之类同义键绕过。
    /// </remarks>
    [Fact]
    public async Task GetCollaborationUser_Should_Strip_Sensitive_Fields()
    {
        var client = new Mock<Mud.Feishu.IFeishuTenantV1TrustPartyCollaborationTenant>();
        client.Setup(c => c.GetCollaborationUserAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Ok(new GetCollaborationUserResult
            {
                TargetUser = new CollaborationUser
                {
                    OpenId = "ou_ext_1",
                    UserId = "u_ext_1",
                    UnionId = "on_ext_1",
                    Name = "张三（外部）",
                    JobTitle = "外部架构师",
                    Mobile = "22643712509",
                    EmployeeNo = "EXT-42",
                    Avatar = new Mud.Feishu.DataModels.TrustParty.AvatarInfo { Avatar72 = "https://x.example/avatar.png" },
                    Status = new UserStatus { IsResigned = true, IsFrozen = false },
                    CustomAttrs = [new UserCustomAttr { Type = "text", Value = new UserCustomAttrValue { Text = "家庭住址" } }],
                    ParentDepartmentIds = [new CollaborationDepartmentId { DepartmentId = "9f00a1", OpenDepartmentId = "od_9f00a1" }],
                    LeaderId = new CollaborationUserId { UserId = "u_leader", OpenId = "ou_leader" },
                },
            }));

        var result = await CreateTools(client).GetCollaborationUserAsync(
            Args(
                ("target_tenant_key", "tk_a"),
                ("target_user_id", "ou_ext_1"),
                ("target_user_id_type", "open_id")),
            CancellationToken.None);

        var payload = Payload(result);

        // ✅ 该给的
        payload["found"]!.GetValue<bool>().Should().BeTrue();
        payload["name"]!.GetValue<string>().Should().Be("张三（外部）");
        payload["job_title"]!.GetValue<string>().Should().Be("外部架构师");
        payload["open_id"]!.GetValue<string>().Should().Be("ou_ext_1");
        payload["parent_department_ids"]!.AsArray()[0]!["open_department_id"]!.GetValue<string>()
            .Should().Be("od_9f00a1");
        payload["leader_open_id"]!.GetValue<string>().Should().Be("ou_leader");

        // ❌ 不许出去的：键名
        payload.ContainsKey("mobile").Should().BeFalse();
        payload.ContainsKey("employee_no").Should().BeFalse();
        payload.ContainsKey("custom_attrs").Should().BeFalse();
        payload.ContainsKey("avatar").Should().BeFalse();
        payload.ContainsKey("status").Should().BeFalse();
        payload.ContainsKey("is_resigned").Should().BeFalse();

        // ❌ 不许出去的：值（防同义改名绕过）
        result.Text.Should().NotContain("22643712509", "手机号不得出站");
        result.Text.Should().NotContain("EXT-42", "工号不得出站");
        result.Text.Should().NotContain("avatar.png", "头像 URL 是可追踪资源，不得出站");
        result.Text.Should().NotContain("家庭住址", "自定义属性内容不可控，不得出站");

        payload["notice"]!.GetValue<string>().Should().Contain("净化", "提示文案必须说明为什么查不到联系方式");
    }

    [Fact]
    public async Task GetCollaborationUser_Missing_Should_Explain_Id_Type_And_Scope()
    {
        var client = new Mock<Mud.Feishu.IFeishuTenantV1TrustPartyCollaborationTenant>();
        client.Setup(c => c.GetCollaborationUserAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Ok(new GetCollaborationUserResult { TargetUser = null }));

        var payload = Payload(await CreateTools(client).GetCollaborationUserAsync(
            Args(("target_tenant_key", "tk_a"), ("target_user_id", "ou_ghost")), CancellationToken.None));

        payload["found"]!.GetValue<bool>().Should().BeFalse();
        payload["message"]!.GetValue<string>()
            .Should().Contain("target_user_id_type", "要把'ID 口径不一致'与'没开放'分开讲");
    }

    // ─────────────────── 5. 外部部门 ───────────────────

    /// <summary>部门负责人只给标识（不给姓名）——姓名要再走一次成员查询。</summary>
    [Fact]
    public async Task GetCollaborationDepartment_Should_Project_Leader_Identifiers_Only()
    {
        var client = new Mock<Mud.Feishu.IFeishuTenantV1TrustPartyCollaborationTenant>();
        client.Setup(c => c.GetCollaborationDepartmentAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Ok(new GetCollaborationDepartmentResult
            {
                TargetDepartment = new CollaborationDepartment
                {
                    DepartmentId = "9f00a1",
                    OpenDepartmentId = "od_9f00a1",
                    Name = "外部研发部",
                    Order = "10",
                    ParentDepartmentId = new CollaborationDepartmentId { DepartmentId = "root", OpenDepartmentId = "od_root" },
                    Leaders = [new CollaborationDepartmentLeader { LeaderType = 1, Id = new CollaborationUserId { UserId = "u_lead", OpenId = "ou_lead" } }],
                },
            }));

        var payload = Payload(await CreateTools(client).GetCollaborationDepartmentAsync(
            Args(("target_tenant_key", "tk_a"), ("target_department_id", "od_9f00a1")),
            CancellationToken.None));

        payload["found"]!.GetValue<bool>().Should().BeTrue();
        payload["open_department_id"]!.GetValue<string>().Should().Be("od_9f00a1");
        payload["parent_open_department_id"]!.GetValue<string>().Should().Be("od_root");

        var leader = payload["leaders"]!.AsArray()[0]!.AsObject();
        leader["leader_type"]!.GetValue<int>().Should().Be(1);
        leader["user_id"]!.GetValue<string>().Should().Be("u_lead");
        leader.ContainsKey("name").Should().BeFalse("负责人只给标识——要姓名请走 get_collaboration_user");
        payload["notice"]!.GetValue<string>().Should().Contain("get_collaboration_user");
    }

    // ─────────────────── 6. 共享实体（三份清单 + 合并 items） ───────────────────

    /// <summary>
    /// 合并 <c>items</c>（带 kind）与三份分类清单必须<b>同源同一次投影</b>——
    /// 少了 items，统一分页出口的 fetch_all 就会把三份清单聚合成空。
    /// </summary>
    [Fact]
    public async Task ListShareEntities_Should_Merge_Items_And_Keep_Three_Lists()
    {
        var client = new Mock<Mud.Feishu.IFeishuTenantV1DirectoryShareEntity>();
        client.Setup(c => c.GetShareEntityListAsync(
                It.IsAny<ShareEntityListQuery?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Ok(new GetShareEntityListResult
            {
                ShareDepartments = [new ShareDepartment { OpenDepartmentId = "od_d", Name = new I18nContent { Value = "共享部门" } }],
                ShareGroups = [new ShareGroup { OpenGroupId = "oc_g", Name = new I18nContent { Value = "共享用户组" } }],
                ShareUsers = [new ShareUser { OpenUserId = "ou_u", Name = new I18nContent { Value = "共享成员" } }],
                HasMore = false,
            }));

        var payload = Payload(await CreateTools(shareEntity: client).ListShareEntitiesAsync(
            Args(("target_tenant_key", "tk_a")), CancellationToken.None));

        payload["departments"]!.AsArray().Count.Should().Be(1);
        payload["groups"]!.AsArray().Count.Should().Be(1);
        payload["users"]!.AsArray().Count.Should().Be(1);

        var items = payload["items"]!.AsArray();
        items.Count.Should().Be(3, "合并 items 是统一分页出口的聚合形态，必须与三份清单同源");
        items[0]!["kind"]!.GetValue<string>().Should().Be("department");
        items[0]!["name"]!.GetValue<string>().Should().Be("共享部门");
        items[1]!["kind"]!.GetValue<string>().Should().Be("group");
        items[2]!["kind"]!.GetValue<string>().Should().Be("user");
    }

    /// <summary>共享实体为空：文案要指向 is_select_subject 口径，而不是"没有共享"。</summary>
    [Fact]
    public async Task ListShareEntities_Empty_Should_Hint_About_Subject_Flag()
    {
        var client = new Mock<Mud.Feishu.IFeishuTenantV1DirectoryShareEntity>();
        client.Setup(c => c.GetShareEntityListAsync(
                It.IsAny<ShareEntityListQuery?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Ok(new GetShareEntityListResult { HasMore = false }));

        var payload = Payload(await CreateTools(shareEntity: client).ListShareEntitiesAsync(
            Args(), CancellationToken.None));

        payload["found"]!.GetValue<bool>().Should().BeFalse();
        payload["message"]!.GetValue<string>().Should().Contain("is_select_subject");
    }

    // ─────────────────── 7. 目录侧关联组织 ───────────────────

    /// <summary>directory 面的名称来自 <see cref="I18nContent.Value"/>（不是 ZhCn）。</summary>
    [Fact]
    public async Task ListAllCollaborationTenants_Should_Project_I18n_Value()
    {
        var client = new Mock<Mud.Feishu.IFeishuTenantV1DirectoryCollaborationTenant>();
        client.Setup(c => c.GetAllCollaborationTenantListAsync(
                It.IsAny<int?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Ok(new GetAllCollaborationTenantListResult
            {
                Items =
                [
                    new DirectoryCollaborationTenant
                    {
                        TenantKey = "tk_dir",
                        Name = new I18nContent { Value = "目录侧组织", Locale = "zh" },
                        ShortName = new I18nContent { Value = "目录" },
                        Brand = "Feishu",
                    },
                ],
                HasMore = false,
            }));

        var payload = Payload(await CreateTools(directoryTenant: client).ListAllCollaborationTenantsAsync(
            Args(), CancellationToken.None));

        var item = payload["items"]!.AsArray()[0]!;
        item!["target_tenant_key"]!.GetValue<string>().Should().Be("tk_dir");
        item["name"]!.GetValue<string>().Should().Be("目录侧组织");
        item["short_name"]!.GetValue<string>().Should().Be("目录");
    }

    /// <summary>directory 面空结果必须点明"两个接口面不是一回事"（别让人据此断言没有互通）。</summary>
    [Fact]
    public async Task ListAllCollaborationTenants_Empty_Should_Explain_Different_Face()
    {
        var client = new Mock<Mud.Feishu.IFeishuTenantV1DirectoryCollaborationTenant>();
        client.Setup(c => c.GetAllCollaborationTenantListAsync(
                It.IsAny<int?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Ok(new GetAllCollaborationTenantListResult { HasMore = false }));

        var payload = Payload(await CreateTools(directoryTenant: client).ListAllCollaborationTenantsAsync(
            Args(), CancellationToken.None));

        payload["message"]!.GetValue<string>().Should().Contain("不同接口面");
    }

    // ─────────────────── 8. 互通规则 ───────────────────

    /// <summary>失效规则要被<b>算出来</b>（invalid_rule_count），而不是让模型逐条比对布尔值。</summary>
    [Fact]
    public async Task ListCollaborationRules_Should_Report_Invalid_Rule_Count()
    {
        var client = new Mock<Mud.Feishu.IFeishuTenantV1DirectoryCollaborationRule>();
        client.Setup(c => c.GetCollaborationRuleListAsync(
                It.IsAny<string>(), It.IsAny<int?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Ok(new GetCollaborationRuleListResult
            {
                Items =
                [
                    new CollaborationRule
                    {
                        RuleId = "r1",
                        SubjectIsValid = true,
                        ObjectIsValid = true,
                        Subjects = new CollaborationRuleEntities { OpenUserIds = ["ou_a"], OpenDepartmentIds = ["od_a"] },
                        Objects = new CollaborationRuleEntities { OpenUserIds = ["ou_b"] },
                    },
                    new CollaborationRule
                    {
                        RuleId = "r2",
                        SubjectIsValid = false,
                        ObjectIsValid = true,
                        Subjects = new CollaborationRuleEntities { OpenUserIds = ["ou_gone"] },
                    },
                ],
                HasMore = false,
            }));

        var payload = Payload(await CreateTools(collaborationRule: client).ListCollaborationRulesAsync(
            Args(("target_tenant_key", "tk_dir")), CancellationToken.None));

        var items = payload["items"]!.AsArray();
        items.Count.Should().Be(2);
        items[0]!["subjects"]!["open_user_ids"]!.AsArray()[0]!.GetValue<string>().Should().Be("ou_a");

        payload["invalid_rule_count"]!.GetValue<int>().Should().Be(1, "r2 的主体已失效");
        payload["notice"]!.GetValue<string>().Should().Contain("失效");
    }

    // ─────────────────── 10. 写面：互通规则 CRUD ───────────────────

    private static Mock<Mud.Feishu.IFeishuTenantV1DirectoryCollaborationRule> RuleClient(
        Action<CreateCollaborationRuleRequest, string>? onCreate = null,
        Action<string, UpdateCollaborationRuleRequest, string>? onUpdate = null,
        Action<string, string>? onDelete = null,
        int updateCode = 0,
        int deleteCode = 0)
    {
        var client = new Mock<Mud.Feishu.IFeishuTenantV1DirectoryCollaborationRule>();
        client.Setup(c => c.CreateCollaborationRuleAsync(
                It.IsAny<CreateCollaborationRuleRequest>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((CreateCollaborationRuleRequest request, string tenantKey, CancellationToken _) =>
            {
                onCreate?.Invoke(request, tenantKey);
                return Ok(new CreateCollaborationRuleResult { AddRuleId = "r_new" });
            });

        client.Setup(c => c.UpdateCollaborationRuleAsync(
                It.IsAny<string>(), It.IsAny<UpdateCollaborationRuleRequest>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string ruleId, UpdateCollaborationRuleRequest request, string tenantKey, CancellationToken _) =>
            {
                onUpdate?.Invoke(ruleId, request, tenantKey);
                return new FeishuNullDataApiResult { Code = updateCode, Msg = updateCode == 0 ? "success" : "boom" };
            });

        client.Setup(c => c.DeleteCollaborationRuleAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string ruleId, string tenantKey, CancellationToken _) =>
            {
                onDelete?.Invoke(ruleId, tenantKey);
                return new FeishuNullDataApiResult { Code = deleteCode, Msg = deleteCode == 0 ? "success" : "boom" };
            });

        return client;
    }

    private static IReadOnlyDictionary<string, object?> RuleArgs(
        string tool, params (string Key, object? Value)[] items)
        => Args([("target_tenant_key", "tk_dir"), .. items]);

    /// <summary>dry_run 必须**零调用下游**（这是写工具的第一道防线）。</summary>
    [Fact]
    public async Task Create_DryRun_Should_Not_Call_Downstream()
    {
        var calls = 0;
        var client = RuleClient(onCreate: (_, _) => calls++);

        var result = await CreateTools(collaborationRule: client).CreateCollaborationRuleAsync(
            RuleArgs("create",
                ("subject_open_user_ids", new[] { "ou_a", "ou_a2" }),
                ("object_open_department_ids", new[] { "od_b" }),
                ("dry_run", true)),
            CancellationToken.None);

        calls.Should().Be(0, "dry_run=true 时不得调用下游");
        result.Text.Should().Contain("未调用下游", "预演结果必须显式说明没有真发请求");
        result.Text.Should().Contain("POST", "预演要给出 HTTP 方法");
        result.Text.Should().Contain("subject_entities", "预演要给出主体条数（不回原文 ID）");
    }

    /// <summary>实发：回执带 rule_id + 两侧条数，且请求体按白名单合成。</summary>
    [Fact]
    public async Task Create_Should_Project_Receipt_With_RuleId()
    {
        CreateCollaborationRuleRequest? captured = null;
        var client = RuleClient(onCreate: (request, _) => captured = request);

        var payload = Payload(await CreateTools(collaborationRule: client).CreateCollaborationRuleAsync(
            RuleArgs("create",
                ("subject_open_user_ids", new[] { "ou_a" }),
                ("object_open_group_ids", new[] { "oc_b" })),
            CancellationToken.None));

        captured.Should().NotBeNull();
        captured!.Subjects!.OpenUserIds.Should().Equal(["ou_a"], "主体侧必须按 open_user_ids 合成");
        captured.Objects!.OpenGroupIds.Should().Equal(["oc_b"], "客体侧必须按 open_group_ids 合成");

        payload["rule_id"]!.GetValue<string>().Should().Be("r_new");
        payload["subject_entities"]!.GetValue<int>().Should().Be(1);
        payload["object_entities"]!.GetValue<int>().Should().Be(1);
        payload["created"]!.GetValue<bool>().Should().BeTrue();
    }

    /// <summary>空侧必须**在 dry_run 之前**被拒（否则模型会拿着"预演成功"的信心去改参数）。</summary>
    [Fact]
    public async Task Create_With_Empty_Side_Should_Reject_Before_DryRun()
    {
        var calls = 0;
        var client = RuleClient(onCreate: (_, _) => calls++);

        var result = await CreateTools(collaborationRule: client).CreateCollaborationRuleAsync(
            RuleArgs("create",
                ("subject_open_user_ids", new[] { "ou_a" }),
                ("dry_run", true)),
            CancellationToken.None);

        calls.Should().Be(0, "参数非法时连预演都不放行");
        result.Text.Should().Contain("tool_error", "必须是结构化错误，模型才能自查");
        result.Text.Should().Contain("object_*", "错误信息要点名缺的是哪一侧");
    }

    /// <summary>实体数之和 ≥100 必须被拒（平台硬约束，超了必被拒）。</summary>
    [Fact]
    public async Task Create_Exceeding_Entity_Budget_Should_Reject()
    {
        var users = Enumerable.Range(0, 60).Select(i => $"ou_{i}").ToArray();
        var departments = Enumerable.Range(0, 60).Select(i => $"od_{i}").ToArray();
        var client = RuleClient();

        var result = await CreateTools(collaborationRule: client).CreateCollaborationRuleAsync(
            RuleArgs("create",
                ("subject_open_user_ids", users),
                ("object_open_department_ids", departments)),
            CancellationToken.None);

        result.Text.Should().Contain("tool_error");
        result.Text.Should().Contain("100", "要报出平台上限，模型才知道该拆批");
    }

    /// <summary>update 是覆盖式语义：请求体两侧按传入合成，回执带覆盖提醒。</summary>
    [Fact]
    public async Task Update_Should_Project_Receipt_And_Warn_About_Replace()
    {
        string? capturedRuleId = null;
        UpdateCollaborationRuleRequest? captured = null;
        var client = RuleClient(onUpdate: (ruleId, request, _) =>
        {
            capturedRuleId = ruleId;
            captured = request;
        });

        var payload = Payload(await CreateTools(collaborationRule: client).UpdateCollaborationRuleAsync(
            RuleArgs("update",
                ("collaboration_rule_id", "r1"),
                ("subject_open_department_ids", new[] { "od_a", "od_a2" }),
                ("object_open_user_ids", new[] { "ou_b" })),
            CancellationToken.None));

        capturedRuleId.Should().Be("r1");
        captured!.Subjects!.OpenDepartmentIds.Should().Equal(["od_a", "od_a2"]);
        captured.Objects!.OpenUserIds.Should().Equal(["ou_b"]);

        payload["rule_id"]!.GetValue<string>().Should().Be("r1");
        payload["updated"]!.GetValue<bool>().Should().BeTrue();
        payload["notice"]!.GetValue<string>().Should().Contain("覆盖式", "必须提醒未传的一侧会被清空");
    }

    /// <summary>delete（high-risk-write）：dry_run 零调用，实发回执带不可撤销提醒。</summary>
    [Fact]
    public async Task Delete_DryRun_Then_Real_Should_Return_Receipt()
    {
        var calls = 0;
        var client = RuleClient(onDelete: (_, _) => calls++);

        var preview = await CreateTools(collaborationRule: client).DeleteCollaborationRuleAsync(
            RuleArgs("delete", ("collaboration_rule_id", "r1"), ("dry_run", true)),
            CancellationToken.None);

        calls.Should().Be(0, "高危写的第一跳必须是 dry_run");
        preview.Text.Should().Contain("未调用下游");
        preview.Text.Should().Contain("DELETE", "预演要给出 HTTP 方法");

        var payload = Payload(await CreateTools(collaborationRule: client).DeleteCollaborationRuleAsync(
            RuleArgs("delete", ("collaboration_rule_id", "r1")),
            CancellationToken.None));

        calls.Should().Be(1, "dry_run=false 时才真发");
        payload["rule_id"]!.GetValue<string>().Should().Be("r1");
        payload["deleted"]!.GetValue<bool>().Should().BeTrue();
        payload["notice"]!.GetValue<string>().Should().Contain("不可撤销", "高危写必须复述后果");
    }

    /// <summary>平台返回非 0 必须转成结构化错误（而不是静默回执成功）。</summary>
    [Fact]
    public async Task Delete_With_Platform_Error_Should_Fail_Structured()
    {
        var client = RuleClient(deleteCode: 1390005);

        var result = await CreateTools(collaborationRule: client).DeleteCollaborationRuleAsync(
            RuleArgs("delete", ("collaboration_rule_id", "r_ghost")),
            CancellationToken.None);

        result.Text.Should().Contain("1390005", "错误码必须透出，宿主才能按码处置");
    }

    // ─────────────────── 9. 软缺席 ───────────────────

    /// <summary>四个客户端全部未注册时，四个面各测一个代表工具：必须给结构化错误，不得抛异常。</summary>
    [Fact]
    public async Task Missing_Clients_Should_Return_Structured_Error_Not_Throw()
    {
        var tools = CreateTools();

        foreach (var call in new Func<Task<FeishuToolResult>>[]
        {
            () => tools.ListCollaborationTenantsAsync(Args(), CancellationToken.None),
            () => tools.ListShareEntitiesAsync(Args(), CancellationToken.None),
            () => tools.ListAllCollaborationTenantsAsync(Args(), CancellationToken.None),
            () => tools.ListCollaborationRulesAsync(Args(("target_tenant_key", "tk")), CancellationToken.None),
        })
        {
            var result = await call();
            result.Text.Should().Contain("未注册", "文案要说清是宿主未启用互通 API，而不是模型参数错");
            result.Text.Should().Contain("tool_error", "必须是结构化错误（第一行紧凑 JSON）");
        }
    }
}
