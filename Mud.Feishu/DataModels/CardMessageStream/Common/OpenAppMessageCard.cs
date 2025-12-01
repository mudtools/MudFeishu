// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.CardMessageStream;


/// <summary>
/// <para>应用消息卡片</para>
/// </summary>
public class OpenAppMessageCard
{
    /// <summary>
    /// <para>业务 ID（非必填字段，开发者可自定义业务 ID 以方便管理数据；若不传入，则 API 响应体中会返回系统自动分配的业务 ID）</para>
    /// <para>必填：否</para>
    /// <para>示例值：096e2927-40a6-41a3-9562-314d641d09ae</para>
    /// </summary>
    [JsonPropertyName("biz_id")]
    public string? BizId { get; set; }

    /// <summary>
    /// <para>主标题（在用户界面中最多展示一行，自动省略超出部分的内容；不支持定义字号及颜色）</para>
    /// <para>必填：否</para>
    /// <para>示例值：主标题</para>
    /// </summary>
    [JsonPropertyName("title")]
    public string? Title { get; set; }

    /// <summary>
    /// <para>头像 key</para>
    /// <para>必填：否</para>
    /// <para>示例值：v3_0041_007bca9f-67ba-4199-bf00-4031b12cf226</para>
    /// </summary>
    [JsonPropertyName("avatar_key")]
    public string? AvatarKey { get; set; }

    /// <summary>
    /// <para>预览信息（在用户界面中最多展示一行，自动省略超出部分的内容；支持多个字段拼接、特殊符号和 emoji；不支持定义字号及颜色）</para>
    /// <para>必填：否</para>
    /// <para>示例值：预览信息</para>
    /// </summary>
    [JsonPropertyName("preview")]
    public string? Preview { get; set; }

    /// <summary>
    /// <para>状态标签（非必填字段，如未选择该字段，则默认展示卡片触达时间）</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("status_label")]
    public OpenMessageStatusLabel? StatusLabel { get; set; }


    /// <summary>
    /// <para>交互按钮（非必填字段，如未传入该字段，则不展示按钮；最多展示 2 个按钮）</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("buttons")]
    public OpenAppMessageCardButtons? Buttons { get; set; }

    /// <summary>
    /// <para>卡片整体跳转链接（创建时该参数为必填参数）</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("link")]
    public OpenAppMessageLink? Link { get; set; }


    /// <summary>
    /// <para>即时提醒状态（设置为 true 后，卡片在消息列表临时置顶；设置为 false，消息卡片不置顶）</para>
    /// <para>必填：否</para>
    /// <para>示例值：false</para>
    /// <para>默认值：false</para>
    /// </summary>
    [JsonPropertyName("time_sensitive")]
    public bool? TimeSensitive { get; set; }

    /// <summary>
    /// <para>通知设置，当前可设置通知是否关闭，为空时默认进行通知</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("notify")]
    public AppMessageNotify? Notify { get; set; }

}