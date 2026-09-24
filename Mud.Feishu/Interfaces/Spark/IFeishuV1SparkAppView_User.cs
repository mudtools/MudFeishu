// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.DataModels.Spark;

namespace Mud.Feishu;


/// <summary>
/// 飞书妙搭（Spark）视图 SDK 是一组服务端 OpenAPI 的封装，用于查询妙搭应用下的视图数据记录。
/// <para>接口详细文档请参见：<see href="https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/spark-v1/overview"/></para>
/// </summary>
[HttpClientApi(TokenManage = nameof(IFeishuAppManager), RegistryGroupName = "Spark")]
[Token(FeishuTokenTypes.UserAccessToken, Name = Consts.Authorization)]
public interface IFeishuUserV1SparkAppView : IFeishuAppContextSwitcher, ICurrentUserId
{
    /// <summary>
    /// 查询视图数据记录
    /// <para>查询应用下的视图数据记录，包括指定列、字段值及分页信息，适用于需要获取应用下某视图数据的记录、展示等场景。</para>
    /// <para>查询参数采用查询对象模式（<see cref="GetViewRecordListQuery"/>，见 AGENTS.md API-2）。</para>
    /// <para><see href="https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/spark-v1/app-view/get_view_record_list">接口文档</see></para>
    /// </summary>
    /// <param name="app_id">妙搭应用 id，可从妙搭应用 URL 中获取，如 https://miaoda.feishu.cn/app/app_4jcn5n11bpf5v 中的 app_4jcn5n11bpf5v 即为 app_id</param>
    /// <param name="view_name">妙搭视图表名，必须属于 app_id 对应的妙搭应用，可从妙搭应用数据库管理视图列表中获取。示例值：student_table_view</param>
    /// <param name="query">分页、列筛选、过滤、排序、环境与用户 ID 类型查询参数</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns>返回视图数据记录分页列表（items 为数组序列化后的 JSONString）</returns>
    [Get("/open-apis/spark/v1/apps/{app_id}/views/{view_name}/records")]
    Task<FeishuApiResult<GetViewRecordListResult>?> GetViewRecordListAsync(
        [Path] string app_id,
        [Path] string view_name,
        [Query] GetViewRecordListQuery? query = null,
        CancellationToken cancellationToken = default);
}
