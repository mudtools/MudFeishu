// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Mail;

/// <summary>
/// <para>多实体搜索返回的单个实体详情</para>
/// <para>接口详细文档请参见：<see href="https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/reference/mail-v1/multi_entity/search"/></para>
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Mail")]
public class MultiEntitySearchItem
{
    /// <summary>
    /// <para>标识当前的实体是哪种（例如：user、chat 等）</para>
    /// <para>必填：否</para>
    /// <para>示例值：user</para>
    /// </summary>
    [JsonPropertyName("type")]
    public string? Type { get; set; }

    /// <summary>
    /// <para>唯一标识 ID</para>
    /// <para>必填：否</para>
    /// <para>示例值：6911188411932033028</para>
    /// </summary>
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    /// <summary>
    /// <para>名称</para>
    /// <para>必填：否</para>
    /// <para>示例值：张三</para>
    /// </summary>
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    /// <summary>
    /// <para>邮箱地址</para>
    /// <para>必填：否</para>
    /// <para>示例值：zhangsan@bytedance.com</para>
    /// </summary>
    [JsonPropertyName("email")]
    public string? Email { get; set; }

    /// <summary>
    /// <para>备注名</para>
    /// <para>必填：否</para>
    /// <para>示例值：备注名</para>
    /// </summary>
    [JsonPropertyName("display_name")]
    public string? DisplayName { get; set; }

    /// <summary>
    /// <para>群成员数量</para>
    /// <para>必填：否</para>
    /// <para>示例值：128</para>
    /// </summary>
    [JsonPropertyName("member_count")]
    public int? MemberCount { get; set; }

    /// <summary>
    /// <para>用户 ID</para>
    /// <para>必填：否</para>
    /// <para>示例值：ou_2d131f4c3a28b0</para>
    /// </summary>
    [JsonPropertyName("user_id")]
    public string? UserId { get; set; }

    /// <summary>
    /// <para>部门信息</para>
    /// <para>必填：否</para>
    /// <para>示例值：飞书研发团队</para>
    /// </summary>
    [JsonPropertyName("department")]
    public string? Department { get; set; }

    /// <summary>
    /// <para>群聊或会话 ID</para>
    /// <para>必填：否</para>
    /// <para>示例值：oc_40ed357053e34b9</para>
    /// </summary>
    [JsonPropertyName("chat_id")]
    public string? ChatId { get; set; }

    /// <summary>
    /// <para>标签</para>
    /// <para>必填：否</para>
    /// <para>示例值：超大群/部门群/邮箱联系人/邮件组/外部</para>
    /// </summary>
    [JsonPropertyName("tag")]
    public string? Tag { get; set; }
}
