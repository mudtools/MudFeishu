// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.DataModels.Acs;

namespace Mud.Feishu;


/// <summary>
/// 飞书智能门禁（ACS）门禁记录 SDK 是一组服务端 OpenAPI 的封装，用于按时间范围（跨度不超过 30 天）分页查询用户在门禁考勤机上成功开门或打卡产生的识别记录，并下载人脸识别方式开门时的抓拍图。本接口全部端点仅支持 tenant_access_token 调用，仅支持自建应用。
/// <para>接口详细文档请参见：<see href="https://open.feishu.cn/document/server-docs/acs-v1/access_record/list"/></para>
/// </summary>
[HttpClientApi(TokenManage = nameof(IFeishuAppManager), RegistryGroupName = "Acs")]
[Token(FeishuTokenTypes.TenantAccessToken, Name = Consts.Authorization)]
public interface IFeishuTenantV1AcsAccessRecord : IFeishuAppContextSwitcher
{
    /// <summary>
    /// 获取门禁记录列表
    /// <para>用户在门禁考勤机上成功开门或打卡后，智能门禁应用都会生成一条门禁记录；该接口返回满足查询参数的识别记录，时间跨度不能超过 30 天（查询参数采用查询对象模式 <see cref="AccessRecordListQuery"/>，见 AGENTS.md API-2）。</para>
    /// <para>所需权限：acs:access_record:readonly（查看智能门禁记录）；user_id_type 取 user_id 时需字段权限 contact:user.employee_id:readonly。</para>
    /// <para><see href="https://open.feishu.cn/document/server-docs/acs-v1/access_record/list">接口文档</see></para>
    /// </summary>
    /// <param name="from">记录开始时间，单位秒，示例值：1624520521（必填）</param>
    /// <param name="to">记录结束时间，单位秒，示例值：1624520521（必填；from 至 to 时间跨度不能超过 30 天）</param>
    /// <param name="query">分页大小（默认 100、最大 500）、分页标记、设备 ID 与用户 ID 类型等可选查询参数</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns>返回门禁记录分页列表（items、page_token、has_more）</returns>
    [Get("/open-apis/acs/v1/access_records")]
    Task<FeishuApiResult<GetAccessRecordListResult>?> GetAccessRecordListAsync(
        [Query("from")] long from,
        [Query("to")] long to,
        [Query] AccessRecordListQuery? query = null,
        CancellationToken cancellationToken = default);


    /// <summary>
    /// 下载开门时的人脸识别图片
    /// <para>对于使用人脸识别方式开门的识别记录，可以使用该接口下载开门时的抓拍图。</para>
    /// <para>所需权限：acs:users（查看、更新智能门禁用户，任一即可）。仅支持自建应用。</para>
    /// <para><see href="https://open.feishu.cn/document/server-docs/acs-v1/access_record/get">接口文档</see></para>
    /// </summary>
    /// <param name="access_record_id">门禁访问记录 ID，示例值：6939433228970082591</param>
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
    [Get("/open-apis/acs/v1/access_records/{access_record_id}/access_photo")]
    Task<byte[]?> DownloadAccessRecordAccessPhotoAsync(
        [Path] string access_record_id,
        CancellationToken cancellationToken = default);
}
