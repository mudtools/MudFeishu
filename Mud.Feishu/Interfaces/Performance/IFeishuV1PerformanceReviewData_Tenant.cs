// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.DataModels.Performance;

namespace Mud.Feishu;


/// <summary>
/// 飞书绩效（Performance）「绩效结果」SDK（v1）用于获取被评估人在指定周期、指定项目中各个环节的评估结果信息，包含绩效所在的周期、项目、评估项、评估模版以及各环节评估数据等信息。仅支持 tenant_access_token 或 user_access_token 调用；tenant_access_token 鉴权模式下推荐使用 v2 的获取绩效详情数据接口获取更丰富的返回数据。
/// <para>接口详细文档请参见：<see href="https://open.feishu.cn/document/server-docs/performance-v1/query"/></para>
/// </summary>
[HttpClientApi(TokenManage = nameof(IFeishuAppManager), RegistryGroupName = "Performance")]
[Token(FeishuTokenTypes.TenantAccessToken, Name = Consts.Authorization)]
public interface IFeishuTenantV1PerformanceReviewData : IFeishuAppContextSwitcher
{
    /// <summary>
    /// 获取绩效结果
    /// <para>获取被评估人在指定周期、指定项目中各个环节的评估结果信息，包含绩效所在的周期、项目、评估项、评估模版以及各环节评估数据等信息。</para>
    /// <para>限频：20 次/分钟。所需权限（开启任一即可）：performance:performance（管理绩效数据）、performance:performance:readonly（查看绩效数据）；字段权限：contact:user.employee_id:readonly（user_id_type 取 user_id 时的返回字段）。</para>
    /// <para><see href="https://open.feishu.cn/document/server-docs/performance-v1/query">接口文档</see></para>
    /// </summary>
    /// <param name="request">请求体（start_time/end_time 周期时间范围必填，填写 semester_id_list 时无效；stage_types 环节类型必填，仅支持 leader_review/communication_and_open_result/view_result；stage_progress 环节状态 0~4；semester_id_list 周期 ID 列表最大 50 个；reviewee_user_id_list 被评估人 ID 列表必填最大 50 个；updated_later_than 更新时间下限，毫秒时间戳）</param>
    /// <param name="user_id_type">用户 ID 类型（open_id/union_id/user_id/people_admin_id），默认 open_id</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns>返回周期列表（semesters）、项目列表（activities）、评估项列表（indicators）、评估模板列表（templates）、评估内容列表（units）、评估字段列表（fields）与评估数据列表（datas）</returns>
    [Post("/open-apis/performance/v1/review_datas/query")]
    Task<FeishuApiResult<QueryReviewDataResult>?> QueryReviewDataAsync(
        [Body] QueryReviewDataRequest request,
        [Query("user_id_type")] string? user_id_type = null,
        CancellationToken cancellationToken = default);
}
