// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.Abstractions.DataModels.Organization;

/// <summary>
/// 成员字段变更 事件体
/// <para>当成员字段发生变更时（变更动作包括「打开/关闭」开关、「增加/删除」成员字段），会触发该事件。事件体的 old_object 展示字段的原始值，object 展示字段的更新值。{使用示例}(url=/api/tools/api_explore/api_explore_config?project=contact&amp;version=v3&amp;resource=custom_attr_event&amp;event=updated)</para>
/// </summary>
public class CustomAttrUpdateResult : IEventResult
{
    /// <summary>
    /// <para>变更后信息</para>
    /// </summary>
    [JsonPropertyName("object")]
    public CustomAttrInfo? Object { get; set; }

    /// <summary>
    /// <para>变更前信息</para>
    /// </summary>
    [JsonPropertyName("old_object")]
    public CustomAttrInfo? OldObject { get; set; }
}
