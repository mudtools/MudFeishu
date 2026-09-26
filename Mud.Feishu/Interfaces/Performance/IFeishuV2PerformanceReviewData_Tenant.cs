// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.DataModels.Performance;

namespace Mud.Feishu;


/// <summary>
/// 飞书绩效（Performance）「绩效详情数据」SDK（v2）用于获取被评估人各环节的绩效评估详情（不包含校准环节），如环节评估数据、环节提交状态等；相比 v1 获取绩效结果接口返回数据更丰富。本接口仅支持 tenant_access_token 调用。
/// <para>接口详细文档请参见：<see href="https://open.feishu.cn/document/performance-v1/review_data/query-2"/></para>
/// </summary>
[HttpClientApi(TokenManage = nameof(IFeishuAppManager), RegistryGroupName = "Performance")]
[Token(FeishuTokenTypes.TenantAccessToken, Name = Consts.Authorization)]
public interface IFeishuTenantV2PerformanceReviewData : IFeishuAppContextSwitcher
{
    /// <summary>
    /// 获取绩效详情数据
    /// <para>获取被评估人各环节的绩效评估详情（不包含校准环节），如环节评估数据、环节提交状态等；stage_types 与 stage_ids 至少要传一个，不传默认不返回任何环节评估数据。</para>
    /// <para>限频：20 次/分钟。所需权限（开启任一即可）：performance:performance（管理绩效数据）、performance:performance:readonly（查看绩效数据）；字段权限：contact:user.employee_id:readonly（user_id_type 取 user_id 时的返回字段）。</para>
    /// <para><see href="https://open.feishu.cn/document/performance-v1/review_data/query-2">接口文档</see></para>
    /// </summary>
    /// <param name="request">请求体（semester_ids 周期 ID 列表必填 0~10 个；reviewee_user_ids 被评估人 ID 列表必填 0~10 个；stage_types 环节类型与 stage_ids 环节 ID 至少传一个；review_stage_roles 评估型环节执行人角色；need_leader_review_data_source 是否返回终评数据来源；updated_later_than 更新时间下限，毫秒时间戳；stage_progresses 环节状态列表 0~50 个）</param>
    /// <param name="user_id_type">用户 ID 类型（open_id/union_id/user_id/people_admin_id），默认 open_id</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns>返回评估数据列表（datas：user_id/semester_id/activity_id/review_template_id/stages（环节、评估记录、评估内容明细、360°与合作项目上级记录信息））</returns>
    [Post("/open-apis/performance/v2/review_datas/query")]
    Task<FeishuApiResult<QueryReviewDataDetailResult>?> QueryReviewDataDetailAsync(
        [Body] QueryReviewDataDetailRequest request,
        [Query("user_id_type")] string? user_id_type = null,
        CancellationToken cancellationToken = default);
}
