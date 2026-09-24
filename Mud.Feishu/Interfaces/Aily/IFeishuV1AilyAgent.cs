// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.DataModels.Aily;

namespace Mud.Feishu.Interfaces;


/// <summary>
/// 飞书 Aily SDK 是一组服务端 OpenAPI 的封装，用于让用户以编程方式调用飞书 Aily（智能伙伴/智能体）的智能体对话、会话管理、附件与产物能力，把它集成到自己的业务系统里。
/// <para>接口详细文档请参见：<see href="https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/aily-v1/agent-agent_chat/create"/></para>
/// </summary>
[HttpClientApi(TokenManage = nameof(IFeishuAppManager), IsAbstract = true)]
[Token(FeishuTokenTypes.TenantAccessToken, Name = Consts.Authorization)]
public interface IFeishuV1AilyAgent
{
    /// <summary>
    /// 上传附件
    /// <para>用于上传需智能体分析的文件，上传成功后返回附件 ID；可在发起智能体对话时通过 agent_attachment_ids 引用。</para>
    /// <para>注意事项：</para>
    /// <para>- type=image/file 时 file 必传（png/jpg/pdf，文件最大 40M、图片最大 5M），doc_url 不生效；</para>
    /// <para>- type=feishu_doc/bitable 时 doc_url 必传，file 不生效。</para>
    /// <para><see href="https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/aily-v1/agent-agent_attachment/create">接口文档</see></para>
    /// </summary>
    /// <param name="agent_id">智能体 ID，通过智能体后台详情的地址栏中获取，示例值：agent_4k4ue29hpwrx2</param>
    /// <param name="request">上传附件请求体（multipart/form-data）</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns>上传成功后返回附件 ID</returns>
    [Post("/open-apis/aily/v1/agents/{agent_id}/attachments")]
    Task<FeishuApiResult<CreateAgentAttachmentResult>?> CreateAgentAttachmentAsync(
        [Path] string agent_id,
        [FormContent] CreateAgentAttachmentRequest request,
        CancellationToken cancellationToken = default);


    /// <summary>
    /// 查询会话列表
    /// <para>用于查询智能体的会话列表，支持分页。</para>
    /// <para><see href="https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/aily-v1/agent-agent_chat_session/list">接口文档</see></para>
    /// </summary>
    /// <param name="agent_id">智能体 ID，通过智能体后台详情的地址栏中获取，示例值：agent_4k4ue29hpwrx2</param>
    /// <param name="page_size">分页大小，示例值：10</param>
    /// <param name="page_token">分页标记，第一次请求不填，表示从头开始遍历；分页查询结果还有更多项时会同时返回新的 page_token，下次遍历可采用该 page_token 获取查询结果</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns>返回智能体会话分页列表（下一页标记在 <c>Data.NextPageToken</c>）</returns>
    [Get("/open-apis/aily/v1/agents/{agent_id}/sessions")]
    Task<FeishuApiResult<AgentChatSessionPageListResult>?> GetAgentChatSessionPageListAsync(
        [Path] string agent_id,
        [Query("page_size")] int page_size = Consts.PageSize_20,
        [Query("page_token")] string? page_token = null,
        CancellationToken cancellationToken = default);


    /// <summary>
    /// 获取指定会话信息
    /// <para>用于查询智能体某次指定会话的详细信息，包括状态、创建时间与对话轮次。</para>
    /// <para><see href="https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/aily-v1/agent-agent_chat_session/get">接口文档</see></para>
    /// </summary>
    /// <param name="agent_id">智能体 ID，通过智能体后台详情的地址栏中获取，示例值：agent_ashcascsa</param>
    /// <param name="agent_chat_session_id">会话 ID，发起对话、创建会话或查询会话列表获取，示例值：conversation_asdasda</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns>返回智能体会话详细信息</returns>
    [Get("/open-apis/aily/v1/agents/{agent_id}/sessions/{agent_chat_session_id}")]
    Task<FeishuApiResult<AgentChatSessionOopsResult>?> GetAgentChatSessionAsync(
        [Path] string agent_id,
        [Path] string agent_chat_session_id,
        CancellationToken cancellationToken = default);


    /// <summary>
    /// 删除会话
    /// <para>用于删除智能体的某次会话。</para>
    /// <para><see href="https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/aily-v1/agent-agent_chat_session/delete">接口文档</see></para>
    /// </summary>
    /// <param name="agent_id">智能体 ID，通过智能体后台详情的地址栏中获取，示例值：agent_asdsad</param>
    /// <param name="agent_chat_session_id">会话 ID，发起对话、创建会话或查询会话列表获取，示例值：conversation_assadasd</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns>删除成功时 <c>Data</c> 为空对象</returns>
    [Delete("/open-apis/aily/v1/agents/{agent_id}/sessions/{agent_chat_session_id}")]
    Task<FeishuNullDataApiResult?> DeleteAgentChatSessionAsync(
        [Path] string agent_id,
        [Path] string agent_chat_session_id,
        CancellationToken cancellationToken = default);


    /// <summary>
    /// 创建会话
    /// <para>用于智能体创建空白会话。</para>
    /// <para><see href="https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/aily-v1/agent-agent_chat_session/create">接口文档</see></para>
    /// </summary>
    /// <param name="agent_id">智能体 ID，通过智能体后台详情的地址栏中获取，示例值：agent_4k4ue29hpwrx2</param>
    /// <param name="request">创建会话请求体</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns>创建成功后返回会话 ID 与会话名</returns>
    [Post("/open-apis/aily/v1/agents/{agent_id}/sessions")]
    Task<FeishuApiResult<AgentChatSessionOopsResult>?> CreateAgentChatSessionAsync(
        [Path] string agent_id,
        [Body] CreateAgentChatSessionRequest request,
        CancellationToken cancellationToken = default);


    /// <summary>
    /// 发起智能体对话
    /// <para>异步发起一轮智能体对话，提交用户消息后立即返回对话 ID，触发智能体在后台运行；可通过获取对话结果接口轮询运行状态与回复。</para>
    /// <para>注意事项：请求体 <c>Stream=false</c>（或不传）时本接口返回 JSON；如需 SSE 流式输出请使用 <see cref="CreateAgentChatStreamAsync"/>。</para>
    /// <para><see href="https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/aily-v1/agent-agent_chat/create">接口文档</see></para>
    /// </summary>
    /// <param name="agent_id">智能体 ID，通过智能体后台详情的地址栏中获取，示例值：agent_4k4ue29hpwrx2</param>
    /// <param name="request">发起对话请求体（应保持 <c>Stream</c> 为 <see langword="false"/> 或 null）</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns>返回对话 ID 与会话 ID</returns>
    [Post("/open-apis/aily/v1/agents/{agent_id}/chats")]
    Task<FeishuApiResult<CreateAgentChatResult>?> CreateAgentChatAsync(
        [Path] string agent_id,
        [Body] CreateAgentChatRequest request,
        CancellationToken cancellationToken = default);


    /// <summary>
    /// 发起智能体对话（SSE 流式输出）
    /// <para>与 <see cref="CreateAgentChatAsync"/> 为同一端点，但请求体 <c>Stream=true</c>，响应为 Server-sent Events（SSE）流，超时时间 5 分钟。</para>
    /// <para>可在请求体传入 <c>SessionId</c> 复用既有会话进行多轮对话。</para>
    /// <para><see href="https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/aily-v1/agent-agent_chat/create">接口文档</see></para>
    /// </summary>
    /// <param name="agent_id">智能体 ID，通过智能体后台详情的地址栏中获取，示例值：agent_4k4ue29hpwrx2</param>
    /// <param name="request">发起对话请求体；调用前必须将 <c>Stream</c> 设为 <see langword="true"/>，否则响应不是 SSE 流</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns>
    /// 成功时返回承载 SSE 流的 <see cref="HttpResponseMessage"/>（可空签名，实际成功路径不会为 <see langword="null"/>）；
    /// 调用方负责读取 <c>Content</c> 流并解析事件后释放响应。
    /// </returns>
    /// <exception cref="ApiException">
    /// 服务端返回非 2xx 状态码时抛出（由 HTTP 执行器统一抛出，异常携带 <c>StatusCode</c> 与响应内容）。
    /// <para>
    /// 注意：飞书部分业务错误以 HTTP 200 + JSON 错误体返回（如错误码 2700001 param is invalid），
    /// 此时本方法会把响应原样返回，调用方需按 Content-Type 自检，详见 <c>documents/ErrorHandling.md</c>。
    /// </para>
    /// </exception>
    [Post("/open-apis/aily/v1/agents/{agent_id}/chats")]
    Task<HttpResponseMessage?> CreateAgentChatStreamAsync(
        [Path] string agent_id,
        [Body] CreateAgentChatRequest request,
        CancellationToken cancellationToken = default);


    /// <summary>
    /// 获取对话结果
    /// <para>用于获取智能体的对话回复，内容包括文字和产物等信息。</para>
    /// <para><see href="https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/aily-v1/agent-agent_chat/get">接口文档</see></para>
    /// </summary>
    /// <param name="agent_id">智能体 ID，通过智能体后台详情的地址栏中获取，示例值：agent_4k4ue29hpwrx2</param>
    /// <param name="agent_chat_id">智能体对话 ID，通过发起智能体对话接口获取，示例值：7640186506971926032</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns>返回对话回复内容、结束原因与状态</returns>
    [Get("/open-apis/aily/v1/agents/{agent_id}/chats/{agent_chat_id}")]
    Task<FeishuApiResult<AgentChatResult>?> GetAgentChatAsync(
        [Path] string agent_id,
        [Path] string agent_chat_id,
        CancellationToken cancellationToken = default);


    /// <summary>
    /// 下载智能体产物
    /// <para>根据产物 ID（agent_artifact_id）获取该产物的下载地址及基础信息（名称、URL），用于拉取智能体会话中生成的图片、文件、云文档等产物。</para>
    /// <para>注意事项：返回的下载 URL 24 小时内有效。</para>
    /// <para><see href="https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/aily-v1/agent-agent_artifact/get">接口文档</see></para>
    /// </summary>
    /// <param name="agent_id">智能体 ID，通过智能体后台详情的地址栏中获取，示例值：agent_4k6jukr5skfax</param>
    /// <param name="agent_artifact_id">智能体产物 ID，调用获取对话结果接口获取，示例值：artifact_4k6m2dbmrjeqf</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns>返回产物 ID、名称与 24 小时有效的下载 URL</returns>
    [Get("/open-apis/aily/v1/agents/{agent_id}/artifacts/{agent_artifact_id}")]
    Task<FeishuApiResult<AgentArtifactOopsResult>?> GetAgentArtifactAsync(
        [Path] string agent_id,
        [Path] string agent_artifact_id,
        CancellationToken cancellationToken = default);
}
