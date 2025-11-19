// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Employees;

/// <summary>
/// 在职员工流转到待离职选项参数
/// </summary>
public class ResignEmployeeOption
{
    /// <summary>
    /// 离职日期 示例值："2024-06-21"
    /// </summary>
    [JsonPropertyName("resign_date")]
    public required string ResignDate { get; set; }

    /// <summary>
    /// 离职原因 可选值有：1：薪酬不符合预期 2：工作时间过长 3：不满意工作内容 4：不认可上级或管理层 5：职业发展机会有限
    /// </summary>
    [JsonPropertyName("resign_reason")]
    public required string ResignReason { get; set; }

    /// <summary>
    /// 离职类型 可选值有：1：主动 2：被动 3：其他 
    /// </summary>
    [JsonPropertyName("resign_type")]
    public required string ResignType { get; set; }

    /// <summary>
    /// 离职备注
    /// </summary>
    [JsonPropertyName("resign_remark")]
    public string? ResignRemark { get; set; }
}
