// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Hire;

/// <summary>
/// 笔试作答登录信息
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Hire")]
public class EcoExamLoginInfo
{
    /// <summary>
    /// <para>笔试链接。若返回的链接已附带候选人唯一标识且无需登录鉴权，可只返回此链接</para>
    /// <para>必填：是</para>
    /// <para>示例值：https://www.exam.com/1234</para>
    /// </summary>
    [JsonPropertyName("exam_url")]
    public string? ExamUrl { get; set; }

    /// <summary>
    /// <para>登录用户名。注意：若笔试链接需要登录鉴权，须返回此登录凭证</para>
    /// <para>必填：否</para>
    /// <para>示例值：waxsdfbhg</para>
    /// </summary>
    [JsonPropertyName("username")]
    public string? Username { get; set; }

    /// <summary>
    /// <para>登录密码。注意：若笔试链接需要登录鉴权，须返回此登录凭证</para>
    /// <para>必填：否</para>
    /// <para>示例值：xxxxxx</para>
    /// </summary>
    [JsonPropertyName("password")]
    public string? Password { get; set; }
}
