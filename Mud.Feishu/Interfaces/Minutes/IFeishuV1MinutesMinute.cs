// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.DataModels.Minutes;

namespace Mud.Feishu.Interfaces;


/// <summary>
/// 飞书妙记（Minutes）SDK 是一组服务端 OpenAPI 的封装，用于获取妙记基础信息、音视频下载链接、文字记录、统计数据、AI 产物与搜索妙记。本接口仅声明支持 tenant_access_token 与 user_access_token 双令牌调用的只读端点；剪辑、导入生成、事件订阅等 user-only 写端点见 <see cref="IFeishuUserV1MinutesMinute"/>。
/// <para>接口详细文档请参见：<see href="https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/minutes-v1/minute/get"/></para>
/// </summary>
[HttpClientApi(TokenManage = nameof(IFeishuAppManager), IsAbstract = true)]
[Token(FeishuTokenTypes.TenantAccessToken, Name = Consts.Authorization)]
public interface IFeishuV1MinutesMinute : IFeishuAppContextSwitcher
{
    /// <summary>
    /// 获取妙记信息
    /// <para>获取妙记基础概览，包括所有者、创建时间、标题、封面、时长与链接。</para>
    /// <para>限频：5 次/秒。所需权限：minutes:minutes.basic:read / minutes:minutes（至少其一）。</para>
    /// <para><see href="https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/minutes-v1/minute/get">接口文档</see></para>
    /// </summary>
    /// <param name="minute_token">妙记唯一标识，取自妙记 URL 链接末段，长度 24 字符，示例值：obcnq3b9jl72l83w4f14xxxx</param>
    /// <param name="user_id_type">用户 ID 类型（open_id/union_id/user_id），默认 open_id；取 user_id 时需 contact:user.employee_id:readonly 字段权限</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns>返回妙记基本信息（token、owner_id、create_time、title、cover、duration、url、note_id）</returns>
    [Get("/open-apis/minutes/v1/minutes/{minute_token}")]
    Task<FeishuApiResult<GetMinuteResult>?> GetMinuteAsync(
        [Path] string minute_token,
        [Query("user_id_type")] string? user_id_type = null,
        CancellationToken cancellationToken = default);


    /// <summary>
    /// 下载妙记音视频文件
    /// <para>获取妙记音视频文件的下载链接（有效期 1 天），用于批量下载。</para>
    /// <para>限频：5 次/秒。所需权限：minutes:minutes.media:export（下载妙记音视频文件）。</para>
    /// <para><see href="https://open.feishu.cn/document/minutes-v1/minute-media/get">接口文档</see></para>
    /// </summary>
    /// <param name="minute_token">妙记唯一标识，取自妙记 URL 链接末段，长度 24 字符，示例值：obcnq3b9jl72l83w4f14xxxx</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns>返回音视频文件下载链接（有效期 1 天）</returns>
    [Get("/open-apis/minutes/v1/minutes/{minute_token}/media")]
    Task<FeishuApiResult<GetMinuteMediaResult>?> GetMinuteMediaAsync(
        [Path] string minute_token,
        CancellationToken cancellationToken = default);


    /// <summary>
    /// 导出妙记文字记录
    /// <para>获取妙记的文字记录（逐字稿），返回文件二进制流。</para>
    /// <para>限频：5 次/秒。所需权限：minutes:minutes.transcript:export（导出妙记文字转写）。</para>
    /// <para><see href="https://open.feishu.cn/document/minutes-v1/minute-transcript/get">接口文档</see></para>
    /// </summary>
    /// <param name="minute_token">妙记唯一标识，取自妙记 URL 链接末段，长度 24 字符，示例值：obcnq3b9jl72l83w4f14xxxx</param>
    /// <param name="need_speaker">是否包含说话人</param>
    /// <param name="need_timestamp">是否包含时间戳</param>
    /// <param name="file_format">导出文件格式，示例值：txt、srt</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns>
    /// 成功时返回文字记录文件的二进制内容（取自 <c>HttpContent.ReadAsByteArrayAsync</c>，不会为 <see langword="null"/>；空响应体对应空数组）。
    /// </returns>
    /// <exception cref="ApiException">
    /// 服务端返回非 2xx 状态码时抛出（由 HTTP 执行器统一抛出，异常携带 <c>StatusCode</c> 与响应内容）。
    /// <para>
    /// 注意：飞书部分业务错误以 HTTP 200 + JSON 错误体（<c>{"code":...,"msg":...}</c>）返回，
    /// 此时本方法会把错误 JSON 当作文件内容返回。落盘前应按 <c>Content-Type</c> 自检，详见 <c>documents/ErrorHandling.md</c>。
    /// </para>
    /// </exception>
    [Get("/open-apis/minutes/v1/minutes/{minute_token}/transcript")]
    Task<byte[]?> GetMinuteTranscriptAsync(
        [Path] string minute_token,
        [Query("need_speaker")] bool? need_speaker = null,
        [Query("need_timestamp")] bool? need_timestamp = null,
        [Query("file_format")] string? file_format = null,
        CancellationToken cancellationToken = default);


    /// <summary>
    /// 获取妙记统计数据
    /// <para>获取妙记的访问统计数据，包括 PV、UV、访问用户 ID 与访问时间。</para>
    /// <para>限频：5 次/秒。所需权限：minutes:minutes.statistics:read（读取妙记统计信息）。</para>
    /// <para><see href="https://open.feishu.cn/document/server-docs/minutes-v1/minute-statistics/get">接口文档</see></para>
    /// </summary>
    /// <param name="minute_token">妙记唯一标识，取自妙记 URL 链接末段，长度 24 字符，示例值：obcnq3b9jl72l83w4f14xxxx</param>
    /// <param name="user_id_type">用户 ID 类型（open_id/union_id/user_id），默认 open_id；取 user_id 时需 contact:user.employee_id:readonly 字段权限</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns>返回访问统计（UV、PV 与用户浏览列表）</returns>
    [Get("/open-apis/minutes/v1/minutes/{minute_token}/statistics")]
    Task<FeishuApiResult<GetMinuteStatisticsResult>?> GetMinuteStatisticsAsync(
        [Path] string minute_token,
        [Query("user_id_type")] string? user_id_type = null,
        CancellationToken cancellationToken = default);


    /// <summary>
    /// 获取妙记 AI 产物
    /// <para>获取妙记的 AI 产物，包括总结、章节、待办、推荐关键词与逐字稿。</para>
    /// <para>限频：5 次/秒。所需权限：minutes:minutes.artifacts:read（获取妙记 AI 产物）。</para>
    /// <para><see href="https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/minutes-v1/minute/artifacts">接口文档</see></para>
    /// </summary>
    /// <param name="minute_token">妙记唯一标识，取自妙记 URL 链接末段，长度 24 字符，示例值：obcnq3b9jl72l83w4f149w9c</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns>返回 AI 产物（总结、章节列表、待办列表、关键词与逐字稿）</returns>
    [Get("/open-apis/minutes/v1/minutes/{minute_token}/artifacts")]
    Task<FeishuApiResult<GetMinuteArtifactsResult>?> GetMinuteArtifactsAsync(
        [Path] string minute_token,
        CancellationToken cancellationToken = default);


    /// <summary>
    /// 搜索妙记
    /// <para>按关键词、所有者、参与者与创建时间等多条件搜索妙记列表，支持分页。</para>
    /// <para>限频：5 次/秒。所需权限：minutes:minutes.search:read（搜索妙记）。搜索时间范围最大为 1 个月。</para>
    /// <para><see href="https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/minutes-v1/minute/search">接口文档</see></para>
    /// </summary>
    /// <param name="request">搜索请求体（query 关键词 10～50 字符；filter 过滤条件（owner_ids/participant_ids/create_time）；sorter 排序方式；至少提供一个过滤条件）</param>
    /// <param name="page_size">分页大小，范围 1～30，默认 15</param>
    /// <param name="page_token">分页标记，首次请求不填，翻页时取上一次返回的 page_token</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns>返回妙记搜索结果（items、total、has_more、page_token、notice）</returns>
    [Post("/open-apis/minutes/v1/minutes/search")]
    Task<FeishuApiResult<SearchMinutesResult>?> SearchMinutesAsync(
        [Body] SearchMinutesRequest request,
        [Query("page_size")] int? page_size = null,
        [Query("page_token")] string? page_token = null,
        CancellationToken cancellationToken = default);
}
