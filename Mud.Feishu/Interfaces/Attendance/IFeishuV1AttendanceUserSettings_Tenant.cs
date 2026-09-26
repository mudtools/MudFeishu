// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.DataModels.AttendanceUser;

namespace Mud.Feishu;

/// <summary>
/// 考勤用户管理接口主要实现了修改用户人脸识别信息、批量查询用户人脸识别信息以及上传下载用户人脸识别照片。
/// <para>接口详细文档请参见：<see href="https://open.feishu.cn/document/server-docs/attendance-v1/user_setting/modify"/></para>
/// </summary>
[HttpClientApi(TokenManage = nameof(IFeishuAppManager), RegistryGroupName = "Attendance")]
[Token(FeishuTokenTypes.TenantAccessToken, Name = Consts.Authorization)]
public interface IFeishuTenantV1AttendanceUserSettings : IFeishuAppContextSwitcher
{
    /// <summary>
    /// 修改授权内员工的用户设置信息，包括人脸照片文件 ID。
    /// <para>修改用户人脸识别信息目前只支持 API 方式修改，管理后台已无法修改。</para>
    /// <para>官方文档：<see href="https://open.feishu.cn/api-explorer?from=op_doc_tab&amp;apiName=modify&amp;project=attendance&amp;resource=user_setting&amp;version=v1"/></para>
    /// </summary>
    /// <param name="userFacialRecognitionRequest">修改用户人脸识别信息请求体。</param>
    /// <param name="employee_type">响应体或请求体中 user_id 的员工 ID 类型。</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns></returns>
    [Post("/open-apis/attendance/v1/user_settings/modify")]
    Task<FeishuApiResult<UserSettingsResult>?> ModifyUserSettingAsync(
             [Body] ModifyUserFacialRecognitionRequest userFacialRecognitionRequest,
             [Query("employee_type")] string employee_type = Consts.User_Id_Type,
             CancellationToken cancellationToken = default);

    /// <summary>
    /// 批量查询授权内员工的用户设置信息，包括人脸照片文件 ID、人脸照片更新时间。
    /// <para>对应页面假勤设置-人脸识别。根据返回的 face_key 可以下载人脸信息（下载用户人脸识别照片）。</para>
    /// <para>官方文档：<see href="https://open.feishu.cn/api-explorer?from=op_doc_tab&amp;apiName=query&amp;project=attendance&amp;resource=user_setting&amp;version=v1"/></para>
    /// </summary>
    /// <param name="userSettingsQueryRequest">批量查询用户人脸识别信息请求体。</param>
    /// <param name="employee_type">响应体或请求体中 user_id 的员工 ID 类型。</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns></returns>
    [Get("/open-apis/attendance/v1/user_settings/query")]
    Task<FeishuApiResult<UserSettingsQueryResult>?> QueryUserSettingAsync(
            [Body] UserSettingsQueryRequest userSettingsQueryRequest,
            [Query("employee_type")] string employee_type = Consts.User_Id_Type,
            CancellationToken cancellationToken = default);

    /// <summary>
    /// 上传用户人脸照片并获取文件 ID，对应小程序端的人脸录入功能。
    /// <para>官方文档：<see href="https://open.feishu.cn/api-explorer?from=op_doc_tab&amp;apiName=upload&amp;project=attendance&amp;resource=file&amp;version=v1"/></para>
    /// </summary>
    /// <param name="uploadFileRequest">需要上传的用户人脸照片文件。</param>
    /// <param name="file_name">照片的文件名（含扩展名，必填），如 photo.png。示例值："人脸照片.jpg"。</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns></returns>
    [Post("/open-apis/attendance/v1/files/upload")]
    Task<FeishuApiResult<UserFileUploadResult>?> UploadFileAsync(
        [FormContent] UploadFileRequest uploadFileRequest,
        [Query("file_name")] string file_name,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 通过文件 ID 下载用户的头像照片文件。
    /// <para>官方文档：<see href="https://open.feishu.cn/api-explorer?from=op_doc_tab&amp;apiName=download&amp;project=attendance&amp;resource=file&amp;version=v1"/></para>
    /// </summary>
    /// <param name="file_id">需要下载的用户人脸照片文件ID。</param>
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
    [Get("/open-apis/attendance/v1/files/{file_id}/download")]
    Task<byte[]?> DownloadFileAsync([Path] string file_id, CancellationToken cancellationToken = default);
}
