// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.AI.Tools;

/// <summary>
/// 工具接口：只声明模型可见 Schema（工具名/描述/扁平参数），不复用 HttpUtils 的
/// <c>[Path]/[Body]</c> 路由特性（Phase 1 §3.2）。
/// </summary>
/// <remarks>
/// <para>
/// 编译期由 <c>FeishuToolSchemaGenerator</c> 产出 OpenAI-compatible JSON Schema 与
/// <c>required_scopes</c> 权限元数据；工具执行链（参数映射 → BeginScope → 授权 →
/// 强类型调用 → 解包裁剪）由分域映射层实现——本接口只承载模型可见契约。
/// </para>
/// <para>
/// scope 字符串为占位，落地时对照开放平台控制台逐工具核对回填（契约守卫只锁
/// 「工具名↔scope 清单存在性」，不锁具体字符串）。
/// </para>
/// </remarks>
[FeishuTool("bitable.query_records",
    Description = "按条件查询多维表格记录（先经 bitable.list_tables 获取 table_id）",
    RequiredScopes = ["bitable:app:readonly"])]
public interface IFeishuBitableQueryTool
{
    /// <summary>
    /// 查询记录（模型可见参数扁平化；复杂请求体由执行链构造）。
    /// </summary>
    /// <returns>裁剪后的记录列表文本（由执行链回填模型）。</returns>
    Task<string> QueryRecordsPageListAsync(
        [ToolParameter("app_token", "多维表格 AppToken（形如 bascnXxx）", Required = true)] string app_token,
        [ToolParameter("table_id", "数据表 ID（形如 tblXxx）", Required = true)] string table_id,
        [ToolParameter("filter", "简化筛选式（可选），如：status = \"done\" and owner = \"张三\"；最多 5 个子句")] string? filter = null,
        [ToolParameter("view_id", "视图 ID（可选）")] string? view_id = null,
        [ToolParameter("page_token", "分页游标（可选，来自上一次结果的 page_token）")] string? page_token = null,
        CancellationToken cancellationToken = default);
}
