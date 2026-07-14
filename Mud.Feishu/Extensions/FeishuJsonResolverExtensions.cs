// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

#if NET8_0_OR_GREATER
using System.Text.Json.Serialization.Metadata;
using Mud.Feishu.Abstractions.Utilities;
using Mud.Feishu.DataModels.AI;
using Mud.Feishu.DataModels.ApprovalComments;
using Mud.Feishu.DataModels.AttendanceApprovals;
using Mud.Feishu.DataModels.Bitable;
using Mud.Feishu.DataModels.Board;
using Mud.Feishu.DataModels.Calendar;
using Mud.Feishu.DataModels.CardElements;
using Mud.Feishu.DataModels.ChatGroupNotice;
using Mud.Feishu.DataModels;
using Mud.Feishu.DataModels.Docx;
using Mud.Feishu.DataModels.Drive;
using Mud.Feishu.DataModels.HelpDesk;
using Mud.Feishu.DataModels.Mail;
using Mud.Feishu.DataModels.Messages;
using Mud.Feishu.DataModels.DepartmentsV1;
using Mud.Feishu.DataModels.Search;
using Mud.Feishu.DataModels.Spreadsheets;
using Mud.Feishu.DataModels.TasksActivitySubscriptions;
using Mud.Feishu.DataModels.VideoConferencing;
using Mud.Feishu.DataModels.Wiki;

namespace Mud.Feishu.Extensions;

/// <summary>
/// Feishu JSON 解析器扩展，用于配置 DataModels 源生成上下文。
/// 在 net8.0+ 下将 20 个已生成的 DataModels Context 合并为一个解析器并注入到 FeishuJsonDefaults。
/// </summary>
public static class FeishuJsonResolverExtensions
{
    /// <summary>
    /// 配置 DataModels 的 JSON 解析器。
    /// 将 20 个已生成的 DataModels Context 合并为一个解析器并注入到 FeishuJsonDefaults。
    /// 必须在应用程序启动时、任何 JSON 序列化/反序列化发生前调用。
    /// </summary>
    public static void ConfigureDataModelsResolver()
    {
        // 合并所有已生成的 DataModels Context（均为 internal，通过 InternalsVisibleTo 访问）
        var dataModelsResolver = JsonTypeInfoResolver.Combine(
            AIJsonContext.Default,
            ApprovalJsonContext.Default,
            AttendanceJsonContext.Default,
            BitableJsonContext.Default,
            BoardJsonContext.Default,
            CalendarJsonContext.Default,
            CardJsonContext.Default,
            ChatGroupJsonContext.Default,
            CommonJsonContext.Default,
            DocxJsonContext.Default,
            DriveJsonContext.Default,
            HelpDeskJsonContext.Default,
            MailJsonContext.Default,
            MessagesJsonContext.Default,
            OrganizationJsonContext.Default,
            SearchJsonContext.Default,
            SpreadsheetsJsonContext.Default,
            TaskJsonContext.Default,
            VideoConferencingJsonContext.Default,
            WikiJsonContext.Default
        );

        // 注入合并后的 DataModels resolver 到 FeishuJsonDefaults
        // FeishuJsonDefaults.ConfigureUserResolver 会进一步将其与 SDK 内置 FeishuJsonContext 合并
        FeishuJsonDefaults.ConfigureUserResolver(dataModelsResolver);
    }
}
#endif