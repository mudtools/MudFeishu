// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.DataModels.Drive;

namespace Mud.Feishu.Interfaces;


/// <summary>
/// 飞书在线文档中的评论。
/// <para>接口详细文档请参见：<see href="https://open.feishu.cn/document/server-docs/docs/CommentAPI/list"/></para>
/// </summary>
[HttpClientApi(TokenManage = nameof(IFeishuAppManager), IsAbstract = true)]
[Token("TenantAccessToken", Name = Consts.Authorization)]
public interface IFeishuV1Comments : IFeishuAppContextSwitcher
{

    /// <summary>
    /// 获取云文档所有评论
    /// <para>用于根据云文档 Token 分页获取文档所有评论信息，包括评论和回复 ID、回复的内容、评论人和回复人的用户 ID 等。该接口支持返回全局评论以及局部评论，可通过 is_whole（是否为全局评论）字段区分。默认每页返回 50 个评论。</para>
    /// <para><see href="https://open.feishu.cn/document/server-docs/docs/CommentAPI/list">接口文档</see></para>
    /// </summary>
    /// <param name="file_token">文件的 token
    /// <para>示例值：XIHSdYSI7oMEU1xrsnxc8fabcef</para>
    /// </param>
    /// <param name="file_type">
    /// <para>必填：是</para>
    /// <para>云文档类型</para>
    /// <para>示例值：doc</para>
    /// <list type="bullet">
    /// <item>doc：旧版文档类型，已不推荐使用</item>
    /// <item>docx：新版文档类型</item>
    /// <item>sheet：电子表格类型</item>
    /// <item>file：文件类型</item>
    /// <item>slides：幻灯片</item>
    /// </list>
    /// </param>
    /// <param name="is_whole">
    /// <para>是否全文评论</para>
    /// <para>示例值：false</para>
    /// <para>默认值：false</para>
    /// </param>
    /// <param name="is_solved">
    /// <para>是否已解决（可选）</para>
    /// <para>示例值：false</para>
    /// <para>默认值：false</para>
    /// </param>
    /// <param name="need_reaction">
    /// <para>是否需要获取评论卡片上挂载的Reaction数据，默认值为false</para>
    /// <para>示例值：false</para>
    /// <para>默认值：false</para>
    /// </param>
    /// <param name="page_size">分页大小，即本次请求所返回的用户信息列表内的最大条目数。默认值：10</param>
    /// <param name="page_token">分页标记，第一次请求不填，表示从头开始遍历；分页查询结果还有更多项时会同时返回新的 page_token，下次遍历可采用该 page_token 获取查询结果</param>
    /// <param name="user_id_type">用户 ID，ID 类型与查询结果中的 user_id_type 类型保持一致。</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    [Get("/open-apis/drive/v1/files/{file_token}/comments")]
    Task<FeishuApiPageListResult<FileComment>?> GetCommentsPageListAsync(
           [Path] string file_token,
           [Query] string file_type,
           [Query] bool? is_whole = false,
           [Query] bool? is_solved = false,
           [Query] bool? need_reaction = false,
           [Query] int page_size = Consts.PageSize_50,
           [Query] string? page_token = null,
           [Query] string? user_id_type = Consts.User_Id_Type,
           CancellationToken cancellationToken = default);


    /// <summary>
    /// 批量获取评论
    /// <para>用于根据评论 ID 列表批量获取云文档评论信息，包括评论和回复 ID、回复的内容、评论人和回复人的用户 ID 等。支持返回全局评论以及局部评论，可通过 is_whole （是否为全局评论标识）字段区分。</para>
    /// <para><see href="https://open.feishu.cn/document/server-docs/docs/CommentAPI/batch_query">接口文档</see></para>
    /// </summary>
    /// <param name="file_token">文件的 token
    /// <para>示例值：XIHSdYSI7oMEU1xrsnxc8fabcef</para>
    /// </param>
    /// <param name="file_type">
    /// <para>必填：是</para>
    /// <para>云文档类型</para>
    /// <para>示例值：doc</para>
    /// <list type="bullet">
    /// <item>doc：旧版文档类型，已不推荐使用</item>
    /// <item>docx：新版文档类型</item>
    /// <item>sheet：电子表格类型</item>
    /// <item>file：文件类型</item>
    /// <item>slides：幻灯片</item>
    /// </list>
    /// </param>
    /// <param name="queryFileCommentRequest">批量查询评论请求对象</param>
    /// <param name="user_id_type">用户 ID，ID 类型与查询结果中的 user_id_type 类型保持一致。</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    [Post("/open-apis/drive/v1/files/{file_token}/comments/batch_query")]
    Task<FeishuApiResult<BatchQueryFileCommentResponse>?> BatchQueryFileCommentAsync(
          [Path] string file_token,
          [Query] string file_type,
          [Body] BatchQueryFileCommentRequest queryFileCommentRequest,
          [Query] string? user_id_type = Consts.User_Id_Type,
          CancellationToken cancellationToken = default);

    /// <summary>
    /// 批量获取评论
    /// <para>用于根据评论 ID 列表批量获取云文档评论信息，包括评论和回复 ID、回复的内容、评论人和回复人的用户 ID 等。支持返回全局评论以及局部评论，可通过 is_whole （是否为全局评论标识）字段区分。</para>
    /// <para><see href="https://open.feishu.cn/document/server-docs/docs/CommentAPI/batch_query">接口文档</see></para>
    /// </summary>
    /// <param name="file_token">文件的 token
    /// <para>示例值：XIHSdYSI7oMEU1xrsnxc8fabcef</para>
    /// </param>
    /// <param name="comment_id">
    /// <para>评论ID</para>
    /// <para>示例值：6916106822734578184</para>
    /// </param>
    /// <param name="file_type">
    /// <para>必填：是</para>
    /// <para>云文档类型</para>
    /// <para>示例值：doc</para>
    /// <list type="bullet">
    /// <item>doc：旧版文档类型，已不推荐使用</item>
    /// <item>docx：新版文档类型</item>
    /// <item>sheet：电子表格类型</item>
    /// <item>file：文件类型</item>
    /// <item>slides：幻灯片</item>
    /// </list>
    /// </param>
    /// <param name="patchFileCommentRequest">解决/恢复评论请求体</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    [Patch("/open-apis/drive/v1/files/{file_token}/comments/{comment_id}")]
    Task<FeishuNullDataApiResult?> PatchFileCommentAsync(
         [Path] string file_token,
         [Path] string comment_id,
         [Query] string file_type,
         [Body] PatchFileCommentRequest patchFileCommentRequest,
         CancellationToken cancellationToken = default);
}