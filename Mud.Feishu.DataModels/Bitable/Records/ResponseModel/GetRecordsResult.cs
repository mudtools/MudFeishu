// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Bitable;

/// <summary>
/// 批量获取记录响应体
/// </summary>
public class GetRecordsResult
{
    /// <summary>
    /// <para>记录列表</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("records")]
    public AppTableRecord[]? Records { get; set; }


    /// <summary>
    /// <para>禁止访问的记录列表（针对开启了高级权限的多维表格）</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("forbidden_record_ids")]
    public string[]? ForbiddenRecordIds { get; set; }

    /// <summary>
    /// <para>不存在的记录列表</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("absent_record_ids")]
    public string[]? AbsentRecordIds { get; set; }
}