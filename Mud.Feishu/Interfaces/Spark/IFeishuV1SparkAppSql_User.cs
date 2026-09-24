// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.DataModels.Spark;

namespace Mud.Feishu;


/// <summary>
/// 飞书妙搭（Spark）SQL SDK 是一组服务端 OpenAPI 的封装，用于在妙搭应用下执行 SQL 语句。
/// <para>接口详细文档请参见：<see href="https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/spark-v1/overview"/></para>
/// </summary>
[HttpClientApi(TokenManage = nameof(IFeishuAppManager), RegistryGroupName = "Spark")]
[Token(FeishuTokenTypes.UserAccessToken, Name = Consts.Authorization)]
public interface IFeishuUserV1SparkAppSql : IFeishuAppContextSwitcher, ICurrentUserId
{
    /// <summary>
    /// 执行 SQL
    /// <para>在应用下执行 SQL。接口频率限制 10 次/秒。</para>
    /// <para><see href="https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/spark-v1/app/sql_commands">接口文档</see></para>
    /// </summary>
    /// <param name="app_id">妙搭应用 id，可从妙搭应用 URL 中获取，如 https://miaoda.feishu.cn/app/app_4jcn5n11bpf5v 中的 app_4jcn5n11bpf5v 即为 app_id</param>
    /// <param name="request">执行 SQL 请求体（sql 为要执行的 SQL 语句）</param>
    /// <param name="env">访问的 database 环境，默认为 online（线上环境）。示例值：online、dev</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns>
    /// SELECT 命令返回查询结果的 JSON 序列化字符串；DELETE 等无返回命令的 result 为空。
    /// </returns>
    [Post("/open-apis/spark/v1/apps/{app_id}/sql_commands")]
    Task<FeishuApiResult<ExecuteSqlCommandResult>?> ExecuteSqlCommandAsync(
        [Path] string app_id,
        [Body] ExecuteSqlCommandRequest request,
        [Query("env")] string? env = "online",
        CancellationToken cancellationToken = default);
}
