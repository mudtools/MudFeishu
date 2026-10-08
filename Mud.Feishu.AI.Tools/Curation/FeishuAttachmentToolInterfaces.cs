// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.AI.FeishuTools.Curation;

// <summary>
// WP7 上传类工具接口集（AT-F07 的上传部分 / AT-F08）：im.send_image / im.send_file。
// </summary>
// <remarks>
// <para>
// <b>参数形态（架构决定）</b>：只接受 <b>URL</b>（<c>image_url</c>/<c>file_url</c>），
// <b>不接受本地路径</b>——进程内 Agent 拿到本地绝对路径是危险信号（模型不可能知道宿主磁盘布局，
// 给出路径往往意味着提示注入或幻觉），直接以 <c>invalid_args</c> 拒绝。
// </para>
// <para>
// <b>落盘由宿主负责</b>：URL → 本地文件的下载/落盘/校验经
// <c>IFeishuAttachmentStager</c>（宿主实现，见 WP7 的 U-5 取舍）；宿主未注册时本组工具
// <b>不注册</b>（软缺席）。
// </para>
// </remarks>

/// <summary>工具接口：im.send_image（上传图片并发送：<c>POST /open-apis/im/v1/images</c> → <c>POST /open-apis/im/v1/messages</c>）。</summary>
[FeishuTool("im.send_image",
    Description = "把一张网络图片作为消息发送到指定会话（image_url 为 http/https 绝对地址，由宿主负责下载落盘）。写操作：默认空名单不启用，启用前须经宿主授权（IToolExecutionAuthorizer），需 im:resource 与 im:message:send_as_bot。",
    RequiredScopes = ["im:resource", "im:message:send_as_bot"],
    IsWrite = true,
    Source = nameof(IFeishuTenantV1Message) + "." + nameof(IFeishuTenantV1Message.SendMessageAsync))]
public interface IFeishuTenantImSendImageTool
{
    /// <summary>发送图片消息。</summary>
    /// <returns>白名单投影后的 JSON 文本（message_id）；<c>dry_run=true</c> 时返回请求摘要且不调用下游。</returns>
    Task<string> SendImageAsync(
        [ToolParameter("receive_id", "接收方 ID（与 receive_id_type 匹配：chat_id 填 oc_xxx，open_id 填 ou_xxx）", Required = true)] string receive_id,
        [ToolParameter("image_url", "图片的 http/https 绝对地址（不接受本地路径）", Required = true)] string image_url,
        [ToolParameter("receive_id_type", "接收方 ID 类型（可选，默认 chat_id）")] string? receive_id_type = null,
        [ToolParameter("file_name", "建议文件名（可选，含扩展名；宿主据此判断图片格式）")] string? file_name = null,
        [ToolParameter("dry_run", "仅预演不发送（可选，默认 false）：返回将要下发的 method/path 与字段摘要，不调用下游")] bool? dry_run = null,
        CancellationToken cancellationToken = default);
}

/// <summary>工具接口：im.send_file（上传文件并发送：<c>POST /open-apis/im/v1/files</c> → <c>POST /open-apis/im/v1/messages</c>）。</summary>
[FeishuTool("im.send_file",
    Description = "把一个网络文件作为消息发送到指定会话（file_url 为 http/https 绝对地址，由宿主负责下载落盘）。file_name 必须带扩展名。写操作：默认空名单不启用，启用前须经宿主授权（IToolExecutionAuthorizer），需 im:resource 与 im:message:send_as_bot。",
    RequiredScopes = ["im:resource", "im:message:send_as_bot"],
    IsWrite = true,
    Source = nameof(IFeishuTenantV1Message) + "." + nameof(IFeishuTenantV1Message.SendMessageAsync))]
public interface IFeishuTenantImSendFileTool
{
    /// <summary>发送文件消息。</summary>
    /// <returns>白名单投影后的 JSON 文本（message_id）；<c>dry_run=true</c> 时返回请求摘要且不调用下游。</returns>
    Task<string> SendFileAsync(
        [ToolParameter("receive_id", "接收方 ID（与 receive_id_type 匹配：chat_id 填 oc_xxx，open_id 填 ou_xxx）", Required = true)] string receive_id,
        [ToolParameter("file_url", "文件的 http/https 绝对地址（不接受本地路径）", Required = true)] string file_url,
        [ToolParameter("file_name", "文件名（含扩展名，如 周报.pdf）", Required = true)] string file_name,
        [ToolParameter("receive_id_type", "接收方 ID 类型（可选，默认 chat_id）")] string? receive_id_type = null,
        [ToolParameter("dry_run", "仅预演不发送（可选，默认 false）：返回将要下发的 method/path 与字段摘要，不调用下游")] bool? dry_run = null,
        CancellationToken cancellationToken = default);
}
