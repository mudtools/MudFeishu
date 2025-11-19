// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Employees;

/// <summary>
/// 异常信息类，包含数据异常的相关信息
/// </summary>
public class AbnormalInfo
{
    /// <summary>
    /// 行错误码
    /// </summary>
    [JsonPropertyName("row_error")]
    public int RowError { get; set; }

    /// <summary>
    /// 字段错误字典，键为字段名，值为错误码
    /// </summary>
    [JsonPropertyName("field_errors")]
    public Dictionary<string, int> FieldErrors { get; set; } = [];

    /// <summary>
    /// 异常记录ID
    /// </summary>
    [JsonPropertyName("id")]
    public string? Id { get; set; }
}