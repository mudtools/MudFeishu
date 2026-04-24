// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.DataModels.Drive;

namespace Mud.Feishu;

/// <summary>
/// 云文档事件订阅，用于订阅云文档的事件，如文件创建、更新、删除等，当云文档发生指定事件时，系统会向配置的地址发送事件通知。
/// <para>接口详细文档请参见：<see href="https://open.feishu.cn/document/server-docs/docs/drive-v1/media/introduction"/></para>
/// </summary>
[HttpClientApi(TokenManage = nameof(IFeishuAppManager), RegistryGroupName = "Drive", InheritedFrom = nameof(FeishuV1DriveSubscribe))]
[Token("UserAccessToken", Name = Consts.Authorization)]
public interface IFeishuUserV1DriveSubscribe : IFeishuV1DriveSubscribe, ICurrentUserId
{

    /// <summary>
    /// 获取订阅状态
    /// <para>根据订阅ID获取该订阅的状态。</para>
    /// <para><see href="https://open.feishu.cn/document/server-docs/docs/docs-assistant/file-subscription/get">接口文档</see></para>
    /// </summary>
    /// <param name="file_token">
    /// <para>云文档的 token。了解如何获取各类云文档的 token，参考[云空间常见问题](https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/reference/drive-v1/faq)。</para>
    /// <para>示例值：doccnfYZzTlvXqZIGTdAHKabcef</para>
    /// </param>
    /// <param name="subscription_id">
    /// <para>订阅关系ID</para>
    /// <para>示例值：1234567890987654321</para>
    /// </param>
    /// <param name="getFileSubscriptionRequest">获取订阅状态请求体</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    [Get("/open-apis/drive/v1/files/{file_token}/subscriptions/{subscription_id}")]
    Task<FeishuApiResult<FileSubscriptionOOpsResult>?> GetFileSubscriptionAsync(
         [Path] string file_token,
         [Path] string subscription_id,
         [Body] GetFileSubscriptionRequest getFileSubscriptionRequest,
         CancellationToken cancellationToken = default);


    /// <summary>
    /// 创建订阅
    /// <para>订阅文档中的变更事件，当前支持文档评论订阅，订阅后文档评论更新会有“云文档助手”推送给订阅的用户</para>
    /// <para><see href="https://open.feishu.cn/document/server-docs/docs/docs-assistant/file-subscription/create">接口文档</see></para>
    /// </summary>
    /// <param name="file_token">
    /// <para>云文档的 token。了解如何获取各类云文档的 token，参考[云空间常见问题](https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/reference/drive-v1/faq)。</para>
    /// <para>示例值：doccnfYZzTlvXqZIGTdAHKabcef</para>
    /// </param>
    /// <param name="createFileSubscriptionRequest">创建订阅请求体</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    [Post("/open-apis/drive/v1/files/{file_token}/subscriptions")]
    Task<FeishuApiResult<FileSubscriptionOOpsResult>?> CreateFileSubscriptionAsync(
        [Path] string file_token,
        [Body] CreateFileSubscriptionRequest createFileSubscriptionRequest,
        CancellationToken cancellationToken = default);
}