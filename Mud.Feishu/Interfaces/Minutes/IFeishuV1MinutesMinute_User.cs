// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.DataModels.Minutes;

namespace Mud.Feishu;


/// <summary>
/// 飞书妙记（Minutes）SDK 是一组服务端 OpenAPI 的封装，用于以用户身份创建妙记剪辑、导入云盘音视频生成妙记、订阅/取消订阅妙记变更事件，并继承双令牌只读端点（基础信息、音视频下载、文字记录、统计数据、AI 产物、搜索）。
/// <para>接口详细文档请参见：<see href="https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/minutes-v1/minute/clip"/></para>
/// </summary>
[HttpClientApi(TokenManage = nameof(IFeishuAppManager), RegistryGroupName = "Minutes", InheritedFrom = nameof(FeishuV1MinutesMinute))]
[Token(FeishuTokenTypes.UserAccessToken, Name = Consts.Authorization)]
public interface IFeishuUserV1MinutesMinute : IFeishuV1MinutesMinute, ICurrentUserId
{
    /// <summary>
    /// 创建妙记剪辑
    /// <para>基于已完成的妙记与指定时间段创建妙记剪辑，响应成功表示已提交创建，转写与音视频文件在后台异步生成。</para>
    /// <para>限频：5 次/秒。所需权限：minutes:minutes.clip:write（创建妙记剪辑）。每个时间段须大于 1000 毫秒，重叠或相邻区间会自动合并。</para>
    /// <para><see href="https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/minutes-v1/minute/clip">接口文档</see></para>
    /// </summary>
    /// <param name="minute_token">妙记唯一标识，示例值：mt_123456789abcdef0123456789abcdef0</param>
    /// <param name="request">剪辑请求体（time_ranges 必填，1～50 个时间段；可选 title 剪辑标题）</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns>返回剪辑妙记的链接（minute_url）</returns>
    [Post("/open-apis/minutes/v1/minutes/{minute_token}/clip")]
    Task<FeishuApiResult<MinuteUrlResult>?> ClipMinuteAsync(
        [Path] string minute_token,
        [Body] ClipMinuteRequest request,
        CancellationToken cancellationToken = default);


    /// <summary>
    /// 导入云盘文件生成妙记
    /// <para>基于云盘音视频文件生成妙记。支持音频 wav/mp3/m4a/aac/ogg/wma/amr，视频 avi/wmv/mov/mp4/m4v/mpeg/ogg/flv，音频时长不超过 6 小时，文件最大 6GB。</para>
    /// <para>限频：5 次/秒。所需权限：minutes:minutes.upload:write（从音视频文件生成妙记）。用户身份调用时用户须具备云盘文件下载权限。</para>
    /// <para><see href="https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/minutes-v1/minute/upload">接口文档</see></para>
    /// </summary>
    /// <param name="request">导入请求体（file_token 必填：音视频云盘文件 token）</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns>返回生成的妙记链接（minute_url）</returns>
    [Post("/open-apis/minutes/v1/minutes/upload")]
    Task<FeishuApiResult<MinuteUrlResult>?> UploadMinuteAsync(
        [Body] UploadMinuteRequest request,
        CancellationToken cancellationToken = default);


    /// <summary>
    /// 订阅妙记变更事件
    /// <para>订阅当前用户身份相关的妙记变更事件，通过指定事件类型订阅不同的变更。</para>
    /// <para>限频：1000 次/分钟且 50 次/秒。所需权限：minutes:minutes.basic:read（读取妙记基本信息）。</para>
    /// <para><see href="https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/minutes-v1/minute/subscription">接口文档</see></para>
    /// </summary>
    /// <param name="request">订阅请求体（event_type 可选：minutes.minute.generated_v1 表示妙记生成事件）</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns>订阅成功时 data 为空对象</returns>
    [Post("/open-apis/minutes/v1/minutes/subscription")]
    Task<FeishuNullDataApiResult?> SubscribeMinuteAsync(
        [Body] MinuteSubscriptionRequest request,
        CancellationToken cancellationToken = default);


    /// <summary>
    /// 取消订阅妙记变更事件
    /// <para>取消订阅当前用户身份相关的妙记变更事件，通过指定事件类型取消对应的订阅。</para>
    /// <para>限频：1000 次/分钟且 50 次/秒。所需权限：minutes:minutes.basic:read（读取妙记基本信息）。</para>
    /// <para><see href="https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/minutes-v1/minute/unsubscription">接口文档</see></para>
    /// </summary>
    /// <param name="request">取消订阅请求体（event_type 可选：minutes.minute.generated_v1 表示妙记生成事件）</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns>取消成功时 data 为空对象</returns>
    [Post("/open-apis/minutes/v1/minutes/unsubscription")]
    Task<FeishuNullDataApiResult?> UnsubscribeMinuteAsync(
        [Body] MinuteSubscriptionRequest request,
        CancellationToken cancellationToken = default);
}
