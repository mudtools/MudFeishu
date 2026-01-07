// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.ApprovalExternal;

/// <summary>
/// <para>用户提交审批时填写的表单数据，用于所有审批列表中展示。可传多个值，最多展示前 3 个，长度不超过 2048 字符。</para>
/// </summary>
public class ExternalInstanceForm
{
    /// <summary>
    /// <para>表单字段名称。</para>
    /// <para>**说明**：</para>
    /// <para>- 这里传入的是国际化文案 Key（即 i18n_resources.texts 参数中的 Key），还需要在 i18n_resources.texts 参数中以 Key:Value 格式进行赋值。</para>
    /// <para>- Key 需要以 @i18n@ 开头。</para>
    /// <para>必填：否</para>
    /// <para>示例值：@i18n@2</para>
    /// </summary>
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    /// <summary>
    /// <para>表单值。</para>
    /// <para>**说明**：</para>
    /// <para>- 这里传入的是国际化文案 Key（即 i18n_resources.texts 参数中的 Key），还需要在 i18n_resources.texts 参数中以 Key:Value 格式进行赋值。</para>
    /// <para>- Key 需要以 @i18n@ 开头。</para>
    /// <para>必填：否</para>
    /// <para>示例值：@i18n@3</para>
    /// </summary>
    [JsonPropertyName("value")]
    public string? Value { get; set; }
}