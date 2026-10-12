// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Mud.Feishu.Abstractions;
using Mud.Feishu.AI.Channels;
using Mud.Feishu.AI.Events;
using Mud.Feishu.AI.Knowledge;
using Mud.Feishu.AI.Tools.Registration;

namespace Mud.Feishu.AI.Tools;

/// <summary>
/// 飞书工具包注册（Phase 1 只读 + Phase 2 写类/流式通道/RAG-A + AI-FD-D12 子域注册粒度）。
/// </summary>
/// <remarks>
/// <para>
/// <b>子域注册粒度（P1D-1c）</b>：<see cref="AddFeishuTools"/> 为「引入全部域」便捷入口
/// （等价于逐域扩展全调）；宿主可按需 <see cref="AddFeishuBitableTools"/>/<see cref="AddFeishuImTools"/>/
/// <see cref="AddFeishuWriteTools"/> 等逐域装配——域客户端缺席时该域执行器与注册器均不注册
/// （<see cref="IFeishuToolDomainRegistrar"/>），工具不进注册表，白名单映射期 fail-fast 报「未注册」，
/// 错误面从启动崩溃收敛为白名单期明确报错。
/// </para>
/// <para>
/// 前置要求：宿主已注册 <c>FeishuAgentOptions</c>（<c>AddFeishuAgent</c>）与所需域的飞书核心客户端
/// （对应域的 <c>Add*Api()</c>）。授权钩子 <see cref="IToolExecutionAuthorizer"/> 与结果整形钩子
/// <see cref="IToolResultShaper"/> 由宿主按需注册（SDK 不内建策略）；写工具在
/// <c>EnforceToolAuthorization=true</c> 且未注册授权器时默认拒绝（安全默认）。
/// </para>
/// <para>
/// 白名单语义（读写分离，Phase 2 §4）：工具默认「收进注册表不启用」；只读工具经
/// <c>FeishuAgent:Tools</c>（<see cref="FeishuAgentOptions.Tools"/>）启用，写类工具
/// <b>单独键控</b>经 <c>FeishuAgent:WriteAllowList</c>（<see cref="FeishuAgentOptions.WriteAllowList"/>，
/// 默认空=不启用任何写工具）启用且必须过授权门禁。名单放错类别的工具名 fail-fast。
/// </para>
/// </remarks>
public static class FeishuToolsServiceCollectionExtensions
{
    /// <summary>
    /// 注册飞书工具包（全部域：只读 + 写类 + 附件上传；全部注册、默认不启用）。
    /// </summary>
    /// <remarks>「引入全部域」便捷入口，产物与逐域扩展全调等价（等价性由用例锁定）。</remarks>
    /// <param name="services">服务集合。</param>
    /// <param name="configure">注册表回调（可空；在配置白名单应用后执行，可再 <c>MapTool</c> 启用更多工具）。</param>
    /// <returns>服务集合。</returns>
    public static IServiceCollection AddFeishuTools(
        this IServiceCollection services,
        Action<FeishuToolRegistry>? configure = null)
        => AddFeishuWriteToolCores(AddFeishuReadonlyToolCores(AddFeishuToolInfrastructure(services, configure)));

    // R-8（B-4 处置）：原 `AddFeishuReadonlyTools` 是 `AddFeishuTools` 的**同义转发**
    // （方法名承诺"只读"，实现却注册含写工具链），名字与行为不符会误导宿主据名推断
    // "未引入写能力"。未发布窗口期零成本删除：全域入口只保留 `AddFeishuTools` 一个，
    // "写工具是否可用"由 `FeishuAgent:WriteAllowList` 白名单表达（安全默认不变）。

    /// <summary>
    /// 按域注册 Bitable 工具（P1D-1c）：4 个只读工具执行器 + 写执行器缺席。
    /// </summary>
    /// <remarks>要求宿主已注册 Bitable 三客户端；注册表单例只按首次注册生效（组合多域时回调只在首域传入）。</remarks>
    public static IServiceCollection AddFeishuBitableTools(
        this IServiceCollection services,
        Action<FeishuToolRegistry>? configure = null)
        => AddFeishuToolInfrastructure(services, configure).AddFeishuBitableToolsCore();

    /// <summary>
    /// 按【域组】注册云文档工具（docx + docxDeep + wiki + minutesRead + sheets + drive + driveComment + drivePermission + translation）。
    /// </summary>
    /// <remarks>
    /// F-3 / R1 §15.3：原 9 个细粒度入口合并为本入口——它们在飞书产品面同属"云文档域"，
    /// 权限与评论是 drive 的子能力。宿主只要子集时，用 <c>FeishuAgent:Tools</c> 白名单裁剪即可，
    /// 不必回到"一域一入口"的粒度（那正是本轮要收敛的公开面）。
    /// </remarks>
    public static IServiceCollection AddFeishuCloudDocTools(
        this IServiceCollection services,
        Action<FeishuToolRegistry>? configure = null)
        => AddFeishuToolInfrastructure(services, configure)
            .AddFeishuDocxToolsCore()
            .AddFeishuDocxDeepToolsCore()
            .AddFeishuWikiToolsCore()
            .AddFeishuMinutesReadToolsCore()
            .AddFeishuSheetsToolsCore()
            .AddFeishuDriveToolsCore()

            // ⚠️ 不要在此列 AddFeishuDriveCommentToolsCore / AddFeishuDrivePermissionToolsCore：
            // Drive 协作面是「读写混合域」，按既有链拆分其 Core **只在写链**登记（见 AddFeishuWriteToolCores）。
            // 两链同时登记 ⇒ 域注册器被装配两次 ⇒ FeishuToolRegistry 抛"工具已注册"（客户端缺席时不可见，
            // 宿主补齐客户端才整域炸掉——与 DocxDeep 同款陷阱）。
            .AddFeishuTranslationToolsCore();

    /// <summary>
    /// 按【域组】注册"目标与任务"工具（task + okr）。
    /// </summary>
    /// <remarks>F-3 / R1 §15.3：任务与 OKR 在飞书产品面同属"目标与任务"，合并为一个公开入口。</remarks>
    public static IServiceCollection AddFeishuTaskOkrTools(
        this IServiceCollection services,
        Action<FeishuToolRegistry>? configure = null)
        => AddFeishuToolInfrastructure(services, configure)
            .AddFeishuTaskToolsCore()
            .AddFeishuOkrToolsCore();

    /// <summary>
    /// 按域注册【安全合规】工具（2 个只读：<c>security.query_audit_logs</c> + <c>security.list_openapi_logs</c>）。
    /// </summary>
    /// <remarks>
    /// F-3：原 security 两个单工具入口合并为域入口（同一授权面、同一 PII 口径）。
    /// 两者均为 <b>PII 且默认不启用</b>，需宿主显式加入 <c>FeishuAgent:Tools</c> 白名单。
    /// </remarks>
    public static IServiceCollection AddFeishuSecurityTools(
        this IServiceCollection services,
        Action<FeishuToolRegistry>? configure = null)
        => AddFeishuToolInfrastructure(services, configure)
            .AddFeishuSecurityAuditLogToolsCore()
            .AddFeishuSecurityOpenApiLogToolsCore();




    /// <summary>
    /// 按【域组】注册"组织与人事"工具（mdm 国家/地区 + org 组织元数据 + hire 招聘）。
    /// </summary>
    /// <remarks>
    /// <para>
    /// F-3 / R1 §15.3（R-8 收尾）：原 3 个细粒度入口合并为本入口——三者在飞书产品面同属
    /// "组织人事主数据"（国家/地区参考数据、职务/职级/职务族/工作城市、招聘岗位与候选人），
    /// 且**共用同一授权面**（通讯录用户只读权限）。宿主只要子集时用 <c>FeishuAgent:Tools</c>
    /// 白名单裁剪，不必回到"一域一入口"的粒度。
    /// </para>
    /// <para>
    /// <b>PII 提示</b>：hire 候选人 / 面试 / Offer、以及 attendance 的流水查询默认不启用，
    /// 需宿主显式加白名单（DP-A5-1 档位 ①）；投影已白名单化（身份证号/性别/年龄/籍贯/学历/经历不返回）。
    /// </para>
    /// </remarks>
    public static IServiceCollection AddFeishuPeopleTools(
        this IServiceCollection services,
        Action<FeishuToolRegistry>? configure = null)
        => AddFeishuToolInfrastructure(services, configure)
            .AddFeishuCountryRegionToolsCore()
            .AddFeishuOrgMetadataToolsCore()
            .AddFeishuHireJobToolsCore()
            .AddFeishuHireJobRequirementToolsCore()
            .AddFeishuHireTalentToolsCore()
            .AddFeishuHireInterviewToolsCore()
            .AddFeishuHireOfferToolsCore();

    /// <summary>
    /// 按【域组】注册"会务与考勤"工具（attendance 考勤 + vc 视频会议）。
    /// </summary>
    /// <remarks>
    /// <para>
    /// F-3 / R1 §15.3（R-8 收尾）：两者在飞书产品面同属"效率套件"的<b>人与时间</b>管理面
    /// （出勤流水 / 会议预约与参会人），且都含 <b>user 身份</b>工具
    /// （<c>attendance.query_my_flow</c> / <c>vc.invite_participants</c> / <c>vc.end_meeting</c>）——
    /// 启用时必须在 <c>FeishuAgent:AllowedIdentities</c> 放行 <c>user</c>，否则装配期 fail-fast。
    /// </para>
    /// <para>
    /// ⚠️ attendance 的流水查询与 vc 的写工具默认不启用（PII / 副作用），需宿主显式加白名单。
    /// vc 的写工具（invite / end）按写面纪律经 <see cref="AddFeishuWriteTools"/> 登记。
    /// </para>
    /// </remarks>
    public static IServiceCollection AddFeishuMeetingTools(
        this IServiceCollection services,
        Action<FeishuToolRegistry>? configure = null)
        => AddFeishuToolInfrastructure(services, configure)
            .AddFeishuAttendanceToolsCore()
            .AddFeishuVcToolsCore();


    /// <summary>按域注册 Spark 妙搭工具（10 个：应用面 6 + 数据表面 4，R7/A6）。</summary>
    /// <remarks>
    /// <c>spark.create_app</c> / <c>patch_app</c> / <c>update_app_visibility</c> / 数据表写面为 <b>user 身份</b>工具，
    /// 启用时必须在 <c>FeishuAgent:AllowedIdentities</c> 放行 <c>user</c>，否则装配期 fail-fast。
    /// </remarks>
    public static IServiceCollection AddFeishuSparkTools(
        this IServiceCollection services,
        Action<FeishuToolRegistry>? configure = null)
        => AddFeishuToolInfrastructure(services, configure)
            .AddFeishuSparkAppToolsCore()
            .AddFeishuSparkTableToolsCore();


    /// <summary>按域注册 Search 工具（1 个只读）。</summary>
    public static IServiceCollection AddFeishuSearchTools(
        this IServiceCollection services,
        Action<FeishuToolRegistry>? configure = null)
        => AddFeishuToolInfrastructure(services, configure)
            .AddFeishuSearchToolsCore()
            .AddFeishuKnowledgeSearchToolsCore();

    /// <summary>按域注册 IM 工具（2 个只读；写工具经 <see cref="AddFeishuWriteTools"/>）。</summary>
    public static IServiceCollection AddFeishuImTools(
        this IServiceCollection services,
        Action<FeishuToolRegistry>? configure = null)
        => AddFeishuToolInfrastructure(services, configure)
            .AddFeishuImToolsCore()
            .AddFeishuAttachmentToolsCore()
            .AddFeishuBoardToolsCore();




    /// <summary>
    /// 按域注册通讯录工具（P0：<c>contact.resolve_user</c> / <c>contact.get_user</c> / <c>contact.batch_get</c>）。
    /// </summary>
    /// <remarks>要求宿主已注册 <c>IFeishuTenantV3User</c> 客户端；缺席时三工具不进注册表，白名单期 fail-fast。</remarks>
    public static IServiceCollection AddFeishuContactTools(
        this IServiceCollection services,
        Action<FeishuToolRegistry>? configure = null)
        => AddFeishuToolInfrastructure(services, configure)
            .AddFeishuContactToolsCore()

            // ⚠️ 同上：AddFeishuContactDepartmentToolsCore（list_departments / list_department_members）
            // 属「读写混合域」，其 Core **只在写链**登记，此处不得再列一次（重复装配会抛"工具已注册"）。
            // 本入口吸收的是 F-1 新增的部门查询执行器（上级链 / 按名搜索，纯只读）。
            .AddFeishuContactDepartmentQueryToolsCore();


    /// <summary>
    /// 按域注册邮件工具（WP5：<c>mail.list_messages</c>/<c>mail.get_message</c> 只读 + <c>mail.send_message</c> 写）。
    /// </summary>
    /// <remarks>
    /// 读侧（list/get）需 <c>IFeishuTenantV1MailMessage</c>（租户令牌），发信需 <c>IFeishuUserV1MailDraft</c>（用户令牌）——
    /// 两者均可软缺席（客户端缺席时对应工具不注册）。
    /// <para>
    /// ⚠️ <b>与全域/写域入口不可叠加</b>：mail 是「读写混合域」，其 Core（<c>AddFeishuMailToolsCore</c>）
    /// <b>只在写链</b>（<see cref="AddFeishuWriteToolCores"/>）登记；本入口再登记一次 ⇒ 同一个域注册器被装配两次 ⇒
    /// <c>FeishuToolRegistry</c> 抛"工具已注册"（与 CloudDoc 的评论/权限、Contact 的 contactDepartment 同款陷阱）。
    /// 宿主要么只调本入口，要么调 <see cref="AddFeishuTools"/>/<see cref="AddFeishuWriteTools"/>
    /// （后者同样包含 mail 读侧）——二者取一，再用 <c>FeishuAgent:Tools</c> 白名单裁剪。
    /// </para>
    /// </remarks>
    public static IServiceCollection AddFeishuMailTools(
        this IServiceCollection services,
        Action<FeishuToolRegistry>? configure = null)
        => AddFeishuToolInfrastructure(services, configure).AddFeishuMailToolsCore();


    /// <summary>
    /// 按域注册日历工具（WP5 / AT-F04：<c>calendar.create_event</c> 写 + <c>find_free_slots</c>/<c>list_events</c> 只读）。
    /// </summary>
    /// <remarks>要求宿主已注册 <c>IFeishuTenantV4CalendarEvent</c> 与 <c>IFeishuTenantV4Calendar</c>（AddCalendarApi）；缺席时整域不注册。</remarks>
    public static IServiceCollection AddFeishuCalendarTools(
        this IServiceCollection services,
        Action<FeishuToolRegistry>? configure = null)
        => AddFeishuToolInfrastructure(services, configure).AddFeishuCalendarToolsCore();



    /// <summary>
    /// 注册写域工具（Phase 2：<c>im.send_message</c>/<c>bitable.add_record</c>/<c>approval.create_instance</c> +
    /// 授权链复用）；对应域客户端缺席时对应写工具不进注册表，白名单期 fail-fast。
    /// </summary>
    public static IServiceCollection AddFeishuWriteTools(
        this IServiceCollection services,
        Action<FeishuToolRegistry>? configure = null)
        => AddFeishuWriteToolCores(AddFeishuToolInfrastructure(services, configure));

    // R-9（阶段 5）：原在本文件的 5 个**集成面**装配入口已随 Channels / Events / Knowledge 一并迁到
    // Mud.Feishu.AI（同命名空间下同名的 FeishuToolsServiceCollectionExtensions 曾承载它们）：
    //   AddFeishuEditMessageChannel / AddFeishuStreamingChannel / AddFeishuImConversationHandler /
    //   AddFeishuKnowledgeContext / AddFeishuAilyKnowledge —— 现落点 Mud.Feishu.AI/Extensions/FeishuAgentServiceCollectionExtensions.cs。
    // ⚠️ 不要在此处重新声明它们：这些入口注册的是集成面实现（流式通道 / 会话事件处理器 / 知识 Provider），
    // 而本包**不得**再引用 Mud.Feishu.EventCallback（迁移后已摘除该引用），重新声明会连带把事件 DTO 面拉回工具包。

    /// <summary>
    /// 只读域工具核心批量注册（W4 抽取：消除 AddFeishuTools 与 AddFeishuWriteTools 的重复链）。
    /// </summary>
    private static IServiceCollection AddFeishuReadonlyToolCores(IServiceCollection services)
        => services
            .AddFeishuBitableToolsCore()
            .AddFeishuDocxToolsCore()

            // A2 / Docx 深化：块级编辑 + Markdown 正文读取 + 群公告（读写混合域，按链拆分见 remarks）。
            .AddFeishuDocxDeepToolsCore()
            .AddFeishuWikiToolsCore()
            .AddFeishuSearchToolsCore()
            .AddFeishuImToolsCore()
            .AddFeishuSheetsToolsCore()
            .AddFeishuDriveToolsCore()
            .AddFeishuContactToolsCore()
            .AddFeishuCalendarToolsCore()
            .AddFeishuTaskToolsCore()
            .AddFeishuAttachmentToolsCore()
            .AddFeishuKnowledgeSearchToolsCore()

                    // R5 / F-11（S-13 修复）：minutes 域执行器 MinutesReadTools。
                    // ⚠️ 本行不是"登记一下"——**漏掉它会让 minutes.* 两个工具静默不入注册表**：
                    // 生成器照常产出 AddFeishuMinutesReadToolsCore（编译通过、无任何诊断），
                    // 但只有本清单会调用它。这张表是**人工维护**的，故已加守卫
                    // ToolDomainCores_ShouldAllBeWiredIntoTheAggregator 锁死"生成的 Core 必须被聚合"。
                    .AddFeishuMinutesReadToolsCore()

                    // F-1 首个补域：MDM 国家/地区执行器 CountryRegionTools（1 个只读）。
                    // 同 minutes 的教训——本行是**唯一**会调用生成 Core 的地方，
                    // 漏加即「工具静默不入注册表」（ToolDomainCoresWiringContractTests 会报红）。
                    .AddFeishuCountryRegionToolsCore()

                    // F-1 P0 批：Security 行为审计日志执行器 SecurityAuditLogTools（1 个只读，PII 默认不启用）。
                    // 同 minutes 的教训——本行是**唯一**会调用生成 Core 的地方，
                    // 漏加即「工具静默不入注册表」（ToolDomainCoresWiringContractTests 会报红）。
                    .AddFeishuSecurityAuditLogToolsCore()

                    // F-1 第三个补域：org 组织元数据执行器 OrgMetadataTools（8 个只读，非 PII）。
                    // 同 minutes 的教训——本行是**唯一**会调用生成 Core 的地方，
                    // 漏加即「工具静默不入注册表」（ToolDomainCoresWiringContractTests 会报红）。
                    .AddFeishuOrgMetadataToolsCore()

                    // F-1 收官：security 应用 API 调用日志执行器 SecurityOpenApiLogTools（1 个只读，PII）。
                    .AddFeishuSecurityOpenApiLogToolsCore()

                    // F-1 P0：contact 部门查询执行器 ContactDepartmentQueryTools（2 个只读，非 PII）。
                    .AddFeishuContactDepartmentQueryToolsCore()

                    // F-1 P0：hire 招聘岗位执行器 HireJobTools（2 个只读，非 PII）。
                    .AddFeishuHireJobToolsCore()

                    // hire 招聘需求执行器 HireJobRequirementTools（1 个只读，非 PII：投影已裁掉负责人/薪资）。
                    .AddFeishuHireJobRequirementToolsCore()

                    // hire 候选人执行器 HireTalentTools（2 个只读，**PII**：默认不启用，需宿主显式加白名单）。
                    .AddFeishuHireTalentToolsCore()

                    // hire 面试安排执行器 HireInterviewTools（1 个只读，**PII**：默认不启用）。
                    .AddFeishuHireInterviewToolsCore()

                    // hire Offer 执行器 HireOfferTools（1 个只读，**PII**：只给状态，结构性不含金额）。
                    .AddFeishuHireOfferToolsCore()

                    // R7 / A4：Board 画板域执行器 BoardTools（6 个工具：2 只读 + 4 写）。
                    // 同 minutes 的教训——本行是**唯一**会调用生成 Core 的地方，
                    // 漏加即「工具静默不入注册表」（ToolDomainCoresWiringContractTests 会报红）。
                    .AddFeishuBoardToolsCore()

                    // R7 / A5：Attendance 考勤域执行器 AttendanceTools（8 个工具：7 只读 + 1 写）。
                    // 与 Board 同属「单执行器混合域」（注册整类，写工具默认不启用）。
                    .AddFeishuAttendanceToolsCore()

                    // R7 / A6：Spark 妙搭域执行器 SparkAppTools（6）+ SparkTableTools（4）。
                    // 两个执行器类各出一枚 Core——**两行都不可漏**（漏一行即该域一半工具静默缺席）。
                    .AddFeishuSparkAppToolsCore()
                    .AddFeishuSparkTableToolsCore()

                    // R7 / C3：AI 文本面执行器 TranslationTools（2 个工具：翻译 / 语种识别，全只读）。
                    // 同 minutes 的教训——本行是**唯一**会调用生成 Core 的地方，
                    // 漏加即「工具静默不入注册表」（ToolDomainCoresWiringContractTests 会报红）。
                    .AddFeishuTranslationToolsCore()

                    // R6 / S2：OKR 域只读执行器 OkrTools（9 个工具）。
                    // 同 minutes 的教训——本行是**唯一**会调用生成 Core 的地方，
                    // 漏加即「工具静默不入注册表」（ToolDomainCoresWiringContractTests 会报红）。
                    .AddFeishuOkrToolsCore()

                    // R6 / S3：VideoConferencing 只读执行器 VcTools（6 个工具）。
                    .AddFeishuVcToolsCore()

                    // R6 / S5：运行时 schema 自省执行器 SchemaReadTools（feishu.schema_read，只读编译期目录）。
                    .AddFeishuSchemaReadToolsCore()
                    .AddFeishuCapabilityLookupToolsCore()
                    // B5：已策展工具检索执行器 ToolSearchTools（feishu.tool_search，只读注册表/契约表）。
                    .AddFeishuToolSearchToolsCore();

    /// <summary>
    /// 写域工具核心批量注册（W4 抽取：消除 AddFeishuTools 与 AddFeishuWriteTools 的重复链）。
    /// </summary>
    /// <remarks>
    /// R3-08：注意——<c>mail</c>（邮件）与 <c>contact-department</c>（通讯录部门轴）为<b>读写混合域</b>，
    /// 其读工具随写链注册（因为 <c>AddFeishuTools</c> 总是同时调用两链，行为正确）。
    /// 未来如按链拆分装配，须把这两个域的读执行器拆到 <see cref="AddFeishuReadonlyToolCores"/>。
    /// </remarks>
    private static IServiceCollection AddFeishuWriteToolCores(IServiceCollection services)
        => services
            // 写域按执行器拆成三个生成的 DI 核心方法（注册器按「执行器类」聚合，
            // 「跨 im/bitable/approval 三模块的写域」不再需要特例分支）。
            .AddFeishuMessageWriteToolsCore()
            .AddFeishuBitableWriteToolsCore()
            .AddFeishuApprovalWriteToolsCore()
            // WP2/R5 写入面补齐：docx/sheets/bitable(update/delete)/drive 写执行器
            .AddFeishuDocxWriteToolsCore()
            // ⚠️ 不要在此再列一次 AddFeishuDocxDeepToolsCore()：DocxDeepTools 是**单执行器混合域**
            // （读 5 + 写 4 由同一个生成 Core 整体注册，见 AddFeishuReadonlyToolCores 的登记）。
            // 两链同时登记会让该执行器的域注册器被装配两次 ⇒ 工具在注册表里重复注册
            // （FeishuToolRegistry.Register 直接抛"工具已注册"），而该缺陷在客户端缺席时**不可见**
            // （注册器解析失败被跳过），一旦宿主补齐客户端就整域炸掉。
            .AddFeishuSheetsWriteToolsCore()
            .AddFeishuBitableWriteRecordOpsCore()
            .AddFeishuDriveWriteToolsCore()
            // WP5/R5 域扩容：邮件工具 + 通讯录部门轴工具
            .AddFeishuMailToolsCore()
            .AddFeishuContactDepartmentToolsCore()

            // R6 / S2：OKR 域写入执行器 OkrWriteTools（6 个工具）。
            // 与 MailTools 同属「读写混合域」——按链拆分的取舍见 AddFeishuWriteToolCores 的 remarks。
            .AddFeishuOkrWriteToolsCore()

            // R6 / S3：VideoConferencing 写入执行器 VcWriteTools（4 个工具，其中含 2 个 user 身份工具）。
            .AddFeishuVcWriteToolsCore()

            // R-12（2026-10-10 评审决策）：万能兜底调用执行器 GenericApiTools（feishu.api_call）
            // 已**整条删除**。删除理由：① 与策展工具构成双轨调用路径（同一能力两条路径，
            // 投影/幂等/风险语义不同）；② 它是整个工具面风险最高的面（任意已登记方法的 HTTP 调度）；
            // ③ 零外部消费（Demos 未启用，仅测试覆盖）。未策展能力的正确处置是"如实告知用户"。
            // ⚠️ 删除后 AddFeishuGenericApiToolsCore 由生成器自动收敛（声明面消失即产物消失），
            // 本清单不得再引用它（否则域核心聚合守卫 ToolDomainCoresWiringContractTests 会红）。

            // MUDFT022：Drive 协作面执行器 DriveCommentTools（评论 4）+ DrivePermissionTools（权限 6）。
            // 与 MailTools 同属「读写混合域」（含只读的 list_comments / get_permission_public）——
            // 按链拆分的取舍见本方法 remarks。⚠️ 本行是**唯一**会调用这两个生成 Core 的地方，
            // 漏加即「工具静默不入注册表」（ToolDomainCoresWiringContractTests 会报红）。
            .AddFeishuDriveCommentToolsCore()
            .AddFeishuDrivePermissionToolsCore();

    /// <summary>执行链协作件 + 注册表 + 工具源桥（幂等；各域扩展共同前置）。</summary>
    private static IServiceCollection AddFeishuToolInfrastructure(
        IServiceCollection services,
        Action<FeishuToolRegistry>? configure)
    {
        // 执行链协作件。
        services.TryAddSingleton<IFeishuToolContextAccessor, FeishuToolContextAccessor>();
        // R3-5：实现零业务客户端依赖（只取单例 IAppContextHolder / IFeishuAppManager），
        // 故「只装 Bitable」等按域装配也能解析 FeishuToolBinding；同时消除 Singleton 捕获 Transient（TMA-13）。
        services.TryAddSingleton<IFeishuAppContextScopeFactory, FeishuAppContextScopeFactory>();
        services.TryAddSingleton<FeishuToolBinding>();

        // 注册表：全部已注册域的工具进表（不启用），白名单显式 Map（读写分离）。
        services.TryAddSingleton(sp => BuildRegistry(sp, configure));

        // 工具源桥：AddFeishuAgent 聚合全部工具源产出 ChatOptions.Tools。
        services.TryAddSingleton<FeishuAgentToolSource, FeishuToolsToolSource>();

        // 工具目录与 Schema 导出（P1D-4）：注册表之上的稳定契约（Phase 4 前置件）。
        services.TryAddSingleton<IToolCatalog>(static sp => FeishuToolCatalog.From(sp.GetRequiredService<FeishuToolRegistry>()));
        services.TryAddSingleton<IToolSchemaExporter, FeishuToolSchemaExporter>();

        // R-3 / F-8：Skills 产物读取接缝。与导出接口同源（编译期常量），但面向"按域取一篇"——
        // 此前 skills/** 随包分发却零仓内消费者，"包内产物 == 读取内容"没有任何路径验证过。
        services.TryAddSingleton<ISkillDocumentReader, FeishuSkillDocumentReader>();

        // R-7 窄接缝：实现类型（FeishuToolAIFunction / FeishuToolBinding）已退出公开面，
        // 周边包（Mud.Feishu.AI.Mcp）改经这两条接缝消费，故必须在此注册——缺注册会让
        // MCP 的 tools/list 静默变空（接缝解析失败被当作"没有工具"）。
        services.TryAddSingleton<IFeishuToolFunctionFactory, FeishuToolFunctionFactory>();
        services.TryAddSingleton<IToolErrorTextFormatter, ToolErrorTextFormatter>();

        // T1 / D1：用户令牌就绪探测器（internal sealed，按需注入 FeishuToolBinding 构造函数）。
        // IFeishuTokenManagerResolver 可空——未注册时探测降级为 fail-open 放行（由装配诊断 Warning 提示）。
        // ⚠️ 必须是**工厂注册**：MS DI 不把可空引用注解当作"可缺席"，只有工厂（或带默认值的构造参数）
        //    才能在服务缺失时传入 null。若改回 TryAddSingleton<UserTokenPrecheck>()，未注册
        //    IFeishuTokenManagerResolver 的宿主（按域装配 + 全部既有用例）会在构建注册表时崩在激活期，
        //    "fail-open 放行"这条语义随之消失。
        services.TryAddSingleton(static sp => new UserTokenPrecheck(
            sp.GetService<IFeishuTokenManagerResolver>(),
            sp.GetRequiredService<IOptions<FeishuAgentOptions>>(),
            sp.GetService<ILogger<UserTokenPrecheck>>()));

        return services;
    }

    /// <summary>
    /// 按域注册能力出处元工具：<c>feishu.capability_lookup</c>（默认不启用）。
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>不放进基础设施层（有意）</b>：该方法在每公开入口都会被调用一次，若在其中登记域注册器，
    /// ① 逐域入口（如 <c>AddFeishuBitableTools</c>）会额外得到非本域工具，破坏"单域注册只含该域"的既有契约；
    /// ② 重复调用会产生多个注册器实例，运行期报"工具已注册"。
    /// 故本元工具是一个<b>显式入口</b>：全域入口 <see cref="AddFeishuTools"/> 已包含它，
    /// 只装配单个域的宿主如需"能力出路"可单独调用本方法。
    /// </para>
    /// <para>
    /// 数据源是编译期常量（能力目录 + 工具名契约表），<b>不依赖任何飞书客户端</b>——不随域缺席而软缺席。
    /// </para>
    /// </remarks>
    /// <param name="services">服务集合。</param>
    /// <param name="configure">注册表回调（可空）。</param>
    /// <returns>服务集合。</returns>
    public static IServiceCollection AddFeishuCapabilityTools(
        this IServiceCollection services,
        Action<FeishuToolRegistry>? configure = null)
        => AddFeishuToolInfrastructure(services, configure)
            .AddFeishuCapabilityLookupToolsCore()
            .AddFeishuToolSearchToolsCore();

    /// <summary>
    /// 注册运行时 schema 自省工具（<c>feishu.schema_read</c>，R6 / S5）。
    /// </summary>
    /// <remarks>
    /// <para>
    /// 与 <see cref="AddFeishuCapabilityTools"/> 同属"元工具显式入口"：数据源是编译期方法目录
    /// （<c>FeishuToolMethodCatalog</c>，1268 个方法的结构化事实），<b>不依赖任何飞书客户端</b>，
    /// 也不随域缺席而软缺席。全域入口 <see cref="AddFeishuTools"/> 已包含它。
    /// </para>
    /// <para>
    /// <b>本工具只读、不提供任何调用通道</b>（R-12：万能兜底 <c>feishu.api_call</c> 已整条删除）——
    /// 它回答"这个方法怎么调"，但查到的未策展方法<b>无法执行</b>，正确处置是如实告知用户。
    /// </para>
    /// </remarks>
    public static IServiceCollection AddFeishuSchemaReadTools(
        this IServiceCollection services,
        Action<FeishuToolRegistry>? configure = null)
        => AddFeishuToolInfrastructure(services, configure).AddFeishuSchemaReadToolsCore();

    private static FeishuToolRegistry BuildRegistry(IServiceProvider sp, Action<FeishuToolRegistry>? configure)
    {
        var registry = new FeishuToolRegistry();

        // 域注册器扫描（P1D-1c）：执行器缺席的域（工厂返回 null）不产生注册器，对应工具不进表。
        // 注：IEnumerable 解析会包含工厂返回 null 的描述符，需显式跳过。
        foreach (var registrar in sp.GetServices<IFeishuToolDomainRegistrar>())
        {
            if (registrar is null)
            {
                continue;
            }

            registrar.Register(registry);
        }

        // 配置面白名单（读写分离；未知名字/放错类别 fail-fast——工具名是模型可见契约）。
        var options = sp.GetRequiredService<IOptions<FeishuAgentOptions>>().Value;
        foreach (var name in options.Tools)
        {
            MapWhitelist(registry, name, expectWrite: false, FeishuAgentOptions.SectionName, nameof(FeishuAgentOptions.Tools));
        }

        foreach (var name in options.WriteAllowList)
        {
            MapWhitelist(registry, name, expectWrite: true, FeishuAgentOptions.SectionName, nameof(FeishuAgentOptions.WriteAllowList));
        }

        configure?.Invoke(registry);

        // R-4 / F-13：高危写（删除 / 撤回）**已实际启用**时的装配期告警。
        // 位置说明：必须在 configure 回调之后——回调可以 MapTool 启用更多工具，
        // 放在白名单映射之后就漏掉"经回调启用"这条路径。
        WarnOnHighRiskWriteEnablement(sp, registry);

        // 身份白名单的装配期 fail-fast（T2-4 / 决策 D-1 ⓑ，R4.1 评审 R-3）：白名单
        // （Tools / WriteAllowList / configure 回调）启用了 identity 不在 AllowedIdentities
        // 闭集内的工具时，启动即抛可读异常——而不是等运行期被策略轴逐请求拒绝
        // （policy_denied: identity_mismatch，宿主会误判为权限问题）。
        // 落点说明：FeishuAgentOptions.Validate() 感知不到工具面（身份在注册表、
        // 白名单映射在装配层），故该校验只能在注册表构建完成后进行。
        var identityViolations = registry.EnabledTools
            .Where(t => !options.AllowedIdentities.Contains(t.Identity, StringComparer.Ordinal))
            .ToArray();
        if (identityViolations.Length > 0)
        {
            throw new InvalidOperationException(
                $"以下已启用工具的身份不在 {FeishuAgentOptions.SectionName}:{nameof(FeishuAgentOptions.AllowedIdentities)} "
                + $"闭集 [{string.Join(",", options.AllowedIdentities)}] 内："
                + string.Join("、", identityViolations.Select(static t => $"{t.Name}(identity={t.Identity})"))
                + "——请把对应身份加入 AllowedIdentities，或从白名单移除这些工具（装配期 fail-fast，防运行期静默拒绝）");
        }

        // T1.1 装配期接线诊断：已启用 user 身份工具时，校验运行时依赖是否就绪。
        // 落点在 identityViolations 之后——只有 identity 校验通过（user 在闭集内）才需要检查接线。
        DiagnoseUserToolWiring(sp, registry, options);

        return registry;
    }

    /// <summary>
    /// R-4 / F-13：<b>高危写（删除 / 撤回）能力受控启用</b>的装配期告警。
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>为什么是"告知"而不是"开关"</b>：启用/禁用删除的真相源唯一——
    /// <c>FeishuAgent:WriteAllowList</c>（列名启用）与 <c>FeishuAgent:MaxToolRisk</c>（`write` 一键拒掉全部
    /// <c>high-risk-write</c>）。再引入一个"删除开关"会形成第二个启用源（R5：同一语义只能有一个配置节）。
    /// 本方法补的是<b>可观测性</b>：把"静默往白名单里加了删除工具"升级为"装配期显式告知"。
    /// </para>
    /// <para>
    /// <b>为什么只 Warning 不阻断</b>：强制力来自 <c>IToolExecutionAuthorizer</c> 与 HITL（写工具默认
    /// fail-closed），日志只负责让运维/宿主在启动那一刻看见。把它做成异常会让"合法启用删除"的宿主
    /// 无法启动——那是把安全默认偷换成"禁止使用"。
    /// </para>
    /// <para>
    /// 日志是<b>可选</b>协作件（宿主未 <c>AddLogging</c> 时 <see cref="ILoggerFactory"/> 不存在）——
    /// 告警不得把装配变成失败源。
    /// </para>
    /// </remarks>
    private static void WarnOnHighRiskWriteEnablement(IServiceProvider sp, FeishuToolRegistry registry)
    {
        var highRisk = registry.EnabledTools
            .Where(static t => t.Risk == FeishuToolRisk.HighRiskWrite)
            .Select(static t => t.Name)
            .OrderBy(static n => n, StringComparer.Ordinal)
            .ToArray();

        if (highRisk.Length == 0)
        {
            return;
        }

        sp.GetService<ILoggerFactory>()?.CreateLogger("Mud.Feishu.AI.Tools").LogWarning(
            "装配期告警：已启用 {Count} 个高危写工具（{RiskLiteral}，不可撤销）：{Tools}。"
            + "它们默认全部关闭，仅因出现在 {Section}:{AllowList} 而启用——请确认："
            + "① 已注册 IToolExecutionAuthorizer 且对这些工具返回 Allowed（未注册时写工具默认拒绝）；"
            + "② 如需一键收回，把 {Section}:{MaxRisk} 设为 'write'（或更低），全部高危写工具会被 risk_exceeded 拒绝。",
            highRisk.Length,
            FeishuToolRiskNames.HighRiskWrite,
            string.Join("、", highRisk),
            FeishuAgentOptions.SectionName,
            nameof(FeishuAgentOptions.WriteAllowList),
            FeishuAgentOptions.SectionName,
            nameof(FeishuAgentOptions.MaxToolRisk));
    }

    /// <summary>
    /// T1.1 装配期接线诊断：已启用 user 身份工具时，校验运行时依赖是否就绪。
    /// </summary>
    /// <remarks>
    /// <para>
    /// 当白名单启用了 <c>identity == user</c> 的工具时，以下三个依赖必须在装配期就绪：
    /// <list type="number">
    /// <item><c>IFeishuCurrentUserContext</c> 已注册——执行链需要它写入当前用户（令牌缓存查找键来源）。</item>
    /// <item><c>IFeishuTokenManagerResolver</c> 已注册——用户令牌探测器需要它解析用户令牌管理器。</item>
    /// <item><c>UserTokenPrecheck</c> 模式非 <c>Off</c>——如果 <c>Off</c>，则前置探测被关闭，用户令牌就绪语义不生效。</item>
    /// </list>
    /// </para>
    /// <para>
    /// <b>为什么是 Warning 而不是 fail-fast</b>：user 身份工具的接线缺失会在运行期被
    /// <c>FeishuToolBinding</c> 逐请求拦截（返回 <c>authorization_denied</c>），装配期诊断是
    /// <b>可观测性增强</b>——把"运行期逐请求报错"提前到"启动时一次性告知"。强制异常会让
    /// "合法启用 user 工具但宿主尚未完成 OAuth 接入"的场景无法启动。
    /// </para>
    /// </remarks>
    private static void DiagnoseUserToolWiring(
        IServiceProvider sp, FeishuToolRegistry registry, FeishuAgentOptions options)
    {
        // 只在启用了 user 身份工具时触发。
        var hasUserTools = registry.EnabledTools
            .Any(static t => string.Equals(t.Identity, FeishuToolIdentityNames.User, StringComparison.Ordinal));
        if (!hasUserTools)
        {
            return;
        }

        var logger = sp.GetService<ILoggerFactory>()?.CreateLogger("Mud.Feishu.AI.Tools");

        // 检查 1：IFeishuCurrentUserContext 已注册。
        var currentUserContext = sp.GetService<IFeishuCurrentUserContext>();
        if (currentUserContext is null)
        {
            logger?.LogWarning(
                "装配期诊断：已启用 user 身份工具，但 IFeishuCurrentUserContext 未注册——"
                + "运行期每次调用 user 工具都会返回 authorization_denied。"
                + "请注册 IFeishuCurrentUserContext（AddFeishu() 默认已注册；自建管线请手动注册）");
        }

        // 检查 2：IFeishuTokenManagerResolver 已注册。
        var resolver = sp.GetService<IFeishuTokenManagerResolver>();
        if (resolver is null)
        {
            logger?.LogWarning(
                "装配期诊断：已启用 user 身份工具，但 IFeishuTokenManagerResolver 未注册——"
                + "用户令牌探测器将 fail-open 放行（由下游 401 + TokenRecoveryExecutor 兜底）。"
                + "如需前置 user_auth_required 语义，请注册 IFeishuTokenManagerResolver（AddFeishu() 默认已注册）");
        }
        else if (options.UserTokenPrecheck is not UserTokenPrecheckModes.Off)
        {
            // 检查 3：默认应用的 UserTokenManager 可达（仅 resolver 已注册且探测非 Off 时检查）。
            // 用 ConfiguredAppKeys 取第一个已配置 appKey 做探测——不触发实例化（TMA-08）。
            var appManager = sp.GetService<IFeishuAppManager>();
            if (appManager is not null)
            {
                foreach (var appKey in appManager.ConfiguredAppKeys)
                {
                    var userTokenManager = resolver.TryGetUserTokenManager(appKey);
                    if (userTokenManager is null)
                    {
                        logger?.LogWarning(
                            "装配期诊断：应用 '{AppKey}' 的用户令牌管理器不可达（TryGetUserTokenManager 返回 null）——"
                            + "可能原因：应用未初始化 / 已退休 / 未配置 OAuth 凭据。"
                            + "该应用上的 user 身份工具探测将 fail-open 放行", appKey);
                    }
                    break; // 只检查第一个已配置应用
                }
            }
        }

        // 检查 4：UserTokenPrecheck 模式为 Off 时的告知。
        if (options.UserTokenPrecheck is UserTokenPrecheckModes.Off)
        {
            logger?.LogWarning(
                "装配期诊断：已启用 user 身份工具，但 {Section}:{Property} = 'off'——"
                + "用户令牌前置探测被关闭，user_auth_required 语义不生效（改造前行为：由下游 401 兜底）",
                FeishuAgentOptions.SectionName,
                nameof(FeishuAgentOptions.UserTokenPrecheck));
        }
    }

    private static void MapWhitelist(
        FeishuToolRegistry registry, string name, bool expectWrite, string section, string propertyName)
    {
        if (!registry.TryGet(name, out var definition) || definition is null)
        {
            throw new InvalidOperationException(
                $"{section}:{propertyName} 白名单包含未注册工具 '{name}'——工具名是模型可见契约，请对照工具契约表修正（域客户端缺席时对应工具不注册）");
        }

        if (definition.IsWrite == expectWrite)
        {
            registry.MapTool(name);
            return;
        }

        throw expectWrite
            ? new InvalidOperationException(
                $"{section}:{propertyName}（写类白名单）不能包含只读工具 '{name}'——读写白名单分离，只读工具请经 FeishuAgent:Tools 启用")
            : new InvalidOperationException(
                $"{section}:{propertyName}（只读白名单）不能包含写类工具 '{name}'——写工具必须经 FeishuAgent:WriteAllowList 单独键控且过授权门禁（安全默认）");
    }
}
