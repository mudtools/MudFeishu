// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.DataModels.Spark;

namespace Mud.Feishu.Interfaces;


/// <summary>
/// 飞书妙搭（Spark）用户目录 SDK 是一组服务端 OpenAPI 的封装，用于在飞书妙搭与飞书开放平台之间转换用户 ID。本接口声明支持 tenant_access_token 与 user_access_token 双令牌调用的端点。
/// <para>接口详细文档请参见：<see href="https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/spark-v1/directory-user/id_convert"/></para>
/// </summary>
[HttpClientApi(TokenManage = nameof(IFeishuAppManager), IsAbstract = true)]
[Token(FeishuTokenTypes.TenantAccessToken, Name = Consts.Authorization)]
public interface IFeishuV1SparkDirectoryUser : IFeishuAppContextSwitcher
{
    /// <summary>
    /// 转换飞书妙搭与飞书开放平台之间的用户 ID
    /// <para>指定转换类型（id_convert_type）与待转换的 ID 列表（ids），实现妙搭用户 ID 与飞书开放平台 OpenID/UnionID、飞书用户 ID 之间的双向转换。</para>
    /// <para>限频：50 次/秒。所需权限：spark:directory.user.id_convert:read（至少具备其一）。</para>
    /// <para><see href="https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/spark-v1/directory-user/id_convert">接口文档</see></para>
    /// </summary>
    /// <param name="request">转换请求体（id_convert_type 必填：10/11/20/21/40；ids 可选，长度 1～100）</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns>返回 ID 映射列表（无结果或查询出错的 ID 不会返回）</returns>
    [Post("/open-apis/spark/v1/directory/user/id_convert")]
    Task<FeishuApiResult<IdConvertResult>?> IdConvertAsync(
        [Body] IdConvertRequest request,
        CancellationToken cancellationToken = default);
}
