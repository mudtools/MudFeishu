using Mud.Feishu.DataModels.JobFamilies;

namespace Mud.Feishu;

/// <summary>
/// 序列是用户属性之一，用来为不同的用户定义不同的工作类型，例如产品、研发、测试、运营。
/// <para>可以根据企业实际需要添加序列，后续在创建或更新用户时，为用户设置相匹配的序列。</para>
/// <para>通过序列 API，可以创建、更新、查询、删除序列信息。</para>
/// </summary>
[HttpClientApi]
[HttpClientApiWrap(TokenManage = nameof(ITokenManager), WrapInterface = nameof(IFeishuJobFamilies))]

public interface IFeishuJobFamiliesApi
{
    /// <summary>
    /// 创建一个序列。序列是用户属性之一，用来定义用户的工作类型，例如产品、研发、运营等。
    /// </summary>
    /// <param name="tenant_access_token">以应用身份调用 API，可读写的数据范围由应用自身的 数据权限范围 决定。</param>
    /// <param name="familyCreateRequest">职位序列创建请求体。</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns></returns>
    [Post("https://open.feishu.cn/open-apis/contact/v3/job_families")]
    Task<FeishuApiResult<JobFamilyResult>> CreateJobFamilyAsync(
          [Token][Header("Authorization")] string tenant_access_token,
          [Body] JobFamilyCreateUpdateRequest familyCreateRequest,
          CancellationToken cancellationToken = default);

    /// <summary>
    /// 更新指定序列的信息。
    /// </summary>
    /// <param name="tenant_access_token">以应用身份调用 API，可读写的数据范围由应用自身的 数据权限范围 决定。</param>
    /// <param name="familyCreateRequest">职位序列创建请求体。</param>
    /// <param name="job_family_id">序列 ID。</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns></returns>
    [Put("https://open.feishu.cn/open-apis/contact/v3/job_families/{job_family_id}")]
    Task<FeishuApiResult<JobFamilyResult>> UpdateJobFamilyAsync(
         [Token][Header("Authorization")] string tenant_access_token,
         [Path] string job_family_id,
         [Body] JobFamilyCreateUpdateRequest familyCreateRequest,
         CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取指定序列的信息，包括序列的名称、描述、启用状态以及 ID 等。
    /// </summary>
    /// <param name="tenant_access_token">以应用身份调用 API，可读写的数据范围由应用自身的 数据权限范围 决定。</param>
    /// <param name="job_family_id">序列 ID。</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns></returns>
    [Get("https://open.feishu.cn/open-apis/contact/v3/job_families/{job_family_id}")]
    Task<FeishuApiResult<JobFamilyResult>> GetJobFamilyByIdAsync(
        [Token][Header("Authorization")] string tenant_access_token,
        [Path] string job_family_id,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取当前租户下的序列信息，包含序列的名称、描述、启用状态以及 ID 等。
    /// </summary>
    /// <param name="tenant_access_token">以应用身份调用 API，可读写的数据范围由应用自身的 数据权限范围 决定。</param>
    /// <param name="name">序列名称。示例值："产品"</param>
    /// <param name="page_size">分页大小，即本次请求所返回的用户信息列表内的最大条目数。默认值：10</param>
    /// <param name="page_token">分页标记，第一次请求不填，表示从头开始遍历；分页查询结果还有更多项时会同时返回新的 page_token，下次遍历可采用该 page_token 获取查询结果</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns></returns>
    [Get("https://open.feishu.cn/open-apis/contact/v3/job_levels")]
    Task<FeishuApiListResult<JobFamilyInfo>> GetJobFamilesListAsync(
         [Token][Header("Authorization")] string tenant_access_token,
         [Query("name")] string name,
         [Query("page_size")] int page_size = 10,
         [Query("page_token")] string? page_token = null,
         CancellationToken cancellationToken = default);

    /// <summary>
    /// 删除指定序列。
    /// <para>
    /// 仅支持删除没有子序列的序列。如果序列内存在子序列，则不能直接删除。
    /// </para>
    /// </summary>
    /// <param name="tenant_access_token">以应用身份调用 API，可读写的数据范围由应用自身的 数据权限范围 决定。</param>
    /// <param name="job_family_id">序列 ID。</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns></returns>
    [Delete("https://open.feishu.cn/open-apis/contact/v3/job_families/{job_family_id}")]
    Task<FeishuNullDataApiResult> DeleteJobFamilyByIdAsync(
            [Token][Header("Authorization")] string tenant_access_token,
            [Path] string job_family_id,
            CancellationToken cancellationToken = default);
}
