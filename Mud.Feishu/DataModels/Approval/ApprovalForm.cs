// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Approval;

/// <summary>
/// <para>审批定义表单</para>
/// </summary>
public class ApprovalForm
{
    /// <summary>
    /// <para>审批定义表单。表单格式为 JSON 数组，实际传值时需要将 JSON 压缩转义为 String 类型。</para>
    /// <para>**注意**：以下示例值未转义，你可以参考下文**请求示例**章节的示例代码。</para>
    /// <para>必填：是</para>
    /// <para>示例值：[{\"id\":\"user_name\", \"type\": \"input\", \"required\":true, \"name\":\"@i18n@widget1\"}]</para>
    /// </summary>
    [JsonPropertyName("form_content")]
    public string FormContent { get; set; } = string.Empty;
}
