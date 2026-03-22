// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Spreadsheets;

/// <summary>
/// 更新工作表属性请求体
/// </summary>
public class BatchUpdateSheetPropertiesRequest
{
    /// <summary>
    /// <para>更新工作表属性的请求。一次请求可以进行多次更新操作。</para>
    /// <para>必填：是</para>
    /// </summary>
    [JsonPropertyName("requests")]
    public UpdateRequestData[] Requests { get; set; } = [];
}

/// <summary></summary>
public class UpdateRequestData
{
    /// <summary>
    /// <para>更新工作表。</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("updateSheet")]
    public UpdateSheetData? UpdateSheet { get; set; }


}

/// <summary></summary>
public class UpdateSheetData
{
    /// <summary>
    /// <para>工作表属性</para>
    /// <para>必填：是</para>
    /// </summary>
    [JsonPropertyName("properties")]
    public UpdatePropertiesData Properties { get; set; } = new();

}

/// <summary></summary>
public class UpdatePropertiesData
{
    /// <summary>
    /// <para>要更新的工作表的 ID。</para>
    /// <para>必填：是</para>
    /// </summary>
    [JsonPropertyName("sheetId")]
    public string SheetId { get; set; } = string.Empty;

    /// <summary>
    /// <para>工作表的标题。更新的标题需符合以下规则：</para>
    /// <para>- 长度不超过 100 个字符</para>
    /// <para>- 不包含这些特殊字符：`/ \ ? * [ ] :`</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("title")]
    public string? Title { get; set; }

    /// <summary>
    /// <para>工作表的位置。从 0 开始计数。</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("index")]
    public int? Index { get; set; }

    /// <summary>
    /// <para>是否要隐藏表格。默认值为 false。</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("hidden")]
    public bool? Hidden { get; set; }

    /// <summary>
    /// <para>要冻结至指定行的行索引。若填 3，表示从第一行冻结至第三行。小于或等于工作表的最大行数，0 表示取消冻结行。</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("frozenRowCount")]
    public int? FrozenRowCount { get; set; }

    /// <summary>
    /// <para>要冻结至指定列的列索引。若填 3，表示从第一列冻结至第三列。小于等于工作表的最大列数，0 表示取消冻结列。</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("frozenColCount")]
    public int? FrozenColCount { get; set; }

    /// <summary>
    /// <para>是否要保护该工作表。</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("protect")]
    public UpdateProtectData? Protect { get; set; }


}

/// <summary></summary>
public class UpdateProtectData
{
    /// <summary>
    /// <para>是否要保护该工作表。可选值：</para>
    /// <para>- LOCK：保护</para>
    /// <para>- UNLOCK：取消保护</para>
    /// <para>必填：是</para>
    /// </summary>
    [JsonPropertyName("lock")]
    public string Lock { get; set; } = string.Empty;

    /// <summary>
    /// <para>保护工作表的备注信息</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("lockInfo")]
    public string? LockInfo { get; set; }

    /// <summary>
    /// <para>添加除操作用户与所有者外其他用户的 ID，为其开通保护范围的编辑权限。ID 类型由查询参数 `user_id_type` 决定。`user_id_type` 不为空时，该字段生效。</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("userIDs")]
    public string[]? UserIDs { get; set; }
}