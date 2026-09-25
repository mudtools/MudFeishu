// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.DataModels.Acs;

namespace Mud.Feishu;


/// <summary>
/// 飞书智能门禁（ACS）用户 SDK 是一组服务端 OpenAPI 的封装，用于查询门禁用户信息（单个/列表，仅限已加入智能门禁权限组的用户）、录入韦根卡号，以及上传/下载用户人脸图片。本接口全部端点仅支持 tenant_access_token 调用。
/// <para>接口详细文档请参见：<see href="https://open.feishu.cn/document/server-docs/acs-v1/user/get"/></para>
/// </summary>
[HttpClientApi(TokenManage = nameof(IFeishuAppManager), RegistryGroupName = "Acs")]
[Token(FeishuTokenTypes.TenantAccessToken, Name = Consts.Authorization)]
public interface IFeishuTenantV1AcsUser : IFeishuAppContextSwitcher
{
    /// <summary>
    /// 获取单个用户信息
    /// <para>获取智能门禁中单个用户的信息，只能获取已加入智能门禁权限组的用户。</para>
    /// <para>所需权限：acs:users（查看、更新智能门禁用户，任一即可）；user_id_type 取 user_id 时需字段权限 contact:user.employee_id:readonly。</para>
    /// <para><see href="https://open.feishu.cn/document/server-docs/acs-v1/user/get">接口文档</see></para>
    /// </summary>
    /// <param name="user_id">用户 ID，示例值：ou_7dab8a3d3cdcc9da365777c7ad535d62</param>
    /// <param name="user_id_type">用户 ID 类型（open_id/union_id/user_id），默认 open_id</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns>返回门禁用户信息（user：user_id 与 feature 卡号/人脸录入状态）</returns>
    [Get("/open-apis/acs/v1/users/{user_id}")]
    Task<FeishuApiResult<GetUserResult>?> GetUserAsync(
        [Path] string user_id,
        [Query("user_id_type")] string? user_id_type = null,
        CancellationToken cancellationToken = default);


    /// <summary>
    /// 获取用户列表
    /// <para>获取智能门禁中所有用户信息，只能获取已加入智能门禁权限组的用户。</para>
    /// <para>所需权限：acs:users（查看、更新智能门禁用户，任一即可）；user_id_type 取 user_id 时需字段权限 contact:user.employee_id:readonly。</para>
    /// <para><see href="https://open.feishu.cn/document/server-docs/acs-v1/user/list">接口文档</see></para>
    /// </summary>
    /// <param name="page_size">分页大小，最大 50</param>
    /// <param name="page_token">分页标记，第一次请求不填，表示从头开始遍历；分页查询结果还有更多项时会同时返回新的 page_token，下次遍历可采用该 page_token 获取查询结果</param>
    /// <param name="user_id_type">用户 ID 类型（open_id/union_id/user_id），默认 open_id</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns>返回门禁用户分页列表（items、page_token、has_more）</returns>
    [Get("/open-apis/acs/v1/users")]
    Task<FeishuApiResult<GetUserListResult>?> GetUserListAsync(
        [Query("page_size")] int? page_size = null,
        [Query("page_token")] string? page_token = null,
        [Query("user_id_type")] string? user_id_type = null,
        CancellationToken cancellationToken = default);


    /// <summary>
    /// 修改用户部分信息
    /// <para>飞书智能门禁在人脸识别成功后会有韦根信号输出，输出用户的卡号；对于使用韦根协议的门禁系统，可使用该接口录入用户卡号。</para>
    /// <para>所需权限：acs:users（查看、更新智能门禁用户，任一即可）；user_id_type 取 user_id 时需字段权限 contact:user.employee_id:readonly。</para>
    /// <para><see href="https://open.feishu.cn/document/server-docs/acs-v1/user/patch">接口文档</see></para>
    /// </summary>
    /// <param name="user_id">用户 ID，示例值：ou_7dab8a3d3cdcc9da365777c7ad535d62</param>
    /// <param name="request">修改请求体（feature.card：要录入的卡号，示例值 123456）</param>
    /// <param name="user_id_type">用户 ID 类型（open_id/union_id/user_id），默认 open_id</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns>成功时 data 为空对象</returns>
    [Patch("/open-apis/acs/v1/users/{user_id}")]
    Task<FeishuNullDataApiResult?> PatchUserAsync(
        [Path] string user_id,
        [Body] PatchUserRequest request,
        [Query("user_id_type")] string? user_id_type = null,
        CancellationToken cancellationToken = default);


    /// <summary>
    /// 上传人脸图片
    /// <para>用户需要录入人脸图片才可以使用门禁考勤机，使用该接口上传门禁用户的人脸图片（multipart/form-data）。</para>
    /// <para>所需权限：acs:users（查看、更新智能门禁用户，任一即可）；user_id_type 取 user_id 时需字段权限 contact:user.employee_id:readonly。</para>
    /// <para><see href="https://open.feishu.cn/document/server-docs/acs-v1/user/update">接口文档</see></para>
    /// </summary>
    /// <param name="user_id">用户 ID，示例值：ou_7dab8a3d3cdcc9da365777c7ad535d62</param>
    /// <param name="request">上传请求体（files：人脸图片本地路径；file_type：jpg/png；file_name：带后缀的文件名，均必填）</param>
    /// <param name="user_id_type">用户 ID 类型（open_id/union_id/user_id），默认 open_id</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns>成功时 data 为空对象</returns>
    [Put("/open-apis/acs/v1/users/{user_id}/face")]
    Task<FeishuNullDataApiResult?> UploadUserFaceAsync(
        [Path] string user_id,
        [FormContent] UploadUserFaceRequest request,
        [Query("user_id_type")] string? user_id_type = null,
        CancellationToken cancellationToken = default);


    /// <summary>
    /// 下载人脸图片
    /// <para>对于已经录入人脸图片的用户，可以使用该接口下载用户人脸图片。</para>
    /// <para>所需权限：acs:users（查看、更新智能门禁用户，任一即可）；user_id_type 取 user_id 时需字段权限 contact:user.employee_id:readonly。</para>
    /// <para><see href="https://open.feishu.cn/document/server-docs/acs-v1/user/get-2">接口文档</see></para>
    /// </summary>
    /// <param name="user_id">用户 ID，示例值：ou_7dab8a3d3cdcc9da365777c7ad535d62</param>
    /// <param name="is_cropped">是否返回裁剪图</param>
    /// <param name="user_id_type">用户 ID 类型（open_id/union_id/user_id），默认 open_id</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns>
    /// 成功时返回响应的二进制内容（取自 <c>HttpContent.ReadAsByteArrayAsync</c>，不会为 <see langword="null"/>；空响应体对应空数组）。
    /// </returns>
    /// <exception cref="ApiException">
    /// 服务端返回非 2xx 状态码时抛出（由 HTTP 执行器统一抛出，异常携带 <c>StatusCode</c> 与响应内容）。
    /// <para>
    /// 注意：飞书部分业务错误以 HTTP 200 + JSON 错误体（<c>{"code":...,"msg":...}</c>）返回，
    /// 此时本方法会把错误 JSON 当作文件内容返回。落盘前应按 <c>Content-Type</c> 自检，详见 <c>documents/ErrorHandling.md</c>。
    /// </para>
    /// </exception>
    [Get("/open-apis/acs/v1/users/{user_id}/face")]
    Task<byte[]?> DownloadUserFaceAsync(
        [Path] string user_id,
        [Query("is_cropped")] bool? is_cropped = null,
        [Query("user_id_type")] string? user_id_type = null,
        CancellationToken cancellationToken = default);
}
