// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// 飞书功能模块枚举
/// </summary>
public enum FeishuModule
{
    /// <summary>
    /// 组织管理
    /// </summary>
    Organization,

    /// <summary>
    /// 消息管理
    /// </summary>
    Message,

    /// <summary>
    /// 群聊管理
    /// </summary>
    ChatGroup,

    /// <summary>
    /// 流程审批管理
    /// </summary>
    Approval,

    /// <summary>
    /// 任务管理
    /// </summary>
    Task,

    /// <summary>
    /// 卡片管理
    /// </summary>
    Card,

    /// <summary>
    /// 考勤管理
    /// </summary>
    Attendance,

    /// <summary>
    /// 飞书云盘管理
    /// </summary>
    Drive,

    /// <summary>
    /// 飞书知识库管理
    /// </summary>
    Wiki,

    /// <summary>
    /// 飞书文档管理
    /// </summary>
    Docx,

    /// <summary>
    /// 飞书电子表格管理
    /// </summary>
    Spreadsheets,

    /// <summary>
    /// 飞书多维表格管理
    /// </summary>
    Bitable,

    /// <summary>
    /// 飞书日历管理
    /// </summary>
    Calendar,

    /// <summary>
    /// 飞书视频会议管理
    /// </summary>
    VideoConferencing,

    /// <summary>
    /// 认证授权
    /// </summary>
    Authentication,

    /// <summary>
    /// 邮箱
    /// </summary>
    Mail,

    /// <summary>
    /// AI能力
    /// </summary>
    AI,

    /// <summary>
    /// 所有功能
    /// </summary>
    All
}
