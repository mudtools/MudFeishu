// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Task;

/// <summary>
/// <para>如果希望设置任务来源为文档，则设置此字段</para>
/// <para>- 和extra互斥，同时设置时报错</para>
/// <para>- 和origin互斥，同时设置时报错</para>
/// <para>- 和custom_complete互斥，同时设置时报错</para>
/// </summary>
public class TaskDocxSource
{
    /// <summary>
    /// <para>任务关联的文档token，要求：如果使用tenant_access_token请求，则请求机器人有文档编辑权限；如果使用user_access_token，则请求用户有文档的编辑权限</para>
    /// <para>必填：是</para>
    /// <para>示例值：OvZCdFYVHo5ArFxJKHjcnRbtnKd</para>
    /// <para>最大长度：27</para>
    /// <para>最小长度：27</para>
    /// </summary>
    [JsonPropertyName("token")]
    public string Token { get; set; } = string.Empty;

    /// <summary>
    /// <para>任务关联的文档block_id，要求block_id存在于token对应文档中、且block_id没有绑定过其他的任务</para>
    /// <para>必填：是</para>
    /// <para>示例值：O6wwd22uIoG8acxwxGtbljaUcfc</para>
    /// <para>最大长度：27</para>
    /// <para>最小长度：27</para>
    /// </summary>
    [JsonPropertyName("block_id")]
    public string BlockId { get; set; } = string.Empty;
}

