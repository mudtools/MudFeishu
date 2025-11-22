// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Messages;

/// <summary>
/// 批量发送消息请求体基类。
/// </summary>
/// <typeparam name="T">消息内容类型</typeparam>
public abstract class BatchSendMessageRequestBase<T>
{
    /// <summary>
    /// 用户 open_id 列表。
    /// <para>示例值：["ou_18eac85d35a26f989317ad4f02e8bbbb","ou_461cf042d9eedaa60d445f26dc747d5e"]</para>
    /// </summary>
    [JsonPropertyName("open_ids")]
    public List<string> OpenIds { get; set; } = [];

    /// <summary>
    /// 部门 ID 列表。列表内支持传入部门 department_id 和 open_department_id
    /// <para>示例值：["3dceba33a33226","d502aaa9514059", "od-5b91c9affb665451a16b90b4be367efa"]</para>
    /// </summary>
    [JsonPropertyName("department_ids")]
    public List<string> DepartmentIds { get; set; } = [];

    /// <summary>
    /// 消息类型。支持的消息类型有：
    /// <para> text：文本</para>
    /// <para> image：图片</para>
    /// <para> post：富文本</para>
    /// <para> share_chat：分享群名片</para>
    /// <para> interactive：卡片</para>
    /// </summary>
    [JsonPropertyName("msg_type")]
    public
#if NET7_0_OR_GREATER
        required
#endif
        virtual string? MsgType
    { get; set; }

    /// <summary>
    /// 消息内容，JSON 结构。
    /// </summary>
    [JsonPropertyName("content")]
    public T? Content { get; set; }
}