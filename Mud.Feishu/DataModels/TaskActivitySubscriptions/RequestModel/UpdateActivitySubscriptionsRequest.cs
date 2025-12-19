// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.TaskActivitySubscriptions;

/// <summary>
/// 更新动态订阅请求体
/// </summary>
public class UpdateActivitySubscriptionsRequest
{
    /// <summary>
    /// <para>要更新的订阅数据</para>
    /// <para>必填：是</para>
    /// </summary>
    [JsonPropertyName("activity_subscription")]
    public ActivitySubscriptionsData ActivitySubscription { get; set; } = new();

    /// <summary>
    /// <para>要更新的字段列表。</para>
    /// <para>必填：是</para>
    /// <para>最大长度：20</para>
    /// <para>最小长度：1</para>
    /// <para>可选值：<list type="bullet">
    /// <item>name：订阅名称</item>
    /// <item>include_keys：订阅的事件类型列表</item>
    /// <item>subscribers：订阅成员列表</item>
    /// <item>disabled：是否禁用该订阅</item>
    /// </list></para>
    /// </summary>
    [JsonPropertyName("update_fields")]
    public string[] UpdateFields { get; set; } = [];
}
