// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.DataModels.Hire;

namespace Mud.Feishu;


/// <summary>
/// 飞书招聘（Hire）人才管理域 SDK 是一组服务端 OpenAPI 的封装，用于人才档案的创建、更新与查询（v1/v2）、人才库与人才文件夹管理、人才标签操作、批量获取人才 ID、在职状态维护及黑名单变更。本接口全部端点仅支持 tenant_access_token 调用。
/// <para>接口详细文档请参见：<see href="https://open.feishu.cn/document/server-docs/hire-v1/candidate-management/talent/list"/></para>
/// </summary>
[HttpClientApi(TokenManage = nameof(IFeishuAppManager), RegistryGroupName = "Hire")]
[Token(FeishuTokenTypes.TenantAccessToken, Name = Consts.Authorization)]
public interface IFeishuTenantV1HireTalent : IFeishuAppContextSwitcher
{
    /// <summary>
    /// 批量加入/移除人才库中人才
    /// <para>对同一个人才库批量执行人才加入或移除操作；不存在的 ID 报错返回，已在/不在库中则静默处理。</para>
    /// <para>限频：10 次/秒。所需权限：hire:talent_folder（更新人才库信息）。</para>
    /// <para><see href="https://open.feishu.cn/document/hire-v1/candidate-management/talent_pool/batch_change_talent_pool">接口文档</see></para>
    /// </summary>
    /// <param name="talent_pool_id">人才库 ID，可通过获取人才库列表接口获取，示例值：6930815272790114324</param>
    /// <param name="request">请求体（talent_id_list 必填：人才 ID 列表，1～50 个；option_type 必填：1 加入 / 2 移除）</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns>成功时 data 为空对象</returns>
    [Post("/open-apis/hire/v1/talent_pools/{talent_pool_id}/batch_change_talent_pool")]
    Task<FeishuNullDataApiResult?> BatchChangeTalentPoolAsync(
        [Path] string talent_pool_id,
        [Body] BatchChangeTalentPoolRequest request,
        CancellationToken cancellationToken = default);


    /// <summary>
    /// 获取人才库列表
    /// <para>分页获取人才库列表，返回人才库 ID、中英文名称与描述、父子关系、可见性与创建/修改时间。</para>
    /// <para>限频：1000 次/分钟、50 次/秒。所需权限：hire:talent_folder:readonly（获取人才库信息）或 hire:talent_folder（更新人才库信息）。</para>
    /// <para><see href="https://open.feishu.cn/document/server-docs/hire-v1/candidate-management/talent_pool/search">接口文档</see></para>
    /// </summary>
    /// <param name="page_size">每页数量，默认 10，最大 100</param>
    /// <param name="page_token">分页标记，首次请求不填，翻页时取上一次返回的 page_token</param>
    /// <param name="id_list">人才库 ID 列表，传入时按列表返回，最大 50 个；不传返回全部</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns>返回人才库分页列表（items、page_token、has_more）</returns>
    [Get("/open-apis/hire/v1/talent_pools")]
    Task<FeishuApiResult<GetTalentPoolListResult>?> GetTalentPoolListAsync(
        [Query("page_size")] int? page_size = null,
        [Query("page_token")] string? page_token = null,
        [Query("id_list")] string[]? id_list = null,
        CancellationToken cancellationToken = default);


    /// <summary>
    /// 将人才加入人才库
    /// <para>将单个人才加入指定人才库，并可选择加入后是否从其他人才库移出。</para>
    /// <para>限频：1000 次/分钟、50 次/秒。所需权限：hire:talent_folder（更新人才库信息）。</para>
    /// <para><see href="https://open.feishu.cn/document/server-docs/hire-v1/candidate-management/talent_pool/move_talent">接口文档</see></para>
    /// </summary>
    /// <param name="talent_pool_id">人才库 ID，可通过获取人才库列表接口获取，示例值：6930815272790114324</param>
    /// <param name="request">请求体（talent_id 必填：人才 ID；add_type 必填：1 不从其他库移出 / 2 从其他库移出）</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns>返回人才库 ID 与人才 ID（talent_pool_id、talent_id）</returns>
    [Post("/open-apis/hire/v1/talent_pools/{talent_pool_id}/talent_relationship")]
    Task<FeishuApiResult<AddTalentToTalentPoolResult>?> AddTalentToTalentPoolAsync(
        [Path] string talent_pool_id,
        [Body] AddTalentToTalentPoolRequest request,
        CancellationToken cancellationToken = default);


    /// <summary>
    /// 操作人才标签
    /// <para>为人才批量新增或删除标签。</para>
    /// <para>限频：20 次/秒。所需权限：hire:talent（更新人才信息）。</para>
    /// <para><see href="https://open.feishu.cn/document/hire-v1/candidate-management/talent/tag">接口文档</see></para>
    /// </summary>
    /// <param name="talent_id">人才 ID，可通过批量获取人才 ID 接口获取，示例值：6930815272790114324</param>
    /// <param name="request">请求体（operation 必填：1 新增 / 2 删除；tag_id_list 必填：标签 ID 列表）</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns>成功时 data 为空对象</returns>
    [Post("/open-apis/hire/v1/talents/{talent_id}/tag")]
    Task<FeishuNullDataApiResult?> OperateTalentTagAsync(
        [Path] string talent_id,
        [Body] OperateTalentTagRequest request,
        CancellationToken cancellationToken = default);


    /// <summary>
    /// 创建人才
    /// <para>在企业内创建一个人才（简历级聚合写入）；预置姓名必填，邮箱/手机是否必填取决于飞书招聘标准简历模板设置。</para>
    /// <para>限频：1000 次/分钟、50 次/秒。所需权限：hire:talent（更新人才信息）。字段权限：contact:user.employee_id:readonly（返回的用户 ID 字段）。</para>
    /// <para><see href="https://open.feishu.cn/document/server-docs/hire-v1/candidate-management/talent/combined_create">接口文档</see></para>
    /// </summary>
    /// <param name="request">创建请求体（basic_info 必填；education_list 等各子经历数组最大 100 条）</param>
    /// <param name="user_id_type">用户 ID 类型（open_id/union_id/user_id/people_admin_id），默认 open_id；取 user_id 时需 contact:user.employee_id:readonly 字段权限</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns>返回人才 ID 与创建者信息（talent_id、creator_id、creator_account_type）</returns>
    [Post("/open-apis/hire/v1/talents/combined_create")]
    Task<FeishuApiResult<CombinedCreateTalentResult>?> CombinedCreateTalentAsync(
        [Body] CombinedCreateTalentRequest request,
        [Query("user_id_type")] string? user_id_type = null,
        CancellationToken cancellationToken = default);


    /// <summary>
    /// 更新人才信息
    /// <para>按人才 ID 全量聚合更新企业内人才信息；各子经历数组为全量覆盖，邮箱/手机必填性取决于标准简历模板设置。</para>
    /// <para>限频：1000 次/分钟、50 次/秒。所需权限：hire:talent（更新人才信息）。字段权限：contact:user.employee_id:readonly（返回的用户 ID 字段）。</para>
    /// <para><see href="https://open.feishu.cn/document/server-docs/hire-v1/candidate-management/talent/combined_update">接口文档</see></para>
    /// </summary>
    /// <param name="request">更新请求体（talent_id 必填；basic_info 必填；operator_id/operator_account_type 为更新者信息）</param>
    /// <param name="user_id_type">用户 ID 类型（open_id/union_id/user_id/people_admin_id），默认 open_id；取 user_id 时需 contact:user.employee_id:readonly 字段权限</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns>返回人才 ID 与更新者信息（talent_id、operator_id、operator_account_type）</returns>
    [Post("/open-apis/hire/v1/talents/combined_update")]
    Task<FeishuApiResult<CombinedUpdateTalentResult>?> CombinedUpdateTalentAsync(
        [Body] CombinedUpdateTalentRequest request,
        [Query("user_id_type")] string? user_id_type = null,
        CancellationToken cancellationToken = default);


    /// <summary>
    /// 人才加入文件夹
    /// <para>把一批人才加入到指定文件夹。</para>
    /// <para>限频：1000 次/分钟、50 次/秒。所需权限：hire:talent（更新人才信息）或 hire:talent_folder_association（更新人才文件夹关联信息）。</para>
    /// <para><see href="https://open.feishu.cn/document/server-docs/hire-v1/candidate-management/talent/add_to_folder">接口文档</see></para>
    /// </summary>
    /// <param name="request">请求体（talent_id_list 必填：人才 ID 列表，1～200 个；folder_id 必填：文件夹 ID）</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns>返回回显的人才 ID 列表与文件夹 ID（talent_id_list、folder_id）</returns>
    [Post("/open-apis/hire/v1/talents/add_to_folder")]
    Task<FeishuApiResult<TalentToFolderResult>?> AddTalentToFolderAsync(
        [Body] TalentToFolderRequest request,
        CancellationToken cancellationToken = default);


    /// <summary>
    /// 人才移出文件夹
    /// <para>按人才 ID 列表把人才从指定文件夹移出。</para>
    /// <para>限频：10 次/秒。所需权限：hire:talent_folder_association（更新人才文件夹关联信息）。</para>
    /// <para><see href="https://open.feishu.cn/document/hire-v1/candidate-management/talent/remove_to_folder">接口文档</see></para>
    /// </summary>
    /// <param name="request">请求体（talent_id_list 必填：人才 ID 列表，1～200 个；folder_id 必填：文件夹 ID）</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns>返回回显的人才 ID 列表与文件夹 ID（talent_id_list、folder_id）</returns>
    [Post("/open-apis/hire/v1/talents/remove_to_folder")]
    Task<FeishuApiResult<TalentToFolderResult>?> RemoveTalentFromFolderAsync(
        [Body] TalentToFolderRequest request,
        CancellationToken cancellationToken = default);


    /// <summary>
    /// 获取人才文件夹信息
    /// <para>分页获取招聘系统中的人才文件夹列表（文件夹维度的人才库）。</para>
    /// <para>限频：1000 次/分钟、50 次/秒。所需权限：hire:talent_folder:readonly（获取人才库信息）或 hire:talent_folder（更新人才库信息）。字段权限：contact:user.employee_id:readonly（user_id_type=user_id 时返回的用户 ID 字段）。</para>
    /// <para><see href="https://open.feishu.cn/document/server-docs/hire-v1/candidate-management/talent/list-2">接口文档</see></para>
    /// </summary>
    /// <param name="page_size">每页数量，默认 10，最大 100</param>
    /// <param name="page_token">分页标记，首次请求不填，翻页时取上一次返回的 page_token</param>
    /// <param name="user_id_type">用户 ID 类型（open_id/union_id/user_id/people_admin_id），默认 open_id；取 user_id 时需 contact:user.employee_id:readonly 字段权限</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns>返回人才文件夹分页列表（items、page_token、has_more）</returns>
    [Get("/open-apis/hire/v1/talent_folders")]
    Task<FeishuApiResult<GetTalentFolderListResult>?> GetTalentFolderListAsync(
        [Query("page_size")] int? page_size = null,
        [Query("page_token")] string? page_token = null,
        [Query("user_id_type")] string? user_id_type = null,
        CancellationToken cancellationToken = default);


    /// <summary>
    /// 根据手机号或邮箱获取人才 ID
    /// <para>通过手机号、邮箱或证件号批量反查人才 ID 及基础信息；三类条件为且（AND）关系，至少传入一种，最多各 100 条。</para>
    /// <para>限频：1000 次/分钟、50 次/秒。所需权限：hire:talent:readonly（获取人才信息）或 hire:talent（更新人才信息）。字段权限：hire:talent_onboard_status:readonly（返回的入职状态字段）。</para>
    /// <para><see href="https://open.feishu.cn/document/server-docs/hire-v1/candidate-management/talent/batch_get_id">接口文档</see></para>
    /// </summary>
    /// <param name="request">请求体（mobile_code、mobile_number_list、email_list、identification_type、identification_number_list 选填）</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns>返回人才信息列表（talent_list）</returns>
    [Post("/open-apis/hire/v1/talents/batch_get_id")]
    Task<FeishuApiResult<BatchGetTalentIdResult>?> BatchGetTalentIdAsync(
        [Body] BatchGetTalentIdRequest request,
        CancellationToken cancellationToken = default);


    /// <summary>
    /// 获取人才列表
    /// <para>按关键词、更新时间等条件分页获取候选人（人才）全量档案列表（查询参数采用查询对象模式 <see cref="TalentListQuery"/>，见 AGENTS.md API-2）。</para>
    /// <para>限频：20 次/秒。所需权限：hire:talent:readonly（获取人才信息）或 hire:talent（更新人才信息）。字段权限：contact:user.employee_id:readonly（user_id_type=user_id 时返回的用户 ID 字段）。</para>
    /// <para><see href="https://open.feishu.cn/document/server-docs/hire-v1/candidate-management/talent/list">接口文档</see></para>
    /// </summary>
    /// <param name="query">关键词、更新时间范围、分页、排序与 ID 类型查询参数</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns>返回人才分页列表（items、has_more、page_token）</returns>
    [Get("/open-apis/hire/v1/talents")]
    Task<FeishuApiResult<GetTalentListResult>?> GetTalentListAsync(
        [Query] TalentListQuery? query = null,
        CancellationToken cancellationToken = default);


    /// <summary>
    /// 获取人才字段
    /// <para>获取人才档案的字段（模块）配置清单，含字段类型、选项与自定义字段。</para>
    /// <para>限频：10 次/秒。所需权限：hire:talent:readonly（获取人才信息）或 hire:talent（更新人才信息）。</para>
    /// <para><see href="https://open.feishu.cn/document/server-docs/hire-v1/candidate-management/talent/query">接口文档</see></para>
    /// </summary>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns>返回模块（字段组）列表（items）</returns>
    [Get("/open-apis/hire/v1/talent_objects/query")]
    Task<FeishuApiResult<QueryTalentObjectResult>?> QueryTalentObjectAsync(
        CancellationToken cancellationToken = default);


    /// <summary>
    /// 获取人才信息（v1）
    /// <para>按人才 ID 获取候选人（人才）完整信息，返回 v1 版 talent 结构。</para>
    /// <para>限频：20 次/秒。所需权限：hire:talent:readonly（获取人才信息）或 hire:talent（更新人才信息）。字段权限：contact:user.employee_id:readonly（user_id_type=user_id 时返回的用户 ID 字段）。</para>
    /// <para><see href="https://open.feishu.cn/document/server-docs/hire-v1/candidate-management/talent/get">接口文档</see></para>
    /// </summary>
    /// <param name="talent_id">人才 ID，可通过获取人才列表接口获取，示例值：6930815272790114324</param>
    /// <param name="user_id_type">用户 ID 类型（open_id/union_id/user_id/people_admin_id），默认 people_admin_id；取 user_id 时需 contact:user.employee_id:readonly 字段权限</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns>返回人才信息（talent）</returns>
    [Get("/open-apis/hire/v1/talents/{talent_id}")]
    Task<FeishuApiResult<GetTalentResult>?> GetTalentAsync(
        [Path] string talent_id,
        [Query("user_id_type")] string? user_id_type = null,
        CancellationToken cancellationToken = default);


    /// <summary>
    /// 获取人才详细信息（v2）
    /// <para>按人才 ID 获取人才 v2 版详细信息（data 直接为 composite_talent，含人才库、文件夹、标签、黑名单、备注等扩展数据）；不支持查询已删除人才。</para>
    /// <para>限频：20 次/秒。所需权限：hire:talent:readonly（获取人才信息）或 hire:talent（更新人才信息）；备注/人才库/标签/黑名单等字段另需对应字段权限。</para>
    /// <para><see href="https://open.feishu.cn/document/server-docs/hire-v1/candidate-management/talent/get-2">接口文档</see></para>
    /// </summary>
    /// <param name="talent_id">人才 ID，示例值：6930815272790114324</param>
    /// <param name="user_id_type">用户 ID 类型（open_id/union_id/user_id/people_admin_id），默认 people_admin_id；取 user_id 时需 contact:user.employee_id:readonly 字段权限</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns>返回 v2 版人才聚合信息（talent_id、basic_info、各经历列表、人才库/标签/黑名单等）</returns>
    [Get("/open-apis/hire/v2/talents/{talent_id}")]
    Task<FeishuApiResult<GetTalentV2Result>?> GetTalentV2Async(
        [Path] string talent_id,
        [Query("user_id_type")] string? user_id_type = null,
        CancellationToken cancellationToken = default);


    /// <summary>
    /// 更新人才在职状态
    /// <para>标记人才「入职/离职」；仅适用于未通过飞书招聘投递入职的候选人，且通过本接口标记入职的只能用本接口标记离职。仅自建应用可用。</para>
    /// <para>限频：20 次/分钟。所需权限：hire:talent（更新人才信息）。</para>
    /// <para><see href="https://open.feishu.cn/document/server-docs/hire-v1/candidate-management/talent/onboard_status">接口文档</see></para>
    /// </summary>
    /// <param name="talent_id">人才 ID，示例值：6930815272790114324</param>
    /// <param name="request">请求体（operation 必填：1 入职 / 2 离职；onboard_time/overboard_time 按操作类型必填，毫秒时间戳）</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns>成功时 data 为空对象</returns>
    [Post("/open-apis/hire/v1/talents/{talent_id}/onboard_status")]
    Task<FeishuNullDataApiResult?> UpdateTalentOnboardStatusAsync(
        [Path] string talent_id,
        [Body] UpdateTalentOnboardStatusRequest request,
        CancellationToken cancellationToken = default);


    /// <summary>
    /// 加入/移出人才黑名单
    /// <para>按人才 ID 将人才加入或移出招聘黑名单。仅自建应用可用。</para>
    /// <para>限频：10 次/秒。所需权限：hire:talent_blocklist（更新黑名单信息）。</para>
    /// <para><see href="https://open.feishu.cn/document/hire-v1/candidate-management/talent/change_talent_block">接口文档</see></para>
    /// </summary>
    /// <param name="request">请求体（talent_id 必填：人才 ID；option 必填：1 加入 / 2 移出；reason 加入黑名单时必填）</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns>成功时 data 为空对象</returns>
    [Post("/open-apis/hire/v1/talent_blocklist/change_talent_block")]
    Task<FeishuNullDataApiResult?> ChangeTalentBlockAsync(
        [Body] ChangeTalentBlockRequest request,
        CancellationToken cancellationToken = default);
}
