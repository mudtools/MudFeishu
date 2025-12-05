// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.Abstractions.DataModels.Organization;

/// <summary>
/// 员工信息被修改事件体
/// <para>应用订阅该事件后，当员工信息（包括：ID、用户名、英文名、别名、邮箱、企业邮箱、职务、手机号、性别、头像、状态、所属部门、直属主管、城市、国家、工位、入职时间、工号、类型、排序、自定义字段、职级、序列、虚线上级）被修改时将会触发该事件。你可以在事件的 old_object 字段中查看修改前的用户信息；在事件的 object 字段中可以查看修改后的用户信息。{使用示例}(url=/api/tools/api_explore/api_explore_config?project=contact&amp;version=v3&amp;resource=user&amp;event=updated)</para>
/// </summary>
public class UserUpdateResult : IEventResult
{
    /// <summary>
    /// <para>变更后的用户信息。</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("object")]
    public UserResultInfo? Object { get; set; }

    /// <summary>
    /// <para>变更前的用户信息，只包含有变更的字段数据。</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("old_object")]
    public UserResultInfo? OldObject { get; set; }

}
