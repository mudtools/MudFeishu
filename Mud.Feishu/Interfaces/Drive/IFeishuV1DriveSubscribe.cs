// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.DataModels.Drive;

namespace Mud.Feishu.Interfaces;

/// <summary>
/// 云文档事件订阅，用于订阅云文档的事件，如文件创建、更新、删除等，当云文档发生指定事件时，系统会向配置的地址发送事件通知。
/// <para>接口详细文档请参见：<see href="https://open.feishu.cn/document/server-docs/docs/drive-v1/media/introduction"/></para>
/// </summary>
[HttpClientApi(TokenManage = nameof(IFeishuAppManager), IsAbstract = true)]
[Token("TenantAccessToken", Name = Consts.Authorization)]
public interface IFeishuV1DriveSubscribe : IFeishuAppContextSwitcher
{

    /// <summary>
    /// 订阅云文档事件
    /// <para>订阅云文档的各类通知事件。调用该接口并在开发者后台添加事件后，当云文档发生指定事件时，系统会向配置的地址发送事件。</para>
    /// <para><see href="https://open.feishu.cn/document/server-docs/docs/drive-v1/event/subscribe">接口文档</see></para>
    /// </summary>
    /// <param name="file_token">
    /// <para>云文档的 token。了解如何获取各类云文档的 token，参考[云空间常见问题](https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/reference/drive-v1/faq)。</para>
    /// <para>示例值：doccnfYZzTlvXqZIGTdAHKabcef</para>
    /// </param>
    /// <param name="file_type">
    /// <para>云文档类型</para>
    /// <para>示例值：docx</para>
    /// <list type="bullet">
    /// <item>doc：旧版文档。已不推荐使用</item>
    /// <item>docx：新版文档</item>
    /// <item>sheet：电子表格</item>
    /// <item>bitable：多维表格</item>
    /// <item>file：文件</item>
    /// <item>folder：文件夹</item>
    /// <item>slides：幻灯片</item>
    /// </list>
    /// </param>
    /// <param name="event_type">
    /// <para>事件类型。</para>
    /// <para>- 若 `file_type` 为 `folder`，需要填写该字段，且字段必须填写为 `file.created_in_folder_v1`，表示订阅[文件夹下文件创建](https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/reference/drive-v1/file/events/created_in_folder)事件</para>
    /// <para>- 若 `file_type` 不为 `folder`，请勿填写该字段。对于文档、电子表格、多维表格等云文档类型，目前仅支持订阅所有相关的云文档事件，暂不支持只订阅该云文档类型下的某个或某些事件</para>
    /// <para>示例值：file.created_in_folder_v1</para>
    /// <para>默认值：null</para>
    /// </param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    [Post("/open-apis/drive/v1/files/{file_token}/subscribe")]
    Task<FeishuNullDataApiResult?> SubscribeFileEventAsync(
      [Path] string file_token,
      [Query("file_type")] string file_type,
      [Query("event_type")] string? event_type = null,
      CancellationToken cancellationToken = default);


    /// <summary>
    /// 查询云文档事件订阅状态
    /// <para>用于查询云文档事件的订阅状态。了解事件订阅的配置流程和使用场景，参考事件概述。</para>
    /// <para><see href="https://open.feishu.cn/document/docs/drive-v1/event/get_subscribe">接口文档</see></para>
    /// </summary>
    /// <param name="file_token">
    /// <para>云文档的 token。了解如何获取各类云文档的 token，参考[云空间常见问题](https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/reference/drive-v1/faq)。</para>
    /// <para>示例值：doccnfYZzTlvXqZIGTdAHKabcef</para>
    /// </param>
    /// <param name="file_type">
    /// <para>云文档类型</para>
    /// <para>示例值：docx</para>
    /// <list type="bullet">
    /// <item>doc：旧版文档。已不推荐使用</item>
    /// <item>docx：新版文档</item>
    /// <item>sheet：电子表格</item>
    /// <item>bitable：多维表格</item>
    /// <item>file：文件</item>
    /// <item>folder：文件夹</item>
    /// <item>slides：幻灯片</item>
    /// </list>
    /// </param>
    /// <param name="event_type">
    /// <para>事件类型。</para>
    /// <para>- 若 `file_type` 为 `folder`，需要填写该字段，且字段必须填写为 `file.created_in_folder_v1`，表示订阅[文件夹下文件创建](https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/reference/drive-v1/file/events/created_in_folder)事件</para>
    /// <para>- 若 `file_type` 不为 `folder`，请勿填写该字段。对于文档、电子表格、多维表格等云文档类型，目前仅支持订阅所有相关的云文档事件，暂不支持只订阅该云文档类型下的某个或某些事件</para>
    /// <para>示例值：file.created_in_folder_v1</para>
    /// <para>默认值：null</para>
    /// </param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    [Get("/open-apis/drive/v1/files/{file_token}/get_subscribe")]
    Task<FeishuApiResult<GetFileSubscribeResult>?> GetFileSubscribeAsync(
       [Path] string file_token,
       [Query("file_type")] string file_type,
       [Query("event_type")] string? event_type = null,
       CancellationToken cancellationToken = default);


    /// <summary>
    /// 取消云文档事件订阅
    /// <para>用于取消订阅云文档的通知事件。</para>
    /// <para><see href="https://open.feishu.cn/document/server-docs/docs/drive-v1/event/delete_subscribe">接口文档</see></para>
    /// </summary>
    /// <param name="file_token">
    /// <para>云文档的 token。了解如何获取各类云文档的 token，参考[云空间常见问题](https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/reference/drive-v1/faq)。</para>
    /// <para>示例值：doccnfYZzTlvXqZIGTdAHKabcef</para>
    /// </param>
    /// <param name="file_type">
    /// <para>云文档类型</para>
    /// <para>示例值：docx</para>
    /// <list type="bullet">
    /// <item>doc：旧版文档。已不推荐使用</item>
    /// <item>docx：新版文档</item>
    /// <item>sheet：电子表格</item>
    /// <item>bitable：多维表格</item>
    /// <item>file：文件</item>
    /// <item>folder：文件夹</item>
    /// <item>slides：幻灯片</item>
    /// </list>
    /// </param>
    /// <param name="event_type">
    /// <para>事件类型。</para>
    /// <para>- 若 `file_type` 为 `folder`，需要填写该字段，且字段必须填写为 `file.created_in_folder_v1`，表示订阅[文件夹下文件创建](https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/reference/drive-v1/file/events/created_in_folder)事件</para>
    /// <para>- 若 `file_type` 不为 `folder`，请勿填写该字段。对于文档、电子表格、多维表格等云文档类型，目前仅支持订阅所有相关的云文档事件，暂不支持只订阅该云文档类型下的某个或某些事件</para>
    /// <para>示例值：file.created_in_folder_v1</para>
    /// <para>默认值：null</para>
    /// </param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    [Delete("/open-apis/drive/v1/files/{file_token}/delete_subscribe")]
    Task<FeishuNullDataApiResult?> UnsubscribeFileEventAsync(
      [Path] string file_token,
      [Query("file_type")] string file_type,
      [Query("event_type")] string? event_type = null,
      CancellationToken cancellationToken = default);


    /// <summary>
    /// 订阅用户云文档事件
    /// <para>订阅用户云文档的各类通知事件，调用后目前可获取接收者视角的云文档评论、回复添加事件 ，未来 还会陆续扩充其它通知事件。</para>
    /// <para><see href="https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/reference/drive-v1/user/subscription">接口文档</see></para>
    /// </summary>
    /// <param name="subscribeUserFileEventRequest">订阅用户云文档事件请求体</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    [Post("/open-apis/drive/v1/user/subscription")]
    Task<FeishuNullDataApiResult?> SubscribeUserFileEventAsync(
      [Body] SubscribeUserFileEventRequest subscribeUserFileEventRequest,
      CancellationToken cancellationToken = default);


    /// <summary>
    /// 取消用户云文档事件订阅
    /// <para>用于取消订阅用户云文档的通知事件。取消订阅后，用户将不再收到云文档评论、回复添加事件。</para>
    /// <para><see href="https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/reference/drive-v1/user/remove_subscription">接口文档</see></para>
    /// </summary>
    /// <param name="event_type">
    /// <para>事件类型。</para>
    /// <para>- 若 `file_type` 为 `folder`，需要填写该字段，且字段必须填写为 `file.created_in_folder_v1`，表示订阅[文件夹下文件创建](https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/reference/drive-v1/file/events/created_in_folder)事件</para>
    /// <para>- 若 `file_type` 不为 `folder`，请勿填写该字段。对于文档、电子表格、多维表格等云文档类型，目前仅支持订阅所有相关的云文档事件，暂不支持只订阅该云文档类型下的某个或某些事件</para>
    /// <para>示例值：file.created_in_folder_v1</para>
    /// <para>默认值：null</para>
    /// </param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    [Delete("/open-apis/drive/v1/user/remove_subscription")]
    Task<FeishuNullDataApiResult?> UnsubscribeUserFileEventAsync(
        [Query("event_type")] string? event_type = null,
        CancellationToken cancellationToken = default);
}