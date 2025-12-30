// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.DataModels.TaskCustomFields;

namespace Mud.Feishu.Interfaces;

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


    /// <summary>
    /// <para>创建一个自定义字段，并将其加入一个资源上（目前资源只支持清单）。</para>
    /// <para>创建自定义字段必须提供字段名称，类型和相应类型的设置。</para>
    /// </summary>
    /// <param name="custom_field_guid">自定义字段GUID。示例值：5ffbe0ca-6600-41e0-a634-2b38cbcf13b8</param>
    /// <param name="updateTaskSectionsRequest">更新自定义字段请求体。</param>
    /// <param name="user_id_type">用户 ID，ID 类型需要与查询参数中的 user_id_type 类型保持一致。</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns></returns>
    [Patch("/open-apis/task/v2/custom_fields/{custom_field_guid}")]
    Task<FeishuApiResult<CustomFieldsResult>?> UpdateCustomFieldsAsync(
     [Path] string custom_field_guid,
     [Body] UpdateCustomFieldsRequest updateTaskSectionsRequest,
     [Query("user_id_type")] string user_id_type = Consts.User_Id_Type,
     CancellationToken cancellationToken = default);

    /// <summary>
    /// <para>更新一个自定义字段的名称和设定。</para>
    /// <para>更新时，将update_fields字段中填写所有要修改的任务字段名，同时在custom_field字段中填写要修改的字段的新值即可。</para>
    /// <para>自定义字段不允许修改类型，只能根据类型修改其设置。</para>
    /// </summary>
    /// <param name="custom_field_guid">自定义字段GUID。示例值：5ffbe0ca-6600-41e0-a634-2b38cbcf13b8</param>
    /// <param name="user_id_type">用户 ID，ID 类型需要与查询参数中的 user_id_type 类型保持一致。</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns></returns>
    [Get("/open-apis/task/v2/custom_fields/{custom_field_guid}")]
    Task<FeishuApiResult<CustomFieldsResult>?> GetCustomFieldsByIdAsync(
          [Path] string custom_field_guid,
          [Query("user_id_type")] string user_id_type = Consts.User_Id_Type,
          CancellationToken cancellationToken = default);


    /// <summary>
    /// <para>分页列取用户可访问的自定义字段列表。如果不提供resource_type和resource_id参数，则返回用户可访问的所有自定义字段。</para>
    /// </summary>
    /// <param name="resource_id">要查询自定义字段的归属resource_id。示例值："caef228f-2342-23c1-c36d-91186414dc64"</param>
    /// <param name="resource_type">资源类型，如提供表示仅查询特定资源下的自定义字段。目前只支持tasklist。示例值："tasklist"</param>
    /// <param name="page_size">分页大小，即本次请求所返回的用户信息列表内的最大条目数。默认值：10</param>
    /// <param name="page_token">分页标记，第一次请求不填，表示从头开始遍历；分页查询结果还有更多项时会同时返回新的 page_token，下次遍历可采用该 page_token 获取查询结果</param>
    /// <param name="user_id_type">用户 ID，ID 类型需要与查询参数中的 user_id_type 类型保持一致。</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns></returns>
    [Get("/open-apis/task/v2/custom_fields")]
    Task<FeishuApiPageListResult<CustomFieldsResult>?> GetCustomFieldsPageListAsync(
        [Query("resource_type")] string? resource_type = null,
        [Query("resource_id")] string? resource_id = null,
        [Query("page_size")] int page_size = 10,
        [Query("page_token")] string? page_token = null,
        [Query("user_id_type")] string user_id_type = Consts.User_Id_Type,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// <para>将自定义字段加入一个资源。目前资源类型支持清单tasklist。一个自定义字段可以加入多个清单中。</para>
    /// <para>加入后，该清单可以展示任务的该字段的值，同时基于该字段实现筛选，分组等功能。</para>
    /// </summary>
    /// <param name="custom_field_guid">自定义字段GUID。示例值：5ffbe0ca-6600-41e0-a634-2b38cbcf13b8</param>
    /// <param name="customFieldsToResourceRequest">将自定义字段加入资源请求体。</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns></returns>
    [Post("/open-apis/task/v2/custom_fields/{custom_field_guid}/add")]
    Task<FeishuNullDataApiResult?> AddCustomFieldsByIdAsync(
               [Path] string custom_field_guid,
               [Body] CustomFieldsToResourceRequest customFieldsToResourceRequest,
               CancellationToken cancellationToken = default);


    /// <summary>
    /// <para>将自定义字段从资源中移出。移除后，该资源将无法再使用该字段。目前资源的类型支持"tasklist"。</para>
    /// <para>如果要移除自定义字段本来就不存在于资源，本接口将正常返回。</para>
    /// </summary>
    /// <param name="custom_field_guid">自定义字段GUID。示例值：5ffbe0ca-6600-41e0-a634-2b38cbcf13b8</param>
    /// <param name="customFieldsToResourceRequest">将自定义字段移出资源请求体。</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns></returns>
    [Post("/open-apis/task/v2/custom_fields/{custom_field_guid}/remove")]
    Task<FeishuNullDataApiResult?> RemoveCustomFieldsByIdAsync(
              [Path] string custom_field_guid,
              [Body] CustomFieldsToResourceRequest customFieldsToResourceRequest,
              CancellationToken cancellationToken = default);

    /// <summary>
    /// <para>为单选或多选字段添加一个自定义选项。一个单选/多选字段最大支持100个选项。</para>
    /// <para>新添加的选项如果不隐藏，其名字不能和已存在的不隐藏选项的名字重复。</para>
    /// </summary>
    /// <param name="custom_field_guid">自定义字段GUID。示例值：5ffbe0ca-6600-41e0-a634-2b38cbcf13b8</param>
    /// <param name="createCustomFieldsOptionsRequest">创建自定义任务选项请求体。</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns></returns>
    [Post("/open-apis/task/v2/custom_fields/{custom_field_guid}/options")]
    Task<FeishuApiResult<CustomFieldsOptionsResult>?> CreateCustomFieldsOptionsAsync(
             [Path] string custom_field_guid,
             [Body] CreateCustomFieldsOptionsRequest createCustomFieldsOptionsRequest,
             CancellationToken cancellationToken = default);

    /// <summary>
    /// <para>根据一个自定义字段的GUID和其选项的GUID，更新该选项的数据。</para>
    /// <para>要更新的字段必须是单选或者多选类型，且要更新的字段必须归属于该字段。</para>
    /// </summary>
    /// <param name="custom_field_guid">要更新的选项的自定义字段GUID。示例值：5ffbe0ca-6600-41e0-a634-2b38cbcf13b8</param>
    /// <param name="option_guid">要更新的选项的GUID。示例值：d4f1e8b3-5f4e-4c3a-8f7b-2e2f4e5c6d7e</param>
    /// <param name="updateCustomFieldsOptionsRequest">更新自定义任务选项请求体。</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns></returns>
    [Post("/open-apis/task/v2/custom_fields/{custom_field_guid}/options/{option_guid}")]
    Task<FeishuApiResult<CustomFieldsOptionsResult>?> UpdateCustomFieldsOptionsAsync(
            [Path] string custom_field_guid,
            [Path] string option_guid,
            [Body] UpdateCustomFieldsOptionsRequest updateCustomFieldsOptionsRequest,
            CancellationToken cancellationToken = default);
}
