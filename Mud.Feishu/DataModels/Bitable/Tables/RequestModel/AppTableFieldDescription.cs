// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Bitable;

/// <summary>
/// <para>字段的描述</para>
/// </summary>
public class AppTableFieldDescription
{
    /// <summary>
    /// <para>是否禁止同步，如果为true，表示禁止同步该描述内容到表单的问题描述</para>
    /// <para>必填：否</para>
    /// <para>示例值：true</para>
    /// <para>默认值：true</para>
    /// </summary>
    [JsonPropertyName("disable_sync")]
    public bool? DisableSync { get; set; }

    /// <summary>
    /// <para>字段描述内容，支持换行\n</para>
    /// <para>必填：否</para>
    /// <para>示例值：请按 name_id 格式填写\n例如：“Alice_20202020”</para>
    /// </summary>
    [JsonPropertyName("text")]
    public string? Text { get; set; }
}