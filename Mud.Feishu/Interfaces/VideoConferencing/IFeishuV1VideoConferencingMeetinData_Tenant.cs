// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.DataModels.VideoConferencing;

namespace Mud.Feishu;


/// <summary>
/// 用于分页查询一段时间内租户的会议数据，包括：查询会议明细、查询参会人明细、查询参会人会议质量数据、查询会议室预定数据。
/// <para>接口详细文档请参见：<see href="https://open.feishu.cn/document/server-docs/vc-v1/meeting-room-data/resource-introduction"/></para>
/// </summary>
[HttpClientApi(TokenManage = nameof(IFeishuAppManager), RegistryGroupName = "VideoConferencing", InheritedFrom = nameof(FeishuV1VideoConferencingMeetinData))]
[Token(FeishuTokenTypes.TenantAccessToken, Name = Consts.Authorization)]
public interface IFeishuTenantV1VideoConferencingMeetinData : IFeishuV1VideoConferencingMeetinData
{

    /// <summary>
    /// 获取告警记录
    /// <para>获取特定条件下租户的设备告警记录。</para>
    /// <para><see href="https://open.feishu.cn/document/server-docs/vc-v1/alert/list">接口文档</see></para>
    /// </summary>
    /// <param name="query_type">
    /// <para>查询对象类型，不填返回所有</para>
    /// <para>示例值：1</para>
    /// <list type="bullet">
    /// <item>1：会议室</item>
    /// <item>2：企业会议室连接器</item>
    /// <item>3：SIP会议室系统</item>
    /// </list>
    /// <para>默认值：null</para>
    /// </param>
    /// <param name="query_value">
    /// <para>查询对象ID，会议室ID或企业会议室连接器ID</para>
    /// <para>示例值：omm_4de32cf10a4358788ff4e09e37ebbf9b</para>
    /// <para>默认值：null</para>
    /// </param>
    /// <param name="start_time">
    /// <para>查询开始时间（unix时间，单位sec）</para>
    /// <para>示例值：1655276858</para>
    /// </param>
    /// <param name="end_time">
    /// <para>查询结束时间（unix时间，单位sec）</para>
    /// <para>示例值：1655276858</para>
    /// </param>
    /// <param name="page_size">分页大小，即本次请求所返回的用户信息列表内的最大条目数。默认值：20</param>
    /// <param name="page_token">分页标记，第一次请求不填，表示从头开始遍历；分页查询结果还有更多项时会同时返回新的 page_token，下次遍历可采用该 page_token 获取查询结果</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    [Get("/open-apis/vc/v1/alerts")]
    Task<FeishuApiPageListResult<AlertInfo>?> GetAlertPageListAsync(
       [Query] string start_time,
       [Query] string end_time,
       [Query] int? query_type = null,
       [Query] string? query_value = null,
       [Query] int? page_size = Consts.PageSize_20,
       [Query] string? page_token = null,
       CancellationToken cancellationToken = default);
}