// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.DataModels.CardMessageStream;

namespace Mud.Feishu;

/// <summary>
/// 应用消息流卡片是飞书为应用提供的消息触达能力，让应用可以直接在消息流发送消息，重要消息能更快触达用户。
/// <para>当前接口使用租户令牌访问，适应于租户应用场景。</para>
/// 接口详细文档请参见：<see href="https://open.feishu.cn/document/im-v2/overview"/>
/// </summary>
[HttpClientApi(RegistryGroupName = "Cards", TokenManage = nameof(ITenantTokenManager))]
[Header("Authorization")]
public interface IFeishuTenantV2AppCardMessageStream
{
    /// <summary>
    /// 创建应用消息流卡片
    /// </summary>
    /// <param name="appCardMessageStreamRequest">创建应用消息流卡片请求体</param>
    /// <param name="user_id_type">用户 ID，ID 类型需要与查询参数中的 user_id_type 类型保持一致。</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns></returns>
    [Post("https://open.feishu.cn/open-apis/im/v2/app_feed_card")]
    Task<FeishuApiResult<CreateAppCardMessageStreamResult>?> CreateAppCardMessageStreamAsync(
       [Body] CreateAppCardMessageStreamRequest appCardMessageStreamRequest,
       [Query("user_id_type")] string user_id_type = Consts.User_Id_Type,
       CancellationToken cancellationToken = default);

    /// <summary>
    /// 更新应用消息流卡片
    /// </summary>
    /// <param name="appCardMessageStreamRequest">更新应用消息流卡片请求体</param>
    /// <param name="user_id_type">用户 ID，ID 类型需要与查询参数中的 user_id_type 类型保持一致。</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns></returns>
    [Put("https://open.feishu.cn/open-apis/im/v2/app_feed_card/batch")]
    Task<FeishuApiResult<UpdateAppCardMessageStreamResult>?> UpdateAppCardMessageStreamAsync(
      [Body] UpdateAppCardMessageStreamRequest appCardMessageStreamRequest,
      [Query("user_id_type")] string user_id_type = Consts.User_Id_Type,
      CancellationToken cancellationToken = default);


    [Delete("https://open.feishu.cn/open-apis/im/v2/app_feed_card/batch")]
    Task<FeishuApiResult<UpdateAppCardMessageStreamResult>?> DeleteAppCardMessageStreamAsync(
     [Body] UpdateAppCardMessageStreamRequest appCardMessageStreamRequest,
     [Query("user_id_type")] string user_id_type = Consts.User_Id_Type,
     CancellationToken cancellationToken = default);
}
