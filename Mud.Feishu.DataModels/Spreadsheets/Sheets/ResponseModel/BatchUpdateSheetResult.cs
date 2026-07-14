// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Spreadsheets;

/// <summary>
///  操作工作表响应体
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Spreadsheets")]
public class BatchUpdateSheetResult
{
    /// <summary>
    /// <para>返回本次相关操作工作表的结果</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("replies")]
    public BatchUpdateReply[]? Replies { get; set; }

}

/// <summary></summary>
[HttpJsonSerializable(SerializerClassName = "Spreadsheets")]
public class BatchUpdateReply
{
    /// <summary>
    /// <para>增加工作表的结果</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("addSheet")]
    public SheetResultInfo? AddSheet { get; set; }

    /// <summary>
    /// <para>复制工作表的结果</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("copySheet")]
    public SheetResultInfo? CopySheet { get; set; }


    /// <summary>
    /// <para>删除工作表的结果</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("deleteSheet")]
    public DeleteSheetSuffix? DeleteSheet { get; set; }
}

/// <summary></summary>
[HttpJsonSerializable(SerializerClassName = "Spreadsheets")]
public class SheetResultInfo
{
    /// <summary>
    /// <para>工作表的属性</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("properties")]
    public SheetPropertyInfo? Properties { get; set; }


}

/// <summary></summary>
[HttpJsonSerializable(SerializerClassName = "Spreadsheets")]
public class DeleteSheetSuffix
{
    /// <summary>
    /// <para>删除工作表是否成功</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("result")]
    public bool? Result { get; set; }

    /// <summary>
    /// <para>被删除的工作表的 ID</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("sheetId")]
    public string? SheetId { get; set; }
}

/// <summary></summary>
[HttpJsonSerializable(SerializerClassName = "Spreadsheets")]
public class SheetPropertyInfo : SheetPropertyData
{
    /// <summary>
    /// <para>工作表的 `sheetId`</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("sheetId")]
    public string? SheetId { get; set; }
}
