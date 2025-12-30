// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.TaskCustomFields;

/// <summary>
/// 将自定义字段加入资源请求体
/// </summary>
public class CustomFieldsToResourceRequest
{
    /// <summary>
    /// <para>要将自定义字段添加到一个资源的资源类型。目前只支持tasklist</para>
    /// <para>必填：是</para>
    /// <para>示例值：tasklist</para>
    /// </summary>
    [JsonPropertyName("resource_type")]
    public string ResourceType { get; set; } = string.Empty;

    /// <summary>
    /// <para>要将自定义字段添加到的资源id，目前只支持tasklist_guid</para>
    /// <para>必填：是</para>
    /// <para>示例值：0110a4bd-f24b-4a93-8c1a-1732b94f9593</para>
    /// </summary>
    [JsonPropertyName("resource_id")]
    public string ResourceId { get; set; } = string.Empty;
}
