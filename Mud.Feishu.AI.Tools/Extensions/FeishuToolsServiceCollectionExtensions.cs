// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Mud.Feishu.AI.Channels;
using Mud.Feishu.AI.Events;
using Mud.Feishu.AI.Knowledge;
using Mud.Feishu.AI.Tools.Registration;
using Mud.Feishu.AI.Tools.Tools;

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

    /// <summary>按域注册 Docx 工具（基础 2 个只读 + R7/A2 深化 9 个：读 5 + 写 4）。</summary>
    /// <remarks>
    /// 两个 Core 都必须在此登记：<c>AddFeishuDocxToolsCore</c>（基础只读）+
    /// <c>AddFeishuDocxDeepToolsCore</c>（块级编辑 / Markdown 正文 / 群公告，单执行器混合域）。
    /// 这也保证"逐域扩展 == 全域入口"的等价性（DomainRegistrarTests 断言）。
    /// </remarks>
    public static IServiceCollection AddFeishuDocxTools(
        this IServiceCollection services,
        Action<FeishuToolRegistry>? configure = null)
        => AddFeishuToolInfrastructure(services, configure)
            .AddFeishuDocxToolsCore()
            .AddFeishuDocxDeepToolsCore();

    /// <summary>按域注册 Wiki 工具（2 个只读）。</summary>
    public static IServiceCollection AddFeishuWikiTools(
        this IServiceCollection services,
        Action<FeishuToolRegistry>? configure = null)
        => AddFeishuToolInfrastructure(services, configure).AddFeishuWikiToolsCore();

    /// <summary>按域注册 Minutes 工具（5 个只读：get/get_artifacts/search/get_statistics/get_media，R5/F-11 + R7/A3）。</summary>
    /// <remarks>
    /// 与 <see cref="AddFeishuReadonlyToolCores"/> 中的登记**成对存在**：
    /// 少任何一处都会让 minutes 工具在对应入口下静默缺席（S-13）。
    /// </remarks>
    public static IServiceCollection AddFeishuMinutesReadTools(
        this IServiceCollection services,
        Action<FeishuToolRegistry>? configure = null)
        => AddFeishuToolInfrastructure(services, configure).AddFeishuMinutesReadToolsCore();

    /// <summary>按域注册 AI 文本面工具（2 个只读：ai.translate_text / ai.detect_language，R7/C3）。</summary>
    /// <remarks>
    /// 与 <see cref="AddFeishuReadonlyToolCores"/> 中的登记**成对存在**：
    /// 少任何一处都会让 ai.* 工具在对应入口下静默缺席（S-13）。
    /// 需宿主已启用飞书 AI 翻译能力（<c>IFeishuTenantV1AITranslation</c>）——缺席时本域工具软缺席。
    /// </remarks>
    public static IServiceCollection AddFeishuTranslationTools(
        this IServiceCollection services,
        Action<FeishuToolRegistry>? configure = null)
        => AddFeishuToolInfrastructure(services, configure).AddFeishuTranslationToolsCore();

    /// <summary>按域注册 Board 画板工具（2 个只读 + 4 个写，R7/A4）。</summary>
    /// <remarks>
    /// 与 <see cref="AddFeishuReadonlyToolCores"/> 中的登记**成对存在**：
    /// 少任何一处都会让 board 工具在对应入口下静默缺席（S-13 同款失败形态）。
    /// </remarks>
    public static IServiceCollection AddFeishuBoardTools(
        this IServiceCollection services,
        Action<FeishuToolRegistry>? configure = null)
        => AddFeishuToolInfrastructure(services, configure).AddFeishuBoardToolsCore();

    /// <summary>按域注册 Attendance 考勤工具（7 个只读 + 1 个写，R7/A5）。</summary>
    /// <remarks>
    /// ⚠️ <c>attendance.query_user_flow</c> / <c>attendance.get_flow</c> 是 <b>PII 敏感</b>且<b>默认不启用</b>
    /// （DP-A5-1 档位 ①）——宿主须显式加白名单；<c>attendance.query_my_flow</c> 是 <b>user 身份</b>工具，
    /// 启用时必须在 <c>FeishuAgent:AllowedIdentities</c> 放行 <c>user</c>，否则装配期 fail-fast。
    /// </remarks>
    public static IServiceCollection AddFeishuAttendanceTools(
        this IServiceCollection services,
        Action<FeishuToolRegistry>? configure = null)
        => AddFeishuToolInfrastructure(services, configure).AddFeishuAttendanceToolsCore();

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

    /// <summary>按域注册 OKR 只读工具（9 个只读，R6/S2；写工具经 <see cref="AddFeishuWriteTools"/>）。</summary>
    /// <remarks>
    /// 与 <see cref="AddFeishuReadonlyToolCores"/> 中的登记**成对存在**：
    /// 少任何一处都会让 okr 只读工具在对应入口下静默缺席（S-13 的同款失败形态）。
    /// </remarks>
    public static IServiceCollection AddFeishuOkrTools(
        this IServiceCollection services,
        Action<FeishuToolRegistry>? configure = null)
        => AddFeishuToolInfrastructure(services, configure).AddFeishuOkrToolsCore();

    /// <summary>按域注册 VideoConferencing 只读工具（6 个只读，R6/S3；写工具经 <see cref="AddFeishuWriteTools"/>）。</summary>
    /// <remarks>
    /// 与 <see cref="AddFeishuReadonlyToolCores"/> 中的登记**成对存在**（S-13 的同款失败形态）。
    /// 注意：<c>vc.invite_participants</c> / <c>vc.end_meeting</c> 是 <b>user 身份</b>工具，
    /// 宿主启用它们时必须在 <c>FeishuAgent:AllowedIdentities</c> 放行 <c>user</c>，否则装配期 fail-fast。
    /// </remarks>
    public static IServiceCollection AddFeishuVcTools(
        this IServiceCollection services,
        Action<FeishuToolRegistry>? configure = null)
        => AddFeishuToolInfrastructure(services, configure).AddFeishuVcToolsCore();

    /// <summary>按域注册 Search 工具（1 个只读）。</summary>
    public static IServiceCollection AddFeishuSearchTools(
        this IServiceCollection services,
        Action<FeishuToolRegistry>? configure = null)
        => AddFeishuToolInfrastructure(services, configure).AddFeishuSearchToolsCore();

    /// <summary>按域注册 IM 工具（2 个只读；写工具经 <see cref="AddFeishuWriteTools"/>）。</summary>
    public static IServiceCollection AddFeishuImTools(
        this IServiceCollection services,
        Action<FeishuToolRegistry>? configure = null)
        => AddFeishuToolInfrastructure(services, configure).AddFeishuImToolsCore();

    /// <summary>按域注册 Sheets 工具（2 个只读）。</summary>
    public static IServiceCollection AddFeishuSheetsTools(
        this IServiceCollection services,
        Action<FeishuToolRegistry>? configure = null)
        => AddFeishuToolInfrastructure(services, configure).AddFeishuSheetsToolsCore();

    /// <summary>按域注册 Drive 工具（2 个只读，AI-FD-D12 P1D-1b 批次 A 新域）。</summary>
    /// <remarks>
    /// 评论/权限协作面（drive.list_comments / add_comment / reply_comment / resolve_comment /
    /// get_permission_public / update_permission_public / grant_permission / update_permission_member /
    /// remove_permission / transfer_owner）由 <see cref="AddFeishuDriveCollabTools"/> 单独装配——
    /// 与 <see cref="AddFeishuReadonlyToolCores"/> 中的登记**成对存在**（S-13 的同款失败形态）。
    /// </remarks>
    public static IServiceCollection AddFeishuDriveTools(
        this IServiceCollection services,
        Action<FeishuToolRegistry>? configure = null)
        => AddFeishuToolInfrastructure(services, configure).AddFeishuDriveToolsCore();

    /// <summary>
    /// 按域注册 Drive 协作面工具（评论 4 + 权限 6，MUDFT022 批次）：只读 3 + 写 7。
    /// </summary>
    /// <remarks>
    /// <c>DriveCommentTools</c> / <c>DrivePermissionTools</c> 与 MailTools 同属「读写混合域」——
    /// 两个 Core 整体挂写链（<see cref="AddFeishuWriteToolCores"/>），按链拆分的取舍见其 remarks。
    /// 权限域工具被引擎风险分级一律 <c>high-risk-write</c>（含底层 GET 的 get_permission_public，
    /// MUDFT017），启用须过 <c>WriteAllowList</c> 键控 + 授权门禁。
    /// </remarks>
    public static IServiceCollection AddFeishuDriveCollabTools(
        this IServiceCollection services,
        Action<FeishuToolRegistry>? configure = null)
        => AddFeishuToolInfrastructure(services, configure)
            .AddFeishuDriveCommentToolsCore()
            .AddFeishuDrivePermissionToolsCore();

    /// <summary>
    /// 按域注册通讯录工具（P0：<c>contact.resolve_user</c> / <c>contact.get_user</c> / <c>contact.batch_get</c>）。
    /// </summary>
    /// <remarks>要求宿主已注册 <c>IFeishuTenantV3User</c> 客户端；缺席时三工具不进注册表，白名单期 fail-fast。</remarks>
    public static IServiceCollection AddFeishuContactTools(
        this IServiceCollection services,
        Action<FeishuToolRegistry>? configure = null)
        => AddFeishuToolInfrastructure(services, configure).AddFeishuContactToolsCore();

    /// <summary>按域注册 Knowledge 工具（<c>knowledge.search</c>；要求宿主已注册 <see cref="IRetriever"/> 实现，如 <c>AddFeishuAilyKnowledge</c>）。</summary>
    public static IServiceCollection AddFeishuKnowledgeTools(
        this IServiceCollection services,
        Action<FeishuToolRegistry>? configure = null)
        => AddFeishuToolInfrastructure(services, configure).AddFeishuKnowledgeSearchToolsCore();

    /// <summary>
    /// 按域注册邮件工具（WP5：<c>mail.list_messages</c>/<c>mail.get_message</c> 只读 + <c>mail.send_message</c> 写）。
    /// </summary>
    /// <remarks>
    /// 读侧（list/get）需 <c>IFeishuTenantV1MailMessage</c>（租户令牌），发信需 <c>IFeishuUserV1MailDraft</c>（用户令牌）——
    /// 两者均可软缺席（客户端缺席时对应工具不注册）。
    /// </remarks>
    public static IServiceCollection AddFeishuMailTools(
        this IServiceCollection services,
        Action<FeishuToolRegistry>? configure = null)
        => AddFeishuToolInfrastructure(services, configure).AddFeishuMailToolsCore();

    /// <summary>
    /// 按域注册通讯录部门轴工具（WP5：<c>contact.list_departments</c>/<c>contact.list_department_members</c>）。
    /// </summary>
    /// <remarks>
    /// 部门列表需 <c>IFeishuTenantV3Departments</c>，部门成员需 <c>IFeishuTenantV1Employees</c>——
    /// 两者均可软缺席（客户端缺席时对应工具不注册）。
    /// </remarks>
    public static IServiceCollection AddFeishuContactDepartmentTools(
        this IServiceCollection services,
        Action<FeishuToolRegistry>? configure = null)
        => AddFeishuToolInfrastructure(services, configure).AddFeishuContactDepartmentToolsCore();

    /// <summary>
    /// 按域注册日历工具（WP5 / AT-F04：<c>calendar.create_event</c> 写 + <c>find_free_slots</c>/<c>list_events</c> 只读）。
    /// </summary>
    /// <remarks>要求宿主已注册 <c>IFeishuTenantV4CalendarEvent</c> 与 <c>IFeishuTenantV4Calendar</c>（AddCalendarApi）；缺席时整域不注册。</remarks>
    public static IServiceCollection AddFeishuCalendarTools(
        this IServiceCollection services,
        Action<FeishuToolRegistry>? configure = null)
        => AddFeishuToolInfrastructure(services, configure).AddFeishuCalendarToolsCore();

    /// <summary>
    /// 按域注册任务工具（WP5 / AT-F17：<c>task.create_task</c> 写 + <c>task.list_my_tasks</c> <b>user 身份</b>）。
    /// </summary>
    /// <remarks>
    /// tenant 客户端（<c>IFeishuTenantV2Task</c>）缺席 → 整域不注册；user 客户端
    /// （<c>IFeishuUserV2Task</c>）缺席 → 仅 <c>list_my_tasks</c> 缺席（工具级软缺席）。
    /// <c>list_my_tasks</c> 还要求宿主在执行上下文提供当前用户（<c>FeishuToolContext.UserId</c>）。
    /// </remarks>
    public static IServiceCollection AddFeishuTaskTools(
        this IServiceCollection services,
        Action<FeishuToolRegistry>? configure = null)
        => AddFeishuToolInfrastructure(services, configure).AddFeishuTaskToolsCore();

    /// <summary>
    /// 按域注册附件上传工具（WP7：<c>im.send_image</c> / <c>im.send_file</c>，三步链路 落盘→上传→发送）。
    /// </summary>
    /// <remarks>
    /// 要求宿主注册 <c>IFeishuAttachmentStager</c> 与 <c>IFeishuTenantV1Message</c>；
    /// <b>落盘器缺席 → 两个工具不注册</b>（软缺席：SDK 不实现下载/落盘，见 U-5）。
    /// </remarks>
    public static IServiceCollection AddFeishuAttachmentTools(
        this IServiceCollection services,
        Action<FeishuToolRegistry>? configure = null)
        => AddFeishuToolInfrastructure(services, configure).AddFeishuAttachmentToolsCore();

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

        // R-7 窄接缝：实现类型（FeishuToolAIFunction / FeishuToolBinding）已退出公开面，
        // 周边包（Mud.Feishu.AI.Mcp）改经这两条接缝消费，故必须在此注册——缺注册会让
        // MCP 的 tools/list 静默变空（接缝解析失败被当作"没有工具"）。
        services.TryAddSingleton<IFeishuToolFunctionFactory, FeishuToolFunctionFactory>();
        services.TryAddSingleton<IToolErrorTextFormatter, ToolErrorTextFormatter>();

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
    /// （<c>FeishuToolMethodCatalog</c>，1228 个方法的结构化事实），<b>不依赖任何飞书客户端</b>，
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

        return registry;
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
