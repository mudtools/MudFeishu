// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.DataModels.TaskSections;

namespace Mud.Feishu;

/// <summary>
/// 飞书自定义分组可以方便的在我负责的和清单中对任务进行自定义归类。通过自定义分组。
/// <para>可以：<list type="bullet">按状态分组，待启动-进行中-已完成
/// <item>按优先级分组，P0-重要且紧急，P1-重要但不紧急，...</item>
/// <item>按类别分组，市场相关、人事相关，...</item></list></para>
/// </summary>
[HttpClientApi(TokenManage = nameof(ITokenManager), IsAbstract = true)]
[Header(Consts.Authorization)]
public interface IFeishuV2TaskSections
{
    /// <summary>
    /// <para>为清单或我负责的任务列表创建一个自定义分组。</para>
    /// <para>创建时可以需要提供名称和可选的配置。如果不指定位置，新分组会放到指定resource的自定义分组列表的最后。</para>
    /// </summary>
    /// <param name="createTaskSectionsRequest">创建自定义分组请求体。</param>
    /// <param name="user_id_type">用户 ID，ID 类型需要与查询参数中的 user_id_type 类型保持一致。</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns></returns>
    [Post("/open-apis/task/v2/sections")]
    Task<FeishuApiResult<TaskSectionsOperationResult>?> CreateTaskSectionsAsync(
          [Body] CreateTaskSectionsRequest createTaskSectionsRequest,
          [Query("user_id_type")] string user_id_type = Consts.User_Id_Type,
          CancellationToken cancellationToken = default);

    /// <summary>
    /// <para>更新自定义分组，可以更新自定义分组的名称和位置。</para>
    /// <para>更新时，将update_fields字段中填写所有要修改的字段名，同时在section字段中填写要修改的字段的新值即可。</para>
    /// </summary>
    /// <param name="section_guid">要更新的自定义分组GUID。示例值："9842501a-9f47-4ff5-a622-d319eeecb97f"</param>
    /// <param name="updateTaskSectionsRequest">更新自定义分组请求体。</param>
    /// <param name="user_id_type">用户 ID，ID 类型需要与查询参数中的 user_id_type 类型保持一致。</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns></returns>
    [Patch("/open-apis/task/v2/sections/{section_guid}")]
    Task<FeishuApiResult<UpdateTaskSectionsResult>?> UpdateSectionsAsync(
       [Path] string section_guid,
       [Body] UpdateTaskSectionsRequest updateTaskSectionsRequest,
       [Query("user_id_type")] string user_id_type = Consts.User_Id_Type,
       CancellationToken cancellationToken = default);

    /// <summary>
    /// <para>获取一个自定义分组详情，包括名称，创建人等信息。</para>
    /// <para>如果该自定义分组归属于一个清单，还会返回清单的摘要信息。</para>
    /// </summary>
    /// <param name="section_guid">要获取的自定义分组GUID。示例值："9842501a-9f47-4ff5-a622-d319eeecb97f"</param>
    /// <param name="user_id_type">用户 ID，ID 类型需要与查询参数中的 user_id_type 类型保持一致。</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns></returns>
    [Get("/open-apis/task/v2/sections/{section_guid}")]
    Task<FeishuApiResult<TaskSectionsOperationResult>?> GetTaskSectionsByIdAsync(
           [Path] string section_guid,
           [Query("user_id_type")] string user_id_type = Consts.User_Id_Type,
           CancellationToken cancellationToken = default);

    /// <summary>
    /// <para>删除一个自定义分组。</para>
    /// <para>删除后该自定义分组中的任务会被移动到被删除自定义分组所属资源的默认自定义分组中。不能删除默认的自定义分组。</para>
    /// </summary>
    /// <param name="section_guid">要删除的自定义分组GUID。示例值："9842501a-9f47-4ff5-a622-d319eeecb97f"</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns></returns>
    [Delete("/open-apis/task/v2/sections/{section_guid}")]
    Task<FeishuNullDataApiResult?> DeleteTaskSectionsByIdAsync(
        [Path] string section_guid,
        CancellationToken cancellationToken = default);


    /// <summary>
    /// <para>分页获取自定义分组列表。</para>
    /// </summary>
    /// <param name="resource_id">如resource_type为"tasklist"，这里需要填写要列取自定义分组的清单的GUID。示例值："caef228f-2342-23c1-c36d-91186414dc64"</param>
    /// <param name="resource_type">自定义分组所属的资源类型。支持my_tasks(我负责的）和tasklist（清单）。当使用tasklist时，需要用resource_id提供清单的全局唯一ID。示例值："tasklist"</param>
    /// <param name="page_size">分页大小，即本次请求所返回的用户信息列表内的最大条目数。默认值：10</param>
    /// <param name="page_token">分页标记，第一次请求不填，表示从头开始遍历；分页查询结果还有更多项时会同时返回新的 page_token，下次遍历可采用该 page_token 获取查询结果</param>
    /// <param name="user_id_type">用户 ID，ID 类型需要与查询参数中的 user_id_type 类型保持一致。</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns></returns>
    [Get("/open-apis/task/v2/sections")]
    Task<FeishuApiPageListResult<SectionSummaryInfo>?> GetTaskSectionsPageListByIdAsync(
         [Query("page_size")] int page_size = 10,
         [Query("page_token")] string? page_token = null,
         [Query("resource_id")] string? resource_id = null,
         [Query("resource_type")] string? resource_type = null,
         [Query("user_id_type")] string user_id_type = Consts.User_Id_Type,
         CancellationToken cancellationToken = default);
}
