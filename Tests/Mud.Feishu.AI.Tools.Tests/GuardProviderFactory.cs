// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！
// 任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.AI.Tools.Tests;

/// <summary>守卫/目录测试共用的最小容器工厂（分域 mock 齐备）。</summary>
/// <remarks>
/// 从 <c>Tools/Catalog/FeishuToolCatalogTests.cs</c> 提取至项目根命名空间（R-1+2c 测试镜像重组）：
/// 本类型被 <c>ContractGuards/</c> 与多个关注面测试共同消费，住在任一关注面目录都会让
/// 另一侧经 using 形成隐性耦合；放在 <c>Mud.Feishu.AI.Tools.Tests</c> 后，
/// <c>Tests.Tools.*</c> 与 <c>Tests.ContractGuards</c> 两个子命名空间均可经命名空间向上查找解析，
/// 无需 using。
/// </remarks>
internal static class GuardProviderFactory
{
    /// <param name="configureOptions">配置覆盖。</param>
    /// <param name="withAttachmentStager">
    /// 是否注册附件落盘器（WP7）：<see langword="false"/> 用于验证"落盘器缺席 → 上传工具软缺席"。
    /// </param>
    /// <param name="withUserTaskClient">
    /// 是否注册用户令牌任务客户端：<see langword="false"/> 用于验证"<b>可空</b> SDK 客户端缺席
    /// <b>不</b>触发软缺席"（<c>TaskTools</c> 的 <c>IFeishuUserV2Task?</c> 是可选依赖，
    /// 缺席时 <c>task.list_my_tasks</c> 仍在位、由执行期回填结构化错误）。
    /// </param>
    internal static ServiceProvider CreateProvider(
        Action<FeishuAgentOptions> configureOptions,
        bool withAttachmentStager = true,
        bool withUserTaskClient = true)
    {
        var options = new FeishuAgentOptions { Instructions = "test" };
        configureOptions(options);

        var services = new ServiceCollection()
            .AddSingleton(Options.Create(options))
            // R3-5：作用域工厂的核心依赖（与生成的 HTTP 客户端同构：IFeishuAppManager + IAppContextHolder
            // + 授权器），与业务域无关——任何装配组合都必须提供，否则连注册表都建不起来。
            .AddSingleton(new Mock<Mud.HttpUtils.IAppContextHolder>().Object)
            .AddSingleton(new Mock<Mud.Feishu.Abstractions.IFeishuAppManager>().Object)
            .AddSingleton(Mock.Of<Mud.HttpUtils.IAppAccessAuthorizer>(a => a.CanSwitchTo(It.IsAny<string>())))
            .AddSingleton(new Mock<Mud.Feishu.IFeishuTenantV1BitableAppTable>().Object)
            .AddSingleton(new Mock<Mud.Feishu.IFeishuTenantV1BitableField>().Object)
            .AddSingleton(new Mock<Mud.Feishu.IFeishuTenantV1BitableRecord>().Object)
            // T-2：Bitable 深化执行器 4 个客户端（必选依赖，缺席 ⇒ form/dashboard/workflow/role
            // 共 17 个深化工具静默缺席，全量契约断言随之落空）。
            .AddSingleton(new Mock<Mud.Feishu.IFeishuTenantV1BitableForm>().Object)
            .AddSingleton(new Mock<Mud.Feishu.IFeishuTenantV1BitableDashboard>().Object)
            .AddSingleton(new Mock<Mud.Feishu.Interfaces.IFeishuTenantV1BitableWorkflow>().Object)
            .AddSingleton(new Mock<Mud.Feishu.IFeishuTenantV2BitableRole>().Object)
            .AddSingleton(new Mock<Mud.Feishu.IFeishuTenantV1Docx>().Object)
            .AddSingleton(new Mock<Mud.Feishu.IFeishuTenantV1DocxBlocks>().Object)
            // R7/A2：Docx 深化执行器（DocxDeepTools）的群公告客户端——**必选依赖**，
            // 缺席时该执行器整体解析失败 ⇒ 9 个 docx 深化工具静默缺席（全量契约断言随之落空）。
            .AddSingleton(new Mock<Mud.Feishu.IFeishuTenantV1DocxAnnouncement>().Object)
            // R7/A4：Board 画板（可空依赖，缺席时运行时回填结构化错误——此处仍注册以覆盖"齐备"路径）。
            .AddSingleton(new Mock<Mud.Feishu.IFeishuTenantV1Board>().Object)
            // R7/A5：Attendance 考勤 6 个客户端（全部可空——注册以满足"客户端齐备"的全量契约断言）。
            .AddSingleton(new Mock<Mud.Feishu.IFeishuUserV1AttendanceUserTask>().Object)
            .AddSingleton(new Mock<Mud.Feishu.IFeishuTenantV1AttendanceUserFlows>().Object)
            .AddSingleton(new Mock<Mud.Feishu.IFeishuTenantV1AttendanceUserDailyShifts>().Object)
            .AddSingleton(new Mock<Mud.Feishu.IFeishuTenantV1AttendanceStats>().Object)
            .AddSingleton(new Mock<Mud.Feishu.IFeishuTenantV1AttendanceRemedys>().Object)
            .AddSingleton(new Mock<Mud.Feishu.IFeishuTenantV1AttendanceShifts>().Object)
            // R7/A6：Spark 妙搭 3 个客户端（tenant 应用面 / user 应用面 / user 数据表面）。
            .AddSingleton(new Mock<Mud.Feishu.IFeishuTenantV1SparkApp>().Object)
            .AddSingleton(new Mock<Mud.Feishu.IFeishuUserV1SparkApp>().Object)
            .AddSingleton(new Mock<Mud.Feishu.IFeishuUserV1SparkAppTable>().Object)
            .AddSingleton(new Mock<Mud.Feishu.IFeishuTenantV2WikiNodes>().Object)
            .AddSingleton(new Mock<Mud.Feishu.IFeishuTenantV2SearchDocWiki>().Object)
            .AddSingleton(new Mock<Mud.Feishu.IFeishuTenantV1Message>().Object)
            .AddSingleton(new Mock<Mud.Feishu.IFeishuTenantV1ChatGroupMember>().Object)
            .AddSingleton(new Mock<Mud.Feishu.IFeishuTenantV3Spreadsheets>().Object)
            .AddSingleton(new Mock<Mud.Feishu.IFeishuTenantV3SpreadsheetData>().Object)
            // T-2：Sheets 深化执行器 5 个客户端（必选依赖，缺席 ⇒ cond_format/data_validation/
            // protected/filter/float_image 共 21 个深化工具静默缺席，全量契约断言随之落空）。
            .AddSingleton(new Mock<Mud.Feishu.IFeishuTenantV2SpreadsheetConditionFormat>().Object)
            .AddSingleton(new Mock<Mud.Feishu.IFeishuTenantV2SpreadsheetDataValidation>().Object)
            .AddSingleton(new Mock<Mud.Feishu.IFeishuTenantV2SpreadsheetProtected>().Object)
            .AddSingleton(new Mock<Mud.Feishu.IFeishuTenantV3SpreadsheetFilter>().Object)
            .AddSingleton(new Mock<Mud.Feishu.IFeishuTenantV3SpreadsheetFloatImage>().Object)
            .AddSingleton(new Mock<Mud.Feishu.IFeishuTenantV4Approval>().Object)
            .AddSingleton(new Mock<Mud.Feishu.IFeishuTenantV4ApprovalQuery>().Object)
            .AddSingleton(new Mock<Mud.Feishu.IFeishuTenantV4ApprovalTask>().Object)
            // R5 / F-11：minutes 域执行器（MinutesReadTools）需要该客户端才能被 DI 装配进目录。
            .AddSingleton(new Mock<Mud.Feishu.IFeishuTenantV1MinutesMinute>().Object)
            .AddSingleton(new Mock<Mud.Feishu.IFeishuTenantV1DriveFolder>().Object)
            .AddSingleton(new Mock<Mud.Feishu.IFeishuTenantV1DriveFiles>().Object)
            // docx.history_list / docx.media_insert 的跨域客户端：版本列表为 DocxDeepTools 的
            // **必选**依赖（缺席 ⇒ 10 个 docx 深化工具整体软缺席），媒体上传为可空依赖。
            .AddSingleton(new Mock<Mud.Feishu.IFeishuTenantV1DriveFilesVersions>().Object)
            .AddSingleton(new Mock<Mud.Feishu.IFeishuTenantV1DriveMedia>().Object)
            // MUDFT022：drive 协作面执行器（DriveCommentTools / DrivePermissionTools）的两个客户端——
            // 缺席时该域软缺席，AddFeishuTools_ShouldRegisterExactlyTheContractTools 的
            // "注册表 ≡ 契约表 125 项"断言必然落空。
            .AddSingleton(new Mock<Mud.Feishu.IFeishuTenantV1DriveComments>().Object)
            .AddSingleton(new Mock<Mud.Feishu.IFeishuTenantV1DrivePermissions>().Object)
            // MailTools（读写混合域，归写链）的三个客户端：消息侧为必选依赖，标签/会话侧可选；
            // 全部缺席时 mail 域软缺席——同样会让全量契约断言落空（且 MailTools 构造参数无默认值）。
            .AddSingleton(new Mock<Mud.Feishu.IFeishuTenantV1MailMessage>().Object)
            .AddSingleton(new Mock<Mud.Feishu.IFeishuTenantV1MailLabel>().Object)
            .AddSingleton(new Mock<Mud.Feishu.IFeishuTenantV1MailThread>().Object)
            .AddSingleton(new Mock<Mud.Feishu.IFeishuTenantV3User>().Object)
            .AddSingleton(new Mock<Mud.Feishu.IFeishuTenantV4CalendarEvent>().Object)
            .AddSingleton(new Mock<Mud.Feishu.IFeishuTenantV4Calendar>().Object)
            .AddSingleton(new Mock<Mud.Feishu.IFeishuTenantV2Task>().Object)
            // T-3：Task 深化执行器（TaskDeepTools）的清单侧客户端——**必选依赖**，
            // 缺席时该执行器整体解析失败 ⇒ tasklist/搜索/提醒/重开共 12 个深化工具静默缺席，
            // 「注册表 ≡ 契约表」的全量断言随之落空。
            .AddSingleton(new Mock<Mud.Feishu.IFeishuTenantV2TaskList>().Object)
            // T-3：Mail 深化执行器（MailDeepTools）的草稿/规则/模板客户端（全部可空依赖——
            // 此处注册以覆盖「客户端齐备」路径，让 17 个深化工具走真实分支而非执行期结构化错误）。
            .AddSingleton(new Mock<Mud.Feishu.IFeishuUserV1MailDraft>().Object)
            .AddSingleton(new Mock<Mud.Feishu.IFeishuTenantV1MailRule>().Object)
            .AddSingleton(new Mock<Mud.Feishu.IFeishuTenantV1MailTemplate>().Object)
            .AddSingleton(new Mock<Mud.Feishu.IFeishuUserV1MailTemplate>().Object)
            .AddSingleton(new Mock<Mud.AI.Abstractions.Knowledge.IRetriever>().Object);

        if (withUserTaskClient)
        {
            services.AddSingleton(new Mock<Mud.Feishu.IFeishuUserV2Task>().Object);
        }

        if (withAttachmentStager)
        {
            // WP7：附件落盘器（宿主注入）——未注册时 im.send_image / im.send_file 不注册（软缺席）。
            services.AddSingleton(new Mock<Mud.Feishu.AI.Tools.Hosting.IFeishuAttachmentStager>().Object);
        }

        // R-8：同义入口 AddFeishuReadonlyTools 已删除，全域入口只有 AddFeishuTools。
        return services.AddFeishuTools().BuildServiceProvider();
    }
}
