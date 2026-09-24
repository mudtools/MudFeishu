// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.DataModels.Spark;

namespace Mud.Feishu;


/// <summary>
/// 飞书妙搭（Spark）自定义枚举 SDK 是一组服务端 OpenAPI 的封装，用于获取妙搭应用下的自定义枚举列表与详情。
/// <para>接口详细文档请参见：<see href="https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/spark-v1/overview"/></para>
/// </summary>
[HttpClientApi(TokenManage = nameof(IFeishuAppManager), RegistryGroupName = "Spark")]
[Token(FeishuTokenTypes.UserAccessToken, Name = Consts.Authorization)]
public interface IFeishuUserV1SparkAppEnum : IFeishuAppContextSwitcher, ICurrentUserId
{
    /// <summary>
    /// 获取自定义枚举列表
    /// <para>获取应用下的自定义枚举列表，包括枚举名称、描述、枚举值列表等字段信息。</para>
    /// <para><see href="https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/spark-v1/app-enum/get_enum_list">接口文档</see></para>
    /// </summary>
    /// <param name="app_id">妙搭应用 id，可从妙搭应用 URL 中获取，如 https://miaoda.feishu.cn/app/app_4jcn5n11bpf5v 中的 app_4jcn5n11bpf5v 即为 app_id</param>
    /// <param name="page_size">分页大小，用于限制一次请求所返回的数据条目数。默认 10，最大 500。示例值：10</param>
    /// <param name="page_token">分页标记，第一次请求不填，表示从头开始遍历；分页查询结果还有更多项时会同时返回新的 page_token，下次遍历可采用该 page_token 获取查询结果</param>
    /// <param name="env">访问的 database 环境，默认为 online（线上环境）。示例值：online、dev</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns>返回自定义枚举分页列表</returns>
    [Get("/open-apis/spark/v1/apps/{app_id}/enums")]
    Task<FeishuApiResult<GetEnumListResult>?> GetEnumListAsync(
        [Path] string app_id,
        [Query("page_size")] int page_size = 10,
        [Query("page_token")] string? page_token = null,
        [Query("env")] string? env = "online",
        CancellationToken cancellationToken = default);


    /// <summary>
    /// 获取自定义枚举详细信息
    /// <para>获取应用下的自定义枚举详细信息，包括枚举名称、描述、枚举值列表等字段信息。</para>
    /// <para><see href="https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/spark-v1/app-enum/get_enum_detail">接口文档</see></para>
    /// </summary>
    /// <param name="app_id">妙搭应用 id，可从妙搭应用 URL 中获取</param>
    /// <param name="enum_name">枚举名称，可以从「获取自定义枚举列表」接口返回列表中获取。示例值：enum_demo_1</param>
    /// <param name="env">访问的 database 环境，默认为 online（线上环境）。示例值：online、dev</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns>返回自定义枚举详细信息</returns>
    [Get("/open-apis/spark/v1/apps/{app_id}/enums/{enum_name}")]
    Task<FeishuApiResult<GetEnumDetailResult>?> GetEnumDetailAsync(
        [Path] string app_id,
        [Path] string enum_name,
        [Query("env")] string? env = "online",
        CancellationToken cancellationToken = default);
}
