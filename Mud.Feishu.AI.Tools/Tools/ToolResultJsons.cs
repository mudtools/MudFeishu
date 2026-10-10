// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.AI.Tools.Tools;

/// <summary>
/// R-3：工具结果信封的<b>单源构造</b>（分页信封 / 列表信封）。
/// </summary>
/// <remarks>
/// <para>
/// <b>为什么需要它</b>：<c>items</c>/<c>has_more</c>/<c>page_token</c> 三件套此前在
/// <b>29 处 / 18 个文件</b>里被逐字重写（其中 <c>BitableTools</c> 已抽出私有副本，
/// 但无人复用）。逐字重写的代价不是行数，而是<b>口径漂移</b>：任何一个执行器把
/// <c>page_token</c> 写成 <c>next_page_token</c>、或忘了"空 token 不写"，模型就无法续页——
/// 而这是<b>静默</b>的（信封仍然是合法 JSON，只是模型续不上）。
/// </para>
/// <para>
/// <b>与 <see cref="ToolPagination.BuildEnvelope"/> 的区别（不可混用）</b>：
/// <list type="bullet">
/// <item><b>本类型</b> = <b>单页</b>信封：一次调用一页，<c>has_more</c> 原样来自平台，
/// <c>page_token</c> 是"下一页游标"（模型据此自己再调一次）；</item>
/// <item><c>ToolPagination.BuildEnvelope</c> = <b><c>fetch_all</c> 聚合</b>信封：已代模型翻完页，
/// 另有 <c>total_fetched</c>/<c>pages_fetched</c>/<c>_budget</c>/<c>truncation_reason</c>，
/// 其 <c>has_more</c> 语义是"预算是否截断"（<b>不是</b>平台还有没有下一页）。</item>
/// </list>
/// 前者给模型"继续拉的把手"，后者给模型"我已经拉完了（或在预算处停下）"的结论——
/// 混用会让模型误以为"聚合信封里的 has_more=true"意味着可以继续拉（其实只能缩小预算重调）。
/// </para>
/// </remarks>
internal static class ToolResultJsons
{
    /// <summary>
    /// <b>单页分页信封</b>（唯一形态）：<c>items</c>（空数组，由调用方填充）+ <c>has_more</c>
    /// + <c>page_token</c>（仅非空时写入）。
    /// </summary>
    /// <param name="hasMore">
    /// 平台返回的"还有下一页"。<b>可空</b>：部分平台的载荷把 <c>has_more</c> 建模为可空
    /// （<c>bool?</c>，如 <c>GetMemberPageListResult</c>）——此时原样回填（<c>null</c> ⇒ JSON <c>null</c>），
    /// 保留"平台没给"与"平台说没有了"的差别，不臆造成 <c>false</c>。
    /// </param>
    /// <param name="pageToken">下一页游标（空/<see langword="null"/> 时不写入）。</param>
    /// <returns>信封对象（调用方随后逐条 <c>envelope["items"]!.AsArray().AddNode(...)</c>）。</returns>
    /// <remarks>
    /// <b>为什么空 token 不写入</b>：写入 <c>"page_token": ""</c> 会被模型当成"有一个可用的下一页游标"
    /// 并照抄回传，而平台对空 token 的处理是"从头再来"——表现为<b>重复拉取第一页</b>且模型无法自查。
    /// 省略字段才是"没有下一页"的准确表达。
    /// </remarks>
    public static JsonObject PageEnvelope(bool? hasMore, string? pageToken)
        => new JsonObject
        {
            ["items"] = new JsonArray(),
            ["has_more"] = hasMore,
        }.WithPageToken(pageToken);

    /// <summary>
    /// <b>无翻页契约的列表信封</b>（只含空 <c>items</c> 数组）：用于返回<b>全量</b>结果的接口
    /// （平台无 <c>page_token</c>，如 <c>sheets.list_sheets</c>/<c>im.get_read_users</c>）。
    /// </summary>
    /// <remarks>
    /// 保留统一形态（而不是让各执行器自己 <c>new JsonObject { ["items"] = … }</c>）的理由：
    /// "有哪些字段"与"字段怎么拼"是同一个契约面——信封形态一旦分叉，
    /// <c>items</c> 的键名/类型就会各自漂移（守卫 <c>PaginationContractGuards</c> 机械拦截）。
    /// </remarks>
    public static JsonObject ItemsEnvelope() => new() { ["items"] = new JsonArray() };

    /// <summary>补 <c>page_token</c>（仅非空时写入；语义见 <see cref="PageEnvelope"/>）。</summary>
    /// <param name="envelope">已建信封（可先带业务标量字段）。</param>
    /// <param name="pageToken">下一页游标。</param>
    /// <returns>同一信封（便于链式书写）。</returns>
    public static JsonObject WithPageToken(this JsonObject envelope, string? pageToken)
    {
        if (!string.IsNullOrEmpty(pageToken))
        {
            envelope["page_token"] = pageToken;
        }

        return envelope;
    }
}
