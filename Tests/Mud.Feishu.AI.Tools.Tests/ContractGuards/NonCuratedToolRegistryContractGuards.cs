// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.AI.Tools.Tests.ContractGuards;

/// <summary>
/// <b>有意不策展的登记守卫（R7 / §4.1 R1 风险登记）</b>：新域里"沉默地不被暴露"必须是<b>显式登记</b>，
/// 而不是"没人想起来"。
/// </summary>
/// <remarks>
/// <para>
/// <b>要解决的缺陷形态</b>：模型能力域里存在天然不适合工具面的方法——本地文件入参
/// （<c>[FormContent]</c>）、二进制返回（<c>byte[]</c>）、任意 SQL 执行。它们<b>不该</b>被策展，
/// 但"不策展"这件事本身没有任何构建期信号：下一次有人"顺手补全域覆盖"时会把它加进来，
/// 而那时唯一能拦住的只有二进制守卫（它只覆盖 <c>byte[]</c> 与 Stream 形态）。
/// </para>
/// <para>
/// 本守卫把这批方法<b>钉在案上</b>：它们一旦出现在工具契约面（<c>Curation/</c>）即报红。
/// 新增域时同批补一次登记——成本一行，收益是"越权/二进制通道不会悄悄开出来"。
/// </para>
/// </remarks>
public class NonCuratedToolRegistryContractGuards
{
    private const string CurationDirectory = "Mud.Feishu.AI.Tools/Curation";

    /// <summary>
    /// 有意不策展的 SDK 方法（跨新域汇总）。
    /// </summary>
    /// <remarks>
    /// 判据是「方法名不得出现在 Curation/ 的任何 <c>Source</c> 引用中」——用方法名（而非限定名）
    /// 是因为 <c>Source</c> 以 <c>nameof</c> 拼接形式声明，文本扫描只能看到方法名。
    /// </remarks>
    private static readonly (string Method, string Reason)[] NonCuratedMethods =
    [
        ("UploadHtmlCodeAndReleaseAsync", "spark：入参为 [FormContent] 本地 tar 文件路径——模型无文件系统，策展即「永远无法成功」的工具"),
        ("UploadAppIconAsync", "spark：入参为 [FormContent] 本地图标文件路径——同上"),
        ("UploadStorageAsync", "spark：对象存储上传（含 byte[]）→ A10 二进制防线"),
        ("DownloadStorageAsync", "spark：对象存储下载（返回 byte[]）→ A10 二进制防线"),
        ("ExecuteSqlAsync", "spark：任意 SQL 执行 = 越权通道，无法机械校验表归属"),
        ("BatchUpdateTableRecordsAsync", "spark：约束反直觉（不同行字段须一致）且与 spark.update_table_records 重叠"),
        ("DownloadWhiteboardImageAsync", "board：画板缩略图（返回 byte[]）→ A10 二进制防线"),

        // F-1 首个补域（MDM）：只策展只读的国家/地区批量查询，其余方法显式登记为"有意不策展"。
        ("BindUserAuthDataRelationAsync", "mdm：写类（用户数据维度绑定，涉及用户 ID）——**产品裁定不策展**：HR/组织主数据写入属管理后台职责（AI 走「提议 → 人工执行」）"),
        ("UnbindUserAuthDataRelationAsync", "mdm：写类（解绑），同上"),
        ("ListCountryRegionsAsync", "mdm：带 filter 表达式体的分页查询——首个切片只策展 mdm.get_countries，本方法留待下一片"),

        // F-1 P0（Security）：只策展只读的"行为审计日志"；其余 12 个方法显式登记（写类 / PII 读 / 留待下一片）。
        // F-1 收官：ListOpenApiLogDataAsync **已策展**为 security.list_openapi_logs（含 PII 登记三处），
        // 故从"有意不策展"表移出——留在本表会被 NonCuratedMethods_ShouldNotBeToolSource 判为越权通道。
        ("CreateUserMigrationAsync", "security：用户数据迁移（写类 + 跨 geo 合规敏感）——**产品裁定不策展**：HR/组织主数据写入属管理后台职责（AI 走「提议 → 人工执行」）"),
        ("CancelUserMigrationAsync", "security：取消迁移（写类）——同上"),
        ("SearchUserMigrationsAsync", "security：迁移记录检索（POST 检索、含用户 PII）——同上"),
        ("GetMultiGeoEntityTenantAsync", "security：多 geo 实体查询（PII）——同上"),
        ("GetUserMigrationAsync", "security：迁移详情（PII）——同上"),
        ("CreateDeviceRecordAsync", "security：设备登记（写类 + 设备指纹 PII）——同上"),
        ("UpdateDeviceRecordAsync", "security：设备更新（写类）——同上"),
        ("DeleteDeviceRecordAsync", "security：设备删除（写类，高风险）——同上"),
        ("ListDeviceRecordsAsync", "security：设备列表（设备 PII）——同上"),
        ("GetDeviceRecordAsync", "security：设备详情（设备 PII）——同上"),
        ("UpdateDeviceApplyRecordAsync", "security：设备申请审核（写类）——同上"),

        // F-1（org）：只读面已全量策展（职务 / 职级 / 职务族 / 工作城市 共 8 条，见 People/FeishuOrgToolInterfaces.cs）；
        // GetJobLevel* / GetJobFamilies* / GetWorkCities* 六个只读方法**已移出本登记表**（它们现在是策展工具）。
        // 下表仅保留写类——它们需要宿主显式授权，**产品裁定不策展**：HR/组织主数据写入属管理后台职责（AI 走「提议 → 人工执行」）。
        ("CreateJobLevelAsync", "org：职级写类（新增）——**产品裁定不策展**：HR/组织主数据写入属管理后台职责（AI 走「提议 → 人工执行」）"),
        ("UpdateJobLevelAsync", "org：职级写类（更新）——同上"),
        ("DeleteJobLevelByIdAsync", "org：职级写类（删除，高风险）——同上"),
        ("CreateJobFamilyAsync", "org：职务族写类（新增）——同上"),
        ("UpdateJobFamilyAsync", "org：职务族写类（更新）——同上"),
        ("DeleteJobFamilyByIdAsync", "org：职务族写类（删除，高风险）——同上"),

        // F-1（hire）：首批只策展"职位"两条（list_jobs / get_job）；其余显式登记。
        // Hire 是 SDK 最大域（165 方法），候选/投递/面试/Offer 均属 PII 批次，不在本批范围。
        ("GetJobDetailAsync", "hire：职位扩展详情（只读）——留待下一片（list 精简 / get 完整的分层已建立）"),
        ("GetJobConfigAsync", "hire：职位配置（只读）——留待下一片"),
        ("CombinedCreateJobAsync", "hire：职位写类（复合创建）——**产品裁定不策展**：HR/组织主数据写入属管理后台职责（AI 走「提议 → 人工执行」）"),
        ("CombinedUpdateJobAsync", "hire：职位写类（复合更新）——同上"),
        ("UpdateJobConfigAsync", "hire：职位配置写类——同上"),
        ("BatchUpdateJobManagerAsync", "hire：职位负责人批量写类——同上"),
        // 招聘需求列表 GetJobRequirementListAsync **已策展**为 hire.list_job_requirements，故从本表移出
        //（纪律 25：留在本表会被 NonCuratedMethods_ShouldNotBeToolSource 判为越权通道）。
        ("SearchJobRequirementAsync", "hire：招聘需求搜索（只读）——留待下一片"),
        ("CreateJobRequirementAsync", "hire：招聘需求写类——写面**产品裁定不策展**：HR/组织主数据写入属管理后台职责（AI 走「提议 → 人工执行」）"),
        ("UpdateJobRequirementAsync", "hire：招聘需求写类——同上"),
        ("DeleteJobRequirementAsync", "hire：招聘需求写类（删除）——同上"),

        // hire 候选人：只策展"检索 + 联系字段"两条；候选人写类与档案类显式登记。
        ("CombinedCreateTalentAsync", "hire：候选人写类（复合创建，含档案）——**产品裁定不策展**：HR/组织主数据写入属管理后台职责（AI 走「提议 → 人工执行」）"),
        ("CombinedUpdateTalentAsync", "hire：候选人写类（复合更新，含档案）——同上"),
        ("AddTalentToTalentPoolAsync", "hire：候选人写类（加入人才库）——同上"),
        ("BatchChangeTalentPoolAsync", "hire：候选人写类（批量调整人才库）——同上"),
        ("ChangeTalentBlockAsync", "hire：候选人写类（拉黑/解黑，高风险）——同上"),
        ("GetInterviewByTalentAsync", "hire：按候选人查全部面试（不分页，含评价与面试官）——已由 hire.list_interviews 覆盖（分页 + 可按投递/时间窗收窄），故不单独策展"),

        // hire Offer：只策展"列表看状态"一条；写类与**含薪资条件的详情**显式登记。
        ("CreateOfferAsync", "hire：Offer 写类（创建，带薪资条件）——**产品裁定不策展**：HR/组织主数据写入属管理后台职责（AI 走「提议 → 人工执行」）"),
        ("UpdateOfferAsync", "hire：Offer 写类（更新，带薪资条件）——同上"),
        ("ChangeOfferStatusAsync", "hire：Offer 写类（改审批/收发状态）——同上"),
        ("ChangeInternOfferStatusAsync", "hire：实习 Offer 写类——同上"),
        ("GetOfferAsync", "hire：Offer 详情（含 ApplicationOfferSalaryPlan 薪资条件）——**不策展**：状态由 hire.list_offers 回答，薪资条件不进模型上下文（纪律 26/28）"),
        ("GetApplicationOfferAsync", "hire：按投递取 Offer 详情（同样含薪资条件）——同上"),

        // F-1（contact 部门面）：只策展两条缺失方向（上级链 / 按名搜索），其余留待下一片。
        ("GetDepartmentInfoByIdAsync", "contact：按 ID 查部门（只读）——已有 list/search/parents 三条路径，暂不重复"),
        ("GetDepartmentsPageListAsync", "contact：部门全量分页（只读）——可由 list_departments 下钻替代"),
    ];

    [Fact]
    public void NonCuratedMethods_ShouldNotBeToolSource()
    {
        var root = FindRepositoryRoot();
        var curation = Path.Combine(root, CurationDirectory.Replace('/', Path.DirectorySeparatorChar));
        Directory.Exists(curation).Should().BeTrue($"工具契约目录必须存在：{CurationDirectory}");

        var sources = Directory
            .EnumerateFiles(curation, "*.cs", SearchOption.AllDirectories)
            .ToArray();
        sources.Should().NotBeEmpty("契约面为空 ⇒ 扫描面失效（假绿）");

        var violations = new List<string>();
        foreach (var (method, reason) in NonCuratedMethods)
        {
            foreach (var file in sources)
            {
                var text = File.ReadAllText(file);
                if (!ContainsNameofReference(text, method))
                {
                    continue;
                }

                violations.Add(
                    $"{Path.GetFileName(file)} 引用了不策展方法 {method}（{reason}）");
            }
        }

        violations.Should().BeEmpty(
            "以下「有意不策展」的方法被引进了工具契约面——它们会把二进制/本地文件/越权通道开给模型：{0}",
            string.Join(" | ", violations));
    }

    /// <summary>
    /// 反向自证：扫描器必须能真的「看到」引用（用合成文本驱动判定逻辑）。
    /// </summary>
    /// <remarks>
    /// 真实 <c>Source</c> 形态是 <c>nameof(接口) + "." + nameof(接口.方法)</c>——方法名<b>带限定名</b>，
    /// 故判据必须覆盖限定形态（早期只查 <c>nameof(方法)</c> 会让本守卫恒绿）。
    /// </remarks>
    [Fact]
    public void Scanner_ShouldDetectReference_WhenSyntheticSourcePresent()
    {
        const string synthetic =
            "Source = nameof(IFeishuUserV1SparkApp) + \".\" + nameof(IFeishuUserV1SparkApp.UploadHtmlCodeAndReleaseAsync)";

        ContainsNameofReference(synthetic, "UploadHtmlCodeAndReleaseAsync").Should().BeTrue(
            "判据失效：合成文本里的 nameof 限定引用未被识别——那本守卫会因「扫不到」而假绿");

        ContainsNameofReference(synthetic, "UploadHtmlCodeAndRelease").Should().BeFalse(
            "判据失效：方法名前缀被误判为命中（子串匹配会让守卫误报）");
    }

    /// <summary>文本中是否存在 <c>nameof(...METHOD)</c> 形态的引用（方法名可带接口限定名）。</summary>
    private static bool ContainsNameofReference(string text, string method)
        => System.Text.RegularExpressions.Regex.IsMatch(
            text,
            @"nameof\([A-Za-z0-9_.]*\b" + System.Text.RegularExpressions.Regex.Escape(method) + @"\)",
            System.Text.RegularExpressions.RegexOptions.None);

    /// <summary>
    /// 出席自证：本批登记的域确实已在契约面（防"域根本没策展却登记了不策展方法"的伪登记）。
    /// </summary>
    [Fact]
    public void RegisteredDomains_ShouldActuallyBeCurated()
    {
        foreach (var toolName in new[]
                 {
                     FeishuToolNames.SparkListApps,
                     FeishuToolNames.SparkCreateApp,
                     FeishuToolNames.BoardGetTheme,
                     FeishuToolNames.AttendanceQueryMyFlow,

                     // F-1：登记了"不策展方法"的域必须真的已策展（防伪登记）。
                     FeishuToolNames.MdmGetCountries,
                     FeishuToolNames.SecurityQueryAuditLogs,
                     FeishuToolNames.OrgListJobTitles,
                     FeishuToolNames.OrgListJobLevels,
                     FeishuToolNames.OrgListJobFamilies,
                     FeishuToolNames.OrgListWorkCities,
                     FeishuToolNames.SecurityListOpenapiLogs,
                     FeishuToolNames.ContactGetDepartmentParents,
                     FeishuToolNames.HireListJobs,
                     FeishuToolNames.HireListTalents,
                 })
        {
            FeishuToolNames.All.Should().Contain(toolName,
                $"{toolName} 必须已策展——否则「不策展登记」讨论的是一个不存在的域");
        }
    }

    private static string FindRepositoryRoot()
    {
        var directory = AppContext.BaseDirectory;
        while (!string.IsNullOrEmpty(directory) && !File.Exists(Path.Combine(directory, "Mud.Feishu.slnx")))
        {
            directory = Path.GetDirectoryName(directory);
        }

        directory.Should().NotBeNullOrEmpty("测试必须能定位仓库根目录（以 Mud.Feishu.slnx 为锚）");
        return directory!;
    }
}
