// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Performance;

/// <summary>
/// 批量删除补充信息请求体
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Performance")]
public class DeleteAdditionalInformationRequest
{
    /// <summary>
    /// <para>评估周期 ID，长度 1~100 字符</para>
    /// <para>必填：是</para>
    /// <para>示例值：7348736302176534547</para>
    /// </summary>
    [JsonPropertyName("semester_id")]
    public string? SemesterId { get; set; }

    /// <summary>
    /// <para>补充信息 ID 列表（1~100 个）</para>
    /// <para>必填：是</para>
    /// <para>示例值：["7350195758357807123"]</para>
    /// </summary>
    [JsonPropertyName("additional_informations")]
    public string[]? AdditionalInformations { get; set; }
}
