// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Users;

/// <summary>
/// 批量获取用户基础信息响应体
/// <para>接口详细文档请参见：<see href="https://open.feishu.cn/document/server-docs/contact-v3/user/basic_batch"/></para>
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Organization")]
public class BasicBatchUserResult
{
    /// <summary>
    /// <para>用户信息列表，仅返回用户的 ID、姓名与国际化名称等基础信息。</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("users")]
    public BasicUserInfo[]? Users { get; set; }
}

/// <summary>
/// 用户基础信息
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Organization")]
public class BasicUserInfo
{
    /// <summary>
    /// <para>用户的 user_id。</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("user_id")]
    public string? UserId { get; set; }

    /// <summary>
    /// <para>用户姓名。</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    /// <summary>
    /// <para>用户的国际化名称。</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("i18n_name")]
    public UserI18nName? I18nName { get; set; }
}

/// <summary>
/// 用户国际化名称
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Organization")]
public class UserI18nName
{
    /// <summary>
    /// <para>中文名。</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("zh_cn")]
    public string? ZhCn { get; set; }

    /// <summary>
    /// <para>日文名。</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("ja_jp")]
    public string? JaJp { get; set; }

    /// <summary>
    /// <para>英文名。</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("en_us")]
    public string? EnUs { get; set; }
}
