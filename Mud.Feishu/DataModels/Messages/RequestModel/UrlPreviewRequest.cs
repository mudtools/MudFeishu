// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Messages;

/// <summary>
/// 更新 URL 预览请求体
/// </summary>
public class UrlPreviewRequest
{
    /// <summary>
    /// URL 预览的 preview_tokens 列表。需要通过拉取链接预览数据回调获取 preview_tokens。
    /// </summary>
    [JsonPropertyName("preview_tokens")]
    public List<string> PreviewTokens { get; set; } = [];

    /// <summary>
    /// 需要更新 URL 预览的用户 open_id。若不传，则默认更新 URL 预览所在会话的所有成员；若用户不在 URL 所在会话，则无法触发更新该用户对应的 URL 预览结果。
    /// </summary>
    [JsonPropertyName("open_ids")]
    public List<string> OpenIds { get; set; } = [];
}