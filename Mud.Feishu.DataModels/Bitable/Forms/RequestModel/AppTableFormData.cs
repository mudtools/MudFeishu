// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Bitable;

/// <summary>
/// 应用表格表单数据
/// </summary>
public class AppTableFormData
{
    /// <summary>
    /// <para>表单名称</para>
    /// <para>必填：否</para>
    /// <para>示例值：文档问题反馈</para>
    /// </summary>
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    /// <summary>
    /// <para>表单描述</para>
    /// <para>必填：否</para>
    /// <para>示例值：请详细描述开发中遇到的问题，并附上问题截图</para>
    /// </summary>
    [JsonPropertyName("description")]
    public string? Description { get; set; }

    /// <summary>
    /// <para>是否开启表单分享，使表单支持填写。可选值：</para>
    /// <para>- true：支持填写</para>
    /// <para>- false：不支持填写</para>
    /// <para>必填：否</para>
    /// <para>示例值：true</para>
    /// </summary>
    [JsonPropertyName("shared")]
    public bool? Shared { get; set; }

    /// <summary>
    /// <para>分享表单范围限制。当 shared 参数为 true 时支持传入该字段</para>
    /// <para>必填：否</para>
    /// <para>示例值：tenant_editable</para>
    /// <para>可选值：<list type="bullet">
    /// <item>off：仅邀请的人可填写</item>
    /// <item>tenant_editable：组织内获得链接的人可填写</item>
    /// <item>anyone_editable：互联网上获得链接的人可填写</item>
    /// </list></para>
    /// </summary>
    [JsonPropertyName("shared_limit")]
    public string? SharedLimit { get; set; }

    /// <summary>
    /// <para>是否将填写次数限制为一次。可选值：</para>
    /// <para>- true：设置表单仅支持填写一次</para>
    /// <para>- false：不限制表单填写次数</para>
    /// <para>必填：否</para>
    /// <para>示例值：true</para>
    /// </summary>
    [JsonPropertyName("submit_limit_once")]
    public bool? SubmitLimitOnce { get; set; }
}