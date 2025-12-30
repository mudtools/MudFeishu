// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.DataModels.TaskCustomFields;

namespace Mud.Feishu;

/// <summary>
/// 任务功能支持在任务中扩充自定义字段，更清晰地添加任务关键信息，高效管理任务，辅助协作推进。
/// <para>任务的使用者可以在使用“任务截止时间”，“任务负责人”……等系统字段之外，自行定义如”优先级“，”项目发布日期“，”价格“等和使用场景密切相关的字段。</para>
/// </summary>
[HttpClientApi(TokenManage = nameof(ITokenManager), IsAbstract = true)]
[Header(Consts.Authorization)]
public interface IFeishuV2TaskCustomFields
{
    /// <summary>
    /// <para>创建一个自定义字段，并将其加入一个资源上（目前资源只支持清单）。</para>
    /// <para>创建自定义字段必须提供字段名称，类型和相应类型的设置。</para>
    /// </summary>
    /// <param name="createCustomFieldsRequest">创建自定义字段请求体。</param>
    /// <param name="user_id_type">用户 ID，ID 类型需要与查询参数中的 user_id_type 类型保持一致。</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns></returns>
    [Post("/open-apis/task/v2/custom_fields")]
    Task<FeishuApiResult<CustomFieldsResult>?> CreateCustomFieldsAsync(
        [Body] CreateCustomFieldsRequest createCustomFieldsRequest,
        [Query("user_id_type")] string user_id_type = Consts.User_Id_Type,
        CancellationToken cancellationToken = default);
}
