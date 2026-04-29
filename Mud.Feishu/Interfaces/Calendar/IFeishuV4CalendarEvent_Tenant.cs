// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu;

/// <summary>
/// 日程是存在于日历内的实例资源，开发人员可以通过关联特定日期或时间段、参与人、地点等规则，构建指定主题内容的工作安排。
/// <para>例如，个人工作提醒、团队会议沟通、活动直播等类型的日程。开发人员可以通过日程资源 API 构建与管理日程。</para>
/// <para>接口详细文档请参见：<see href="https://open.feishu.cn/document/server-docs/calendar-v4/calendar-event/introduction"/></para>
/// </summary>
[HttpClientApi(RegistryGroupName = "Calendar", TokenManage = nameof(IFeishuAppManager), InheritedFrom = nameof(FeishuV4CalendarEvent))]
[Token("TenantAccessToken", Name = Consts.Authorization)]
public interface IFeishuTenantV4CalendarEvent : IFeishuV4CalendarEvent
{

    /// <summary>
    /// 删除请假日程
    /// <para>删除一个指定的请假日程。请假日程删除后，用户个人签名页的请假信息也会消失。</para>
    /// <para><see href="https://open.feishu.cn/document/server-docs/calendar-v4/timeoff_event/delete">接口文档</see></para>
    /// </summary> 
    /// <param name="timeoff_event_id">
    /// <para>请假日程 ID，在创建请假日程时从返回结果中获取。</para>
    /// <para>示例值：timeoff:XXXXXX-XXXX-0917-1623-aa493d591a39</para>
    /// </param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    [Delete("/open-apis/calendar/v4/timeoff_events/{timeoff_event_id}")]
    Task<FeishuNullDataApiResult?> DeleteTimeoffEventAsync(
       [Query] string timeoff_event_id,
       CancellationToken cancellationToken = default);
}