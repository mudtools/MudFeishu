// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.DataModels.Units;

namespace Mud.Feishu;

/// <summary>
/// 通讯录的单位用于代表企业中的“子公司”、“分支机构”这类组织实体。
/// <para>例如，你的企业下存在负责不同业务的两家子公司，那么你可以在同一个租户内，为两家子公司分别创建对应的单位资源。</para>
/// <para>目前单位资源的主要作用是在部分用户权限上实现“子公司”级别的权限隔离。</para>
/// <para>接口详细文档请参见：<see href="https://open.feishu.cn/document/server-docs/contact-v3/unit/overview"/></para>
/// </summary>
[HttpClientApi(RegistryGroupName = "Organization")]
[HttpClientApiWrap(TokenManage = nameof(ITokenManager), WrapInterface = nameof(IFeishuV3Unit))]
public interface IFeishuV3UnitApi
{
    /// <summary>
    /// 创建一个单位。
    /// </summary>
    /// <param name="tenant_access_token">以应用身份调用 API，可读写的数据范围由应用自身的 数据权限范围 决定。</param>
    /// <param name="groupInfoRequest">单位信息请求体</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns></returns>
    [Post("https://open.feishu.cn/open-apis/contact/v3/unit")]
    Task<FeishuApiResult<UnitCreateResult>> CreateUnitAsync(
      [Token][Header("Authorization")] string tenant_access_token,
      [Body] UnitInfoRequest groupInfoRequest,
      CancellationToken cancellationToken = default);

    /// <summary>
    /// 修改指定单位的名字。
    /// </summary>
    /// <param name="tenant_access_token">以应用身份调用 API，可读写的数据范围由应用自身的 数据权限范围 决定。</param>
    /// <param name="unit_id">单位 ID。</param>
    /// <param name="nameUpdateRequest">单位名称更新请求体</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns></returns>
    [Patch("https://open.feishu.cn/open-apis/contact/v3/unit/{unit_id}")]
    Task<FeishuNullDataApiResult> UpdateUnitAsync(
         [Token][Header("Authorization")] string tenant_access_token,
         [Path] string unit_id,
         [Body] UnitNameUpdateRequest nameUpdateRequest,
         CancellationToken cancellationToken = default);

    /// <summary>
    /// 建立部门与单位的绑定关系。一个部门同时只能绑定一个单位。
    /// </summary>
    /// <param name="tenant_access_token">以应用身份调用 API，可读写的数据范围由应用自身的 数据权限范围 决定。</param>
    /// <param name="unitBindDepartment">部门与单位的绑定关系请求体</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <remarks>
    /// <para>单个单位可关联的部门数量上限为 1,000。</para>
    /// <para>同一个部门只能关联一个单位。</para>
    /// </remarks>
    /// <returns></returns>
    [Post("https://open.feishu.cn/open-apis/contact/v3/unit/bind_department")]
    Task<FeishuApiResult<UnitCreateResult>> BindDepartmentAsync(
          [Token][Header("Authorization")] string tenant_access_token,
          [Body] UnitBindDepartmentRequest unitBindDepartment,
          CancellationToken cancellationToken = default);

    /// <summary>
    /// 解除部门与单位的绑定关系。
    /// </summary>
    /// <param name="tenant_access_token">以应用身份调用 API，可读写的数据范围由应用自身的 数据权限范围 决定。</param>
    /// <param name="unitBindDepartment">部门与单位的绑定关系请求体</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns></returns>
    [Post("https://open.feishu.cn/open-apis/contact/v3/unit/unbind_department")]
    Task<FeishuApiResult<UnitCreateResult>> UnBindDepartmentAsync(
          [Token][Header("Authorization")] string tenant_access_token,
          [Body] UnitBindDepartmentRequest unitBindDepartment,
          CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取单位绑定的部门列表
    /// </summary>
    /// <param name="tenant_access_token">以应用身份调用 API，可读写的数据范围由应用自身的 数据权限范围 决定。</param>
    /// <param name="unit_id">单位 ID。</param>
    /// <param name="department_id_type">此次调用中使用的部门 ID 类型。</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <param name="page_size">分页大小，即本次请求所返回的用户信息列表内的最大条目数。默认值：10</param>
    /// <param name="page_token">分页标记，第一次请求不填，表示从头开始遍历；分页查询结果还有更多项时会同时返回新的 page_token，下次遍历可采用该 page_token 获取查询结果</param>
    /// <returns></returns>
    [Get("https://open.feishu.cn/open-apis/contact/v3/unit/list_department")]
    Task<FeishuApiResult<UnitDepartmentListResult>> GetDepartmentListAsync(
         [Token][Header("Authorization")] string tenant_access_token,
         [Query("unit_id")] string unit_id,
         [Query("page_size")] int page_size = 10,
         [Query("page_token")] string? page_token = null,
         [Query("department_id_type")] string? department_id_type = Consts.Department_Id_Type,
         CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取指定单位的信息，包括单位 ID、名字、类型。
    /// </summary>
    /// <param name="tenant_access_token">以应用身份调用 API，可读写的数据范围由应用自身的 数据权限范围 决定。</param>
    /// <param name="unit_id">单位 ID。</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns></returns>
    [Get("https://open.feishu.cn/open-apis/contact/v3/unit/{unit_id}")]
    Task<FeishuApiResult<UnitInfo>> GetUnitInfoAsync(
        [Token][Header("Authorization")] string tenant_access_token,
        [Path] string unit_id,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取当前租户内的单位列表。列表内主要包含各单位的 ID、名字、类型信息。
    /// </summary>
    /// <param name="tenant_access_token">以应用身份调用 API，可读写的数据范围由应用自身的 数据权限范围 决定。</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <param name="page_size">分页大小，即本次请求所返回的用户信息列表内的最大条目数。默认值：10</param>
    /// <param name="page_token">分页标记，第一次请求不填，表示从头开始遍历；分页查询结果还有更多项时会同时返回新的 page_token，下次遍历可采用该 page_token 获取查询结果</param>
    /// <returns></returns>
    [Get("https://open.feishu.cn/open-apis/contact/v3/unit")]
    Task<FeishuApiResult<UnitListDataResult>> GetUnitListAsync(
       [Token][Header("Authorization")] string tenant_access_token,
       [Query("page_size")] int page_size = 10,
       [Query("page_token")] string? page_token = null,
       CancellationToken cancellationToken = default);

    /// <summary>
    /// 删除指定单位。
    /// </summary>
    /// <param name="tenant_access_token">应用调用 API 时，通过访问凭证（access_token）进行身份鉴权</param>
    /// <param name="unit_id">需删除的单位 ID。</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns></returns>
    [Delete("https://open.feishu.cn/open-apis/contact/v3/unit/{unit_id}")]
    Task<FeishuNullDataApiResult> DeleteUnitByIdAsync(
      [Token][Header("Authorization")] string tenant_access_token,
      [Path] string unit_id,
      CancellationToken cancellationToken = default);
}
