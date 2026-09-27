// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.AI.FeishuTools;

/// <summary>
/// 工具名契约表（Phase 1 §3.3.2 + AI-FD-D12 P1D-1b 批次 A 扩容的唯一权威：8 域 16 个只读工具）。
/// </summary>
/// <remarks>
/// 工具名是模型可见契约——注册名、Schema 名与契约表的漂移由契约守卫锁定
/// （如 <c>bitable.list_fields</c> 强制命名，防源码方法名语义污染）。
/// </remarks>
public static class FeishuToolNames
{
    /// <summary>bitable.list_tables：列出多维表格数据表。</summary>
    public const string BitableListTables = "bitable.list_tables";

    /// <summary>bitable.list_fields：列出数据表字段定义。</summary>
    public const string BitableListFields = "bitable.list_fields";

    /// <summary>bitable.query_records：查询记录（filter/sort 简化文法）。</summary>
    public const string BitableQueryRecords = "bitable.query_records";

    /// <summary>bitable.get_records_by_ids：按 ID 批量取记录（P1D-1b 批次 A）。</summary>
    public const string BitableGetRecordsByIds = "bitable.get_records_by_ids";

    /// <summary>docx.get_raw_content：读取文档纯文本正文。</summary>
    public const string DocxGetRawContent = "docx.get_raw_content";

    /// <summary>docx.get_document_blocks：分块读取文档结构（P1D-1b 批次 A）。</summary>
    public const string DocxGetDocumentBlocks = "docx.get_document_blocks";

    /// <summary>wiki.get_node：解析知识库节点信息。</summary>
    public const string WikiGetNode = "wiki.get_node";

    /// <summary>wiki.list_nodes：列出知识空间子节点。</summary>
    public const string WikiListNodes = "wiki.list_nodes";

    /// <summary>search.doc_wiki：云文档与知识库搜索。</summary>
    public const string SearchDocWiki = "search.doc_wiki";

    /// <summary>im.get_history_messages：读取群聊历史消息。</summary>
    public const string ImGetHistoryMessages = "im.get_history_messages";

    /// <summary>im.get_message_content：单条消息内容回查（P1D-1b 批次 A，依赖 P1D-1a 路由修复）。</summary>
    public const string ImGetMessageContent = "im.get_message_content";

    /// <summary>drive.list_folder_files：列出云空间文件夹内容（P1D-1b 批次 A）。</summary>
    public const string DriveListFolderFiles = "drive.list_folder_files";

    /// <summary>drive.get_file_metas：文件元信息批量查询（P1D-1b 批次 A）。</summary>
    public const string DriveGetFileMetas = "drive.get_file_metas";

    /// <summary>knowledge.search：飞书知识库检索（P2D-4b 批次 A）。</summary>
    public const string KnowledgeSearch = "knowledge.search";

    /// <summary>sheets.list_sheets：列出电子表格工作表。</summary>
    public const string SheetsListSheets = "sheets.list_sheets";

    /// <summary>sheets.get_range_values：读取工作表单元格区域数据。</summary>
    public const string SheetsGetRangeValues = "sheets.get_range_values";

    /// <summary>im.send_message：发送文本消息（写，Phase 2）。</summary>
    public const string ImSendMessage = "im.send_message";

    /// <summary>bitable.add_record：新增记录（写，Phase 2）。</summary>
    public const string BitableAddRecord = "bitable.add_record";

    /// <summary>approval.create_instance：发起审批实例（写，Phase 2）。</summary>
    public const string ApprovalCreateInstance = "approval.create_instance";

    /// <summary>只读契约名（Phase 1 §3.3.2 + AI-FD-D12 P1D-1b 批次 A，契约守卫逐一比对）。</summary>
    public static readonly string[] ReadonlyAll =
    [
        BitableListTables,
        BitableListFields,
        BitableQueryRecords,
        BitableGetRecordsByIds,
        DocxGetRawContent,
        DocxGetDocumentBlocks,
        WikiGetNode,
        WikiListNodes,
        SearchDocWiki,
        ImGetHistoryMessages,
        ImGetMessageContent,
        DriveListFolderFiles,
        DriveGetFileMetas,
        KnowledgeSearch,
        SheetsListSheets,
        SheetsGetRangeValues,
    ];

    /// <summary>写类契约名（Phase 2 §3.3，契约守卫逐一比对；白名单单独键控 WriteAllowList）。</summary>
    public static readonly string[] WriteAll =
    [
        ImSendMessage,
        BitableAddRecord,
        ApprovalCreateInstance,
    ];

    /// <summary>全部契约名（只读 + 写；契约守卫逐一比对）。</summary>
    public static readonly string[] All = [.. ReadonlyAll, .. WriteAll];

    /// <summary>判断工具名是否写类（白名单读写分离判定）。</summary>
    public static bool IsWriteTool(string name) => WriteAll.Contains(name, StringComparer.Ordinal);
}

/// <summary>绑定层固定分页大小（隐藏参数：模型不可见，绑定层给默认值并钳制上限，§3.3.2）。</summary>
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

    /// <summary>单条消息 content 预览截断长度。</summary>
    public const int MessagePreviewLength = 200;
}
