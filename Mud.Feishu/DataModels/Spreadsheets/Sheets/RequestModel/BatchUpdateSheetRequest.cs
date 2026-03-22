// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Spreadsheets;

/// <summary>
/// 操作工作表请求体
/// </summary>
public class BatchUpdateSheetRequest
{
    /// <summary>
    /// <para>支持增加、复制、和删除工作表。一次请求可以同时进行多个操作。</para>
    /// <para>必填：是</para>
    /// </summary>
    [JsonPropertyName("requests")]
    public SheetRequest[] Requests { get; set; } = [];
}

/// <summary></summary>
public class SheetRequest
{
    /// <summary>
    /// <para>增加工作表。</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("addSheet")]
    public AddsheetSuffix? AddSheet { get; set; }

    /// <summary>
    /// <para>复制工作表。复制的新工作表位于源工作表索引位置之后。</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("copySheet")]
    public CopysheetSuffix? CopySheet { get; set; }

    /// <summary>
    /// <para>删除工作表。</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("deleteSheet")]
    public SheetInfo? DeleteSheet { get; set; }
}

/// <summary></summary>
public class AddsheetSuffix
{
    /// <summary>
    /// <para>工作表属性</para>
    /// <para>必填：是</para>
    /// </summary>
    [JsonPropertyName("properties")]
    public SheetPropertyData Properties { get; set; } = new();
}

/// <summary></summary>
public class CopysheetSuffix
{
    /// <summary>
    /// <para>需要复制的工作表资源</para>
    /// <para>必填：是</para>
    /// </summary>
    [JsonPropertyName("source")]
    public SheetInfo Source { get; set; } = new();

    /// <summary>
    /// <para>新工作表的属性</para>
    /// <para>必填：是</para>
    /// </summary>
    [JsonPropertyName("destination")]
    public DestinationSheetProperty Destination { get; set; } = new();
}

/// <summary></summary>
public class DestinationSheetProperty
{
    /// <summary>
    /// <para>新工作表名称。不填默认为“源工作表名称”+“(副本_源工作表的 `index` 值)”，如 “Sheet1(副本_0)”。</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("title")]
    public string? Title { get; set; }
}

/// <summary></summary>
public class SheetInfo
{
    /// <summary>
    /// <para>要删除的工作表的 ID。调用[获取工作表](https://open.feishu.cn/document/ukTMukTMukTM/uUDN04SN0QjL1QDN/sheets-v3/spreadsheet-sheet/query)获取 ID</para>
    /// <para>必填：是</para>
    /// </summary>
    [JsonPropertyName("sheetId")]
    public string SheetId { get; set; } = string.Empty;
}

/// <summary></summary>
public class SheetPropertyData
{
    /// <summary>
    /// <para>新增工作表的标题</para>
    /// <para>必填：是</para>
    /// </summary>
    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// <para>新增工作表的位置。不填默认在工作表的第 0 索引位置增加工作表。</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("index")]
    public int? Index { get; set; }
}