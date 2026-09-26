// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Hire;

/// <summary>
/// 账号自定义字段
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Hire")]
public class EcoAccountCustomFieldData
{
    /// <summary>
    /// <para>自定义字段的标识，在同一 scope 下唯一</para>
    /// <para>必填：是</para>
    /// <para>示例值：account_token</para>
    /// </summary>
    [JsonPropertyName("key")]
    public string? Key { get; set; }

    /// <summary>
    /// <para>自定义字段的名称，用户在「飞书招聘」-「设置」-「生态对接」-「笔试/背景调查」添加账号表单看到的控件标题</para>
    /// <para>必填：是</para>
    /// </summary>
    [JsonPropertyName("name")]
    public I18n? Name { get; set; }

    /// <summary>
    /// <para>是否必填。true：必填；false：非必填。注意：该字段在更新接口中暂不生效</para>
    /// <para>必填：是</para>
    /// <para>示例值：true</para>
    /// </summary>
    [JsonPropertyName("is_required")]
    public bool? IsRequired { get; set; }

    /// <summary>
    /// <para>自定义字段的描述，用户在添加账号表单看到的 placeholder</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("description")]
    public I18n? Description { get; set; }
}
