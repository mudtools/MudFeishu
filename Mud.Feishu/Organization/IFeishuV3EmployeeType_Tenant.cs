// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.DataModels.EmployeeType;

namespace Mud.Feishu;

/// <summary>
/// 飞书人员类型是通讯录中一种特殊的用户属性字段，用于标记用户的身份类型。
/// <para>当前接口使用租户令牌访问，适应于租户应用场景。</para>
/// <para>使用通讯录 API，可以对人员类型资源进行增删改查操作。</para>
/// <para>接口详细文档请参见：<see href="https://open.feishu.cn/document/server-docs/contact-v3/employee_type_enum/overview"/></para>
/// </summary>
[HttpClientApi(TokenManage = nameof(ITenantTokenManager), RegistryGroupName = "Organization")]
[Header("Authorization")]
public interface IFeishuTenantV3EmployeeType
{
    /// <summary>
    /// 新增一个自定义的人员类型。人员类型是用户属性之一，用于灵活标记用户的身份类型。
    /// </summary>
    /// <param name="groupInfoRequest">新增人员类型请求体</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns></returns>
    [Post("https://open.feishu.cn/open-apis/contact/v3/employee_type_enums")]
    Task<FeishuApiResult<EmployeeTypeEnumResult>?> CreateEmployeeTypeAsync(
        [Body] EmployeeTypeEnumRequest groupInfoRequest,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 更新指定的自定义人员类型信息。
    /// </summary>
    /// <param name="enum_id">自定义人员类型的选项 ID。可以在新建人员类型时从返回值中获取，也可以调用查询人员类型接口，获取选项的 ID。</param>
    /// <param name="groupInfoRequest">新增人员类型请求体</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns></returns>
    [Put("https://open.feishu.cn/open-apis/contact/v3/employee_type_enums/{enum_id}")]
    Task<FeishuApiResult<EmployeeTypeEnumResult>?> UpdateEmployeeTypeAsync(
        [Path] string enum_id,
        [Body] EmployeeTypeEnumRequest groupInfoRequest,
        CancellationToken cancellationToken = default);


    /// <summary>
    /// 查询当前租户下所有的人员类型信息，包括选项 ID、类型、编号以及内容等。
    /// </summary>
    /// <param name="page_size">分页大小，即本次请求所返回的用户信息列表内的最大条目数。默认值：10</param>
    /// <param name="page_token">分页标记，第一次请求不填，表示从头开始遍历；分页查询结果还有更多项时会同时返回新的 page_token，下次遍历可采用该 page_token 获取查询结果</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns></returns>
    [Get("https://open.feishu.cn/open-apis/contact/v3/employee_type_enums")]
    Task<FeishuApiPageListResult<EmployeeTypeEnum>?> GetEmployeeTypesAsync(
       [Query("page_size")] int page_size = 10,
       [Query("page_token")] string? page_token = null,
       CancellationToken cancellationToken = default);

    /// <summary>
    /// 删除指定的自定义人员类型。
    /// <para>仅支持删除自定义的人员类型。默认包含的正式、实习、外包、劳务、顾问五个选项不支持删除。</para>
    /// </summary>
    /// <param name="enum_id">自定义人员类型的选项 ID。</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns></returns>
    [Delete("https://open.feishu.cn/open-apis/contact/v3/employee_type_enums/{enum_id}")]
    Task<FeishuNullDataApiResult?> DeleteEmployeeTypeByIdAsync(
         [Path] string enum_id,
         CancellationToken cancellationToken = default);
}
