// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.AI.FeishuTools;

/// <summary>
/// 绑定层固定分页大小（隐藏参数：模型不可见，绑定层给默认值并钳制上限，§3.3.2）。
/// </summary>
/// <remarks>
/// 与工具名契约表分离：工具名/读写分类由源生成器从 <c>[FeishuTool]</c> 特性派生
/// （<c>FeishuToolNames.g.cs</c>），而分页尺寸是执行层的实现细节，属于手写常量。
/// </remarks>
internal static class PageSizes
{
    /// <summary>bitable.list_tables（上限 100）。</summary>
    public const int BitableTables = 50;

    /// <summary>bitable.list_fields（上限 100）。</summary>
    public const int BitableFields = 100;

    /// <summary>bitable.query_records（默认 20，上限 50）。</summary>
    public const int BitableRecords = 20;

    /// <summary>wiki.list_nodes（上限 50）。</summary>
    public const int WikiNodes = 50;

    /// <summary>search.doc_wiki（上限 20）。</summary>
    public const int Search = 20;

    /// <summary>im.get_history_messages（默认 10，上限 20）。</summary>
    public const int History = 10;

    /// <summary>docx.get_document_blocks（官方默认 500/页）。</summary>
    public const int DocxBlocks = 500;

    /// <summary>drive.list_folder_files（绑定层固定页大小，上限 50）。</summary>
    public const int DriveFiles = 50;

    /// <summary>drive.get_file_metas（官方单请求上限 200）。</summary>
    public const int DriveMetas = 200;

    /// <summary>bitable.get_records_by_ids（官方单请求上限 100）。</summary>
    public const int BitableRecordsByIds = 100;

    /// <summary>通讯录按手机号/邮箱批量查 ID（官方单请求上限 50）。</summary>
    public const int ContactResolve = 50;

    /// <summary>通讯录按姓名/关键字搜人（官方默认 10；此处取 20 以减少"找不到人"的追问轮次）。</summary>
    public const int ContactSearch = 20;

    /// <summary>calendar.list_events（绑定层固定页大小，上限 50）。</summary>
    public const int CalendarEvents = 50;

    /// <summary>task.list_my_tasks（官方默认 10；此处取 50 以减少追问轮次）。</summary>
    public const int TaskList = 50;

    /// <summary>approval.list_pending_tasks（官方默认 10；此处取 50 以减少追问轮次）。</summary>
    public const int ApprovalTasks = 50;

    /// <summary>mail.list_messages（官方默认 20，上限 50）。</summary>
    public const int MailMessages = 20;

    /// <summary>contact.list_departments（官方默认 10，上限 50）。</summary>
    public const int Departments = 50;

    /// <summary>contact.list_department_members（官方上限 100）。</summary>
    public const int DepartmentMembers = 50;

    /// <summary>单条消息 content 预览截断长度。</summary>
    public const int MessagePreviewLength = 200;
}
