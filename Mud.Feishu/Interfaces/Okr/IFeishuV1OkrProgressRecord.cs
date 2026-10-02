// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.DataModels.Okr;

namespace Mud.Feishu.Interfaces;


/// <summary>
/// 飞书 OKR「进展记录」SDK 是一组服务端 OpenAPI 的封装，用于创建、更新、查询、删除 OKR 进展记录，以及上传进展记录中的图片附件（富文本 content_block 结构）。本接口全部端点为 okr/v1，同时支持 tenant_access_token 与 user_access_token 调用（租户态见 <see cref="IFeishuTenantV1OkrProgressRecord"/>，用户态见 <see cref="IFeishuUserV1OkrProgressRecord"/>）。
/// <para><see href="https://open.feishu.cn/document/server-docs/okr-v1/progress_record/create">接口文档</see></para>
/// </summary>
[HttpClientApi(TokenManage = nameof(IFeishuAppManager), IsAbstract = true)]
[Token(FeishuTokenTypes.TenantAccessToken, Name = Consts.Authorization)]
public interface IFeishuV1OkrProgressRecord : IFeishuAppContextSwitcher
{
    /// <summary>
    /// 创建 OKR 进展记录
    /// <para>为指定的目标（Objective）或关键结果（KR）创建一条进展记录，内容为富文本格式（content_block）。</para>
    /// <para>限频：100 次/分钟。所需权限（任一即可）：okr:okr（更新 OKR）、okr:okr.progress:writeonly（更新 OKR 进展）；user_id_type 取 user_id 时需字段权限 contact:user.employee_id:readonly。支持的应用类型：自建应用。</para>
    /// <para><see href="https://open.feishu.cn/document/server-docs/okr-v1/progress_record/create">接口文档</see></para>
    /// </summary>
    /// <param name="request">创建进展记录请求体（source_title 来源、source_url 来源链接、target_id 目标 id、target_type 目标类型 2=Objective/3=KR、content 富文本内容均必填，另支持 source_url_pc/source_url_mobile）</param>
    /// <param name="user_id_type">用户 ID 类型（open_id/union_id/user_id），默认 open_id</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns>返回进展记录信息（progress_id、modify_time、content）</returns>
    [Post("/open-apis/okr/v1/progress_records")]
    Task<FeishuApiResult<ProgressRecordResult>?> CreateProgressRecordAsync(
        [Body] CreateProgressRecordRequest request,
        [Query("user_id_type")] string user_id_type = Consts.User_Id_Type,
        CancellationToken cancellationToken = default);


    /// <summary>
    /// 获取 OKR 进展记录详情
    /// <para>根据进展记录 id 获取进展记录详情（富文本 content 内容）。</para>
    /// <para>限频：100 次/分钟。所需权限（任一即可）：okr:okr:readonly（获取 OKR 信息）、okr:okr.progress:readonly（获取 OKR 进展）、okr:okr（更新 OKR）；user_id_type 取 user_id 时需字段权限 contact:user.employee_id:readonly。支持的应用类型：自建应用、商店应用。</para>
    /// <para><see href="https://open.feishu.cn/document/server-docs/okr-v1/progress_record/get">接口文档</see></para>
    /// </summary>
    /// <param name="progress_id">待查询的进展记录 id，示例值：7041857032248410131</param>
    /// <param name="user_id_type">用户 ID 类型（open_id/union_id/user_id），默认 open_id</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns>返回进展记录信息（progress_id、modify_time、content）</returns>
    [Get("/open-apis/okr/v1/progress_records/{progress_id}")]
    Task<FeishuApiResult<ProgressRecordResult>?> GetProgressRecordAsync(
        [Path] string progress_id,
        [Query("user_id_type")] string user_id_type = Consts.User_Id_Type,
        CancellationToken cancellationToken = default);


    /// <summary>
    /// 更新 OKR 进展记录
    /// <para>更新指定进展记录的富文本内容（content_block）。</para>
    /// <para>限频：100 次/分钟。所需权限（任一即可）：okr:okr（更新 OKR）、okr:okr.progress:writeonly（更新 OKR 进展）；user_id_type 取 user_id 时需字段权限 contact:user.employee_id:readonly。支持的应用类型：自建应用。</para>
    /// <para><see href="https://open.feishu.cn/document/server-docs/okr-v1/progress_record/update">接口文档</see></para>
    /// </summary>
    /// <param name="progress_id">待更新的进展记录 id，示例值：7041857032248410131</param>
    /// <param name="request">更新进展记录请求体（content 富文本内容必填）</param>
    /// <param name="user_id_type">用户 ID 类型（open_id/union_id/user_id），默认 open_id</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns>返回进展记录信息（progress_id、modify_time、content）</returns>
    [Put("/open-apis/okr/v1/progress_records/{progress_id}")]
    Task<FeishuApiResult<ProgressRecordResult>?> UpdateProgressRecordAsync(
        [Path] string progress_id,
        [Body] UpdateProgressRecordRequest request,
        [Query("user_id_type")] string user_id_type = Consts.User_Id_Type,
        CancellationToken cancellationToken = default);


    /// <summary>
    /// 删除 OKR 进展记录
    /// <para>根据进展记录 id 删除进展记录。</para>
    /// <para>限频：100 次/分钟。所需权限（任一即可）：okr:okr（更新 OKR）、okr:okr.progress:delete（删除 OKR 进展）。支持的应用类型：自建应用。</para>
    /// <para><see href="https://open.feishu.cn/document/server-docs/okr-v1/progress_record/delete">接口文档</see></para>
    /// </summary>
    /// <param name="progress_id">待删除的进展记录 id，示例值：7041857032248410131</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns>成功时 data 为空对象</returns>
    [Delete("/open-apis/okr/v1/progress_records/{progress_id}")]
    Task<FeishuNullDataApiResult?> DeleteProgressRecordAsync(
        [Path] string progress_id,
        CancellationToken cancellationToken = default);


    /// <summary>
    /// 上传进展记录图片
    /// <para>上传进展记录中的图片（multipart/form-data）。上传成功后可继续调用创建/更新进展记录接口，把返回的 url 与 file_token 传入 gallery 的 imageList 参数。</para>
    /// <para>限频：100 次/分钟。所需权限（任一即可）：okr:okr（更新 OKR）、okr:okr.progress.file:upload（上传进展记录图片附件）。支持的应用类型：自建应用。</para>
    /// <para><see href="https://open.feishu.cn/document/server-docs/okr-v1/progress_record/upload">接口文档</see></para>
    /// </summary>
    /// <param name="request">上传图片请求体（data 图片文件本地路径、target_id 目标 id、target_type 目标类型 2=Objective/3=KR 均必填）</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns>返回图片 token（file_token）与图片下载链接（url）</returns>
    [Post("/open-apis/okr/v1/images/upload")]
    Task<FeishuApiResult<UploadProgressRecordImageResult>?> UploadProgressRecordImageAsync(
        [FormContent] UploadProgressRecordImageRequest request,
        CancellationToken cancellationToken = default);
}
