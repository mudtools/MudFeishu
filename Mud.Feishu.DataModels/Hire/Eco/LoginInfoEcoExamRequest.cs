// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Hire;

/// <summary>
/// 回传笔试安排结果请求体
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Hire")]
public class LoginInfoEcoExamRequest
{
    /// <summary>
    /// <para>状态码：0 表示成功；非 0 表示服务商内部的失败错误码</para>
    /// <para>必填：否</para>
    /// <para>示例值：0</para>
    /// </summary>
    [JsonPropertyName("result")]
    public int? Result { get; set; }

    /// <summary>
    /// <para>安排成功或失败的描述信息</para>
    /// <para>必填：否</para>
    /// <para>示例值：success</para>
    /// </summary>
    [JsonPropertyName("msg")]
    public string? Msg { get; set; }

    /// <summary>
    /// <para>笔试作答信息</para>
    /// <para>必填：是</para>
    /// </summary>
    [JsonPropertyName("exam_login_info")]
    public EcoExamLoginInfo? ExamLoginInfo { get; set; }
}
