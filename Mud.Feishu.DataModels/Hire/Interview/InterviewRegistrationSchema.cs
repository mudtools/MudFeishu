// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Hire;

/// <summary>
/// 面试登记表模板（获取面试登记表模板列表响应子项）
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Hire")]
public class InterviewRegistrationSchema
{
    /// <summary>
    /// <para>面试登记表模板 ID</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    /// <summary>
    /// <para>面试登记表模板名称</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    /// <summary>
    /// <para>是否用作全局面试登记表：true 全局登记表，全部职位都应用该登记表；false 非全局登记表，可按职位选择</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("is_used_as_interview")]
    public bool? IsUsedAsInterview { get; set; }

    /// <summary>
    /// <para>模块列表</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("object_list")]
    public CommonSchema[]? ObjectList { get; set; }
}
