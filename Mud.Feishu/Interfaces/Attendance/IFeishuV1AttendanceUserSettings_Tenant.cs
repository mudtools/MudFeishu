// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
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
[Header(Consts.Authorization)]
[Token(TokenType.TenantAccessToken)]
public interface IFeishuTenantV1AttendanceUserSettings : IFeishuAppContextSwitcher
{
    /// <summary>
    /// 修改授权内员工的用户设置信息，包括人脸照片文件 ID。
    /// <para>修改用户人脸识别信息目前只支持 API 方式修改，管理后台已无法修改。</para>
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
    /// </summary>
    /// <param name="userSettingsQueryRequest">批量查询用户人脸识别信息请求体。</param>
    /// <param name="employee_type">响应体或请求体中 user_id 的员工 ID 类型。</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns></returns>
    [Get("/open-apis/attendance/v1/user_settings/query")]
    Task<FeishuApiResult<UserSettingsResult>?> QueryUserSettingAsync(
            [Body] UserSettingsQueryRequest userSettingsQueryRequest,
            [Query("employee_type")] string employee_type = Consts.User_Id_Type,
            CancellationToken cancellationToken = default);

    /// <summary>
    /// 上传用户人脸照片并获取文件 ID，对应小程序端的人脸录入功能。
    /// </summary>
    /// <param name="uploadFileRequest">需要上传的用户人脸照片文件。</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns></returns>
    [Post("/open-apis/attendance/v1/files/upload")]
    Task<FeishuApiResult<UserFileUploadResult>?> UploadFileAsync(
        [FormContent] UploadFileRequest uploadFileRequest,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 通过文件 ID 下载用户的头像照片文件。
    /// </summary>
    /// <param name="file_id">需要下载的用户人脸照片文件ID。</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns></returns>
    [Get("/open-apis/attendance/v1/files/{file_id}/download")]
    Task<byte[]?> DownloadFileAsync([Path] string file_id, CancellationToken cancellationToken = default);
}
