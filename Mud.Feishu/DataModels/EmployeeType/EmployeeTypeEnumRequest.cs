// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.EmployeeType;

/// <summary>
/// 新建的人员类型信息请求体。
/// </summary>
public class EmployeeTypeEnumRequest
{
    /// <summary>
    /// 人员类型的选项内容。
    /// </summary>
    [JsonPropertyName("content")]
    public
#if NET7_0_OR_GREATER
        required
#endif
  string? Content
    { get; set; }

    /// <summary>
    /// 人员类型的选项类型。新增人员类型时固定取值为 2 即可。
    /// <para>可选值有：1：内置类型，只读。新增人员类型时不支持选择该类型。2：自定义。</para>
    /// </summary>
    [JsonPropertyName("enum_type")]
    public
#if NET7_0_OR_GREATER
        required
#endif
  int EnumType
    { get; set; }

    /// <summary>
    /// 人员类型的选项激活状态。只有已激活的选项可以用于配置用户属性。
    /// <para>可选值有：1：激活 2：未激活</para>
    /// </summary>
    [JsonPropertyName("enum_status")]
    public int EnumStatus { get; set; }

    /// <summary>
    /// 选项内容的国际化配置。
    /// </summary>
    [JsonPropertyName("i18n_content")]
    public List<I18nContent>? I18nContent { get; set; }
}