// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------


namespace Mud.Feishu.DataModels.Docx;

/// <summary>
/// 云文档 <c>block_type</c> 的<b>取值常量表与反查表</b>（R5 / B-1）。
/// </summary>
/// <remarks>
/// <para>
/// <b>为什么存在（R5 / B-1 根因）</b>：<see cref="Block.BlockType"/> 在 DataModels 层把平台 wire 值
/// 直接暴露为 <see cref="int"/>，且取值清单<b>只存在于 XML 注释里</b>。后果有三：
/// <list type="number">
/// <item>工具层被迫把 <c>"块类型（2=文本段落, 3=标题1, …, 11=标题9）"</c> 这样的<b>裸整数</b>
/// 写进模型可见的 Schema（模型须猜平台值）；</item>
/// <item>生成器虽已具备枚举闭集能力（<c>ParameterSchemaRenderer.RenderEnum</c> /
/// <c>TypeSchemaResolver.ResolveEnumSchema</c>），但<b>没有枚举/常量类型可取</b>；</item>
/// <item><see cref="Block.BlockType"/> 作为 <see cref="int"/> 无法做编译期校验。</item>
/// </list>
/// </para>
/// <para>
/// <b>为什么是 static class + const int 而不是 enum</b>：<see cref="Block.BlockType"/> 的类型是
/// <see cref="int"/>，改成 <c>enum</c> 是<b>破坏性变更</b>。保持 <c>int</c> 并新增常量类，
/// 可做到二进制与序列化行为<b>零变化</b>。
/// </para>
/// <para>
/// <b>⚠️ 生成器联动（F-2 的实现级耦合，务必留意）</b>：正因为这里用 <c>static class</c> +
/// <c>const int</c>，其 <c>TypeKind</c> 是 <c>Class</c> 而<b>不是</b> <c>Enum</c> ⇒
/// <c>ParameterSchemaRenderer</c> 中 <c>type.TypeKind == TypeKind.Enum</c> 的判断<b>永不命中</c>。
/// F-2 必须同时支持"enum 类型"与"常量类"两种闭集来源，否则本类建好后 <c>block_type</c> 仍锁不住。
/// </para>
/// <para>
/// <b>取值清单的纪律</b>：本表的<b>唯一真相源</b>是 <see cref="Block.BlockType"/> 的 XML 注释
/// （即平台 OpenAPI 文档），由 <c>BlockTypesContractTests</c> 机械锁定"常量集合 == 注释清单 == Schema 枚举集"。
/// <list type="bullet">
/// <item><b>16 是平台保留号</b>：官方清单从 15 直接跳到 17，本表以 <see cref="Reserved"/> 显式登记，
/// <b>不臆造语义</b>（R5 实施纪律①：平台事实不凭推测补全）；</item>
/// <item><c>Block</c> 上的 <c>equation</c> 属性在官方 block_type 清单中<b>没有对应编号</b>，
/// 故本表<b>不为其编造常量</b>；<see cref="GetName"/> 对未知值返回 <see cref="UnknownName"/>
/// 而非抛异常（避免未知块型导致解析失败）。</item>
/// </list>
/// </para>
/// </remarks>
public static class BlockTypes
{
    /// <summary>页面 Block。</summary>
    public const int Page = 1;

    /// <summary>文本 Block。</summary>
    public const int Text = 2;

    /// <summary>标题 1 Block。</summary>
    public const int Heading1 = 3;

    /// <summary>标题 2 Block。</summary>
    public const int Heading2 = 4;

    /// <summary>标题 3 Block。</summary>
    public const int Heading3 = 5;

    /// <summary>标题 4 Block。</summary>
    public const int Heading4 = 6;

    /// <summary>标题 5 Block。</summary>
    public const int Heading5 = 7;

    /// <summary>标题 6 Block。</summary>
    public const int Heading6 = 8;

    /// <summary>标题 7 Block。</summary>
    public const int Heading7 = 9;

    /// <summary>标题 8 Block。</summary>
    public const int Heading8 = 10;

    /// <summary>标题 9 Block。</summary>
    public const int Heading9 = 11;

    /// <summary>无序列表 Block。</summary>
    public const int Bullet = 12;

    /// <summary>有序列表 Block。</summary>
    public const int Ordered = 13;

    /// <summary>代码块 Block。</summary>
    public const int Code = 14;

    /// <summary>引用 Block。</summary>
    public const int Quote = 15;

    /// <summary>平台保留号（官方清单中不存在，本仓不赋语义；登记以消除"缺号"歧义）。</summary>
    public const int Reserved = 16;

    /// <summary>待办事项 Block。</summary>
    public const int Todo = 17;

    /// <summary>多维表格 Block。</summary>
    public const int Bitable = 18;

    /// <summary>高亮块 Block。</summary>
    public const int Callout = 19;

    /// <summary>会话卡片 Block。</summary>
    public const int ChatCard = 20;

    /// <summary>流程图 &amp; UML Block。</summary>
    public const int Diagram = 21;

    /// <summary>分割线 Block。</summary>
    public const int Divider = 22;

    /// <summary>文件 Block。</summary>
    public const int File = 23;

    /// <summary>分栏 Block。</summary>
    public const int Grid = 24;

    /// <summary>分栏列 Block。</summary>
    public const int GridColumn = 25;

    /// <summary>内嵌 Block。</summary>
    public const int Iframe = 26;

    /// <summary>图片 Block。</summary>
    public const int Image = 27;

    /// <summary>开放平台小组件 Block。</summary>
    public const int Isv = 28;

    /// <summary>思维笔记 Block。</summary>
    public const int Mindnote = 29;

    /// <summary>电子表格 Block。</summary>
    public const int Sheet = 30;

    /// <summary>表格 Block。</summary>
    public const int Table = 31;

    /// <summary>表格单元格 Block。</summary>
    public const int TableCell = 32;

    /// <summary>视图 Block。</summary>
    public const int View = 33;

    /// <summary>引用容器 Block。</summary>
    public const int QuoteContainer = 34;

    /// <summary>任务 Block。</summary>
    public const int Task = 35;

    /// <summary>OKR Block。</summary>
    public const int Okr = 36;

    /// <summary>OKR Objective。</summary>
    public const int OkrObjective = 37;

    /// <summary>OKR Key Result。</summary>
    public const int OkrKeyResult = 38;

    /// <summary>OKR 进展。</summary>
    public const int OkrProgress = 39;

    /// <summary>文档小组件。</summary>
    public const int Widget = 40;

    /// <summary>Jira Issue。</summary>
    public const int JiraIssue = 41;

    /// <summary>Wiki 子目录 Block。</summary>
    public const int WikiCatalog = 42;

    /// <summary>画板 Block。</summary>
    public const int Board = 43;

    /// <summary>议程 Block。</summary>
    public const int Agenda = 44;

    /// <summary>议程项 Block。</summary>
    public const int AgendaItem = 45;

    /// <summary>议程项标题 Block。</summary>
    public const int AgendaItemTitle = 46;

    /// <summary>议程项内容 Block。</summary>
    public const int AgendaItemContent = 47;

    /// <summary>链接预览 Block。</summary>
    public const int LinkPreview = 48;

    /// <summary>源同步块。</summary>
    public const int SyncedBlock = 49;

    /// <summary>引用同步块。</summary>
    public const int QuoteSynced = 50;

    /// <summary>新版 Wiki 子目录 Block。</summary>
    public const int WikiCatalogV2 = 51;

    /// <summary>AI 模板 Block。</summary>
    public const int AiTemplate = 52;

    /// <summary>未支持 Block（未知类型）。</summary>
    public const int Undefined = 999;

    /// <summary><see cref="GetName"/> 对未知块型返回的名称（<b>不抛异常</b>）。</summary>
    public const string UnknownName = "unknown";

    /// <summary>取值 → 常量名的反查表（源码生成序，保持确定性）。</summary>
    private static readonly Dictionary<int, string> NameByType = new()
    {
        [Page] = nameof(Page),
        [Text] = nameof(Text),
        [Heading1] = nameof(Heading1),
        [Heading2] = nameof(Heading2),
        [Heading3] = nameof(Heading3),
        [Heading4] = nameof(Heading4),
        [Heading5] = nameof(Heading5),
        [Heading6] = nameof(Heading6),
        [Heading7] = nameof(Heading7),
        [Heading8] = nameof(Heading8),
        [Heading9] = nameof(Heading9),
        [Bullet] = nameof(Bullet),
        [Ordered] = nameof(Ordered),
        [Code] = nameof(Code),
        [Quote] = nameof(Quote),
        [Reserved] = nameof(Reserved),
        [Todo] = nameof(Todo),
        [Bitable] = nameof(Bitable),
        [Callout] = nameof(Callout),
        [ChatCard] = nameof(ChatCard),
        [Diagram] = nameof(Diagram),
        [Divider] = nameof(Divider),
        [File] = nameof(File),
        [Grid] = nameof(Grid),
        [GridColumn] = nameof(GridColumn),
        [Iframe] = nameof(Iframe),
        [Image] = nameof(Image),
        [Isv] = nameof(Isv),
        [Mindnote] = nameof(Mindnote),
        [Sheet] = nameof(Sheet),
        [Table] = nameof(Table),
        [TableCell] = nameof(TableCell),
        [View] = nameof(View),
        [QuoteContainer] = nameof(QuoteContainer),
        [Task] = nameof(Task),
        [Okr] = nameof(Okr),
        [OkrObjective] = nameof(OkrObjective),
        [OkrKeyResult] = nameof(OkrKeyResult),
        [OkrProgress] = nameof(OkrProgress),
        [Widget] = nameof(Widget),
        [JiraIssue] = nameof(JiraIssue),
        [WikiCatalog] = nameof(WikiCatalog),
        [Board] = nameof(Board),
        [Agenda] = nameof(Agenda),
        [AgendaItem] = nameof(AgendaItem),
        [AgendaItemTitle] = nameof(AgendaItemTitle),
        [AgendaItemContent] = nameof(AgendaItemContent),
        [LinkPreview] = nameof(LinkPreview),
        [SyncedBlock] = nameof(SyncedBlock),
        [QuoteSynced] = nameof(QuoteSynced),
        [WikiCatalogV2] = nameof(WikiCatalogV2),
        [AiTemplate] = nameof(AiTemplate),
        [Undefined] = nameof(Undefined),
    };

    /// <summary>全部已登记的取值（升序；供守卫遍历与工具层生成闭集）。</summary>
    public static IReadOnlyList<int> All { get; } =
        [.. NameByType.Keys.OrderBy(static value => value)];

    /// <summary>判断取值是否为已登记的块类型。</summary>
    /// <param name="blockType">待校验的 <c>block_type</c>。</param>
    public static bool IsValid(int blockType) => NameByType.ContainsKey(blockType);

    /// <summary>反查块类型名称；<b>未知值返回 <see cref="UnknownName"/> 而非抛异常</b>。</summary>
    /// <param name="blockType">待反查的 <c>block_type</c>。</param>
    public static string GetName(int blockType)
        => NameByType.TryGetValue(blockType, out var name) ? name : UnknownName;
}