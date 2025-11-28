// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.ChatGroupNotice;


/// <summary>
/// <para>多维表格 Block</para>
/// </summary>
public class BlockBitable
{
    /// <summary>
    /// <para>多维表格文档 Token。格式为 {BitableToken}_{TableID}，其中 BitableToken 是一篇多维表格的唯一标识，TableID 是一张数据表的唯一标识，使用时请注意拆分。</para>
    /// <para>必填：否</para>
    /// <para>示例值：basbcqH9FfRn3sWCCBOtdNVpCsb_tblSAh8fEwhuMXQg</para>
    /// </summary>
    [JsonPropertyName("token")]
    public string? Token { get; set; }

    /// <summary>
    /// <para>类型</para>
    /// <para>必填：否</para>
    /// <para>示例值：1</para>
    /// <para>可选值：<list type="bullet">
    /// <item>1：数据表</item>
    /// <item>2：看板</item>
    /// </list></para>
    /// </summary>
    [JsonPropertyName("view_type")]
    public int? ViewType { get; set; }
}

