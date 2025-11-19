// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.DataModels.JobLevel;

namespace Mud.Feishu;

/// <summary>
/// 职级是用户属性之一，可以根据企业组织架构的需要，添加职级，例如 P1、P2、P3、P4。
/// <para>后续在创建用户或者更新用户时，可以为用户设置指定的职级属性。</para>
/// <para>使用职级 API，可以创建、更新、删除或查询职级。</para>
/// </summary>
[HttpClientApi]
[HttpClientApiWrap(TokenManage = nameof(ITokenManager), WrapInterface = nameof(IFeishuV3JobLevel))]
public interface IFeishuV3JobLevelApi
{

    /// <summary>
    /// 创建一个职级。职级是用户属性之一，用于标识用户的职位级别，例如 P1、P2、P3、P4。
    /// </summary>
    /// <param name="tenant_access_token">以应用身份调用 API，可读写的数据范围由应用自身的 数据权限范围 决定。</param>
    /// <param name="levelCreateRequest">创建职级请求体。</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns></returns>
    [Post("https://open.feishu.cn/open-apis/contact/v3/job_levels")]
    Task<FeishuApiResult<JobLevelResult>> CreateJobLevelAsync(
       [Token][Header("Authorization")] string tenant_access_token,
       [Body] JobLevelCreateUpdateRequest levelCreateRequest,
       CancellationToken cancellationToken = default);

    /// <summary>
    /// 更新指定职级的信息。
    /// </summary>
    /// <param name="tenant_access_token">以应用身份调用 API，可读写的数据范围由应用自身的 数据权限范围 决定。</param>
    /// <param name="levelCreateRequest">更新职级请求体。</param>
    /// <param name="job_level_id">职级 ID。</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns></returns>
    [Put("https://open.feishu.cn/open-apis/contact/v3/job_levels/{job_level_id}")]
    Task<FeishuApiResult<JobLevelResult>> UpdateJobLevelAsync(
       [Token][Header("Authorization")] string tenant_access_token,
       [Path] string job_level_id,
       [Body] JobLevelCreateUpdateRequest levelCreateRequest,
       CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取指定职级的信息，包括职级名称、描述、排序、状态以及多语言等。
    /// </summary>
    /// <param name="tenant_access_token">以应用身份调用 API，可读写的数据范围由应用自身的 数据权限范围 决定。</param>
    /// <param name="job_level_id">职级 ID。</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns></returns>
    [Get("https://open.feishu.cn/open-apis/contact/v3/job_levels/{job_level_id}")]
    Task<FeishuApiResult<JobLevelResult>> GetJobLevelByIdAsync(
           [Token][Header("Authorization")] string tenant_access_token,
           [Path] string job_level_id,
           CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取当前租户下的职级信息，包括职级名称、描述、排序、状态以及多语言等。
    /// </summary>
    /// <param name="tenant_access_token">以应用身份调用 API，可读写的数据范围由应用自身的 数据权限范围 决定。</param>
    /// <param name="name">职级名称。示例值："高级专家"</param>
    /// <param name="page_size">分页大小，即本次请求所返回的用户信息列表内的最大条目数。默认值：10</param>
    /// <param name="page_token">分页标记，第一次请求不填，表示从头开始遍历；分页查询结果还有更多项时会同时返回新的 page_token，下次遍历可采用该 page_token 获取查询结果</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns></returns>
    [Get("https://open.feishu.cn/open-apis/contact/v3/job_levels")]
    Task<FeishuApiListResult<JobLevelResult>> GetJobLevelListAsync(
         [Token][Header("Authorization")] string tenant_access_token,
         [Query("name")] string name,
         [Query("page_size")] int page_size = 10,
         [Query("page_token")] string? page_token = null,
         CancellationToken cancellationToken = default);

    /// <summary>
    /// 删除指定的职级。
    /// </summary>
    /// <param name="tenant_access_token">以应用身份调用 API，可读写的数据范围由应用自身的 数据权限范围 决定。</param>
    /// <param name="job_level_id">职级 ID。</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns></returns>
    [Delete("https://open.feishu.cn/open-apis/contact/v3/job_levels/{job_level_id}")]
    Task<FeishuNullDataApiResult> DeleteJobLevelByIdAsync(
            [Token][Header("Authorization")] string tenant_access_token,
            [Path] string job_level_id,
            CancellationToken cancellationToken = default);
}
