// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
// -----------------------------------------------------------------------

namespace Mud.Feishu.AI.CapabilityDemo.Common;

/// <summary>
/// 工具白名单的<b>唯一定义处</b>：所有幕与 <c>--verify</c> 都从这里取，
/// 禁止在各幕里内联工具名（否则自检无法证明"演示用的就是这一份"）。
/// </summary>
/// <remarks>
/// 名字即模型可见契约；拼错不会在编译期被发现，因此 <c>--verify</c> 的
/// <c>whitelist</c> 组会拿它与 <c>IToolCatalog</c> 逐名比对（存在性 + 读写分类）。
/// </remarks>
internal static class ToolSets
{
    /// <summary>元工具（零飞书请求，但执行链仍要求 appKey 上下文）。</summary>
    public static readonly string[] Meta4 =
    [
        "feishu.tool_search",
        "feishu.capability_lookup",
        "feishu.guidance_read",
        "feishu.schema_read",
    ];

    /// <summary>A1：最小只读面（列表 + 查询）。</summary>
    public static readonly string[] A1Read = ["bitable.list_tables", "bitable.query_records"];

    /// <summary>A5：多维表格读链 5 枚（博客"完整跑一遍"里的读面）。</summary>
    public static readonly string[] BitableRead5 =
    [
        "bitable.list_tables",
        "bitable.list_fields",
        "bitable.list_views",
        "bitable.get_view",
        "bitable.query_records",
    ];

    /// <summary>A5：写面 2 枚（刻意不含 <see cref="BitableDelete"/>）。</summary>
    public static readonly string[] BitableWrite2 = ["bitable.record_batch_create", "bitable.update_record"];

    /// <summary>高危写：只有显式 <c>--include-delete</c> 才进入白名单。</summary>
    public const string BitableDelete = "bitable.delete_record";

    /// <summary>A8 / A6：最小只读面（演示租户隔离与协议，不需要更多）。</summary>
    public static readonly string[] MinimalRead = ["bitable.list_tables"];

    /// <summary>A6：MCP 暴露面（只读 + 元工具；<b>不含写</b>——MCP 没有审批管线）。</summary>
    public static readonly string[] McpRead =
    [
        "bitable.list_tables",
        "feishu.capability_lookup",
        "feishu.guidance_read",
    ];

    /// <summary>A5 默认写面（可选参数追加删除）。</summary>
    public static string[] WritesForA5(bool includeDelete)
        => includeDelete ? [.. BitableWrite2, BitableDelete] : BitableWrite2;

    /// <summary>
    /// 宿主插件软缺席工具：注册表按「宿主是否注入插件」缺席，而编译期策展面（McpEnvelope 导出）恒含。
    /// </summary>
    /// <remarks>
    /// <para>
    /// <c>drive.download_file</c> / <c>drive.export_document</c> / <c>im.send_image</c> / <c>im.send_file</c>
    /// 依赖宿主注入 <c>IFeishuAttachmentStager</c>（落盘 = 宿主安全域：域名白名单 / 大小上限 / 清理），
    /// <c>knowledge.search</c> 依赖 <c>IRetriever</c>。本 Demo 只注入白名单用得到的钩子
    /// （审计 / 审批 / 授权器），故这 5 枚不进注册表——「三源一致」检查据此把差额当<b>契约</b>而不是漂移。
    /// </para>
    /// <para>
    /// 这不是 SDK 缺陷：软缺席让「宿主没这个能力」在装配期就成立，而不是运行期才发现（对齐
    /// <c>AddFeishuTools</c> 的「客户端缺席 ⇒ 该域工具不进注册表」同一机制）。
    /// </para>
    /// </remarks>
    public static readonly string[] HostPluginSoftAbsent =
    [
        "drive.download_file",
        "drive.export_document",
        "im.send_file",
        "im.send_image",
        "knowledge.search",
    ];
}
