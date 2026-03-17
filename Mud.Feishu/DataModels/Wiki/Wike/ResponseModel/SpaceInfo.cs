// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Wiki;

/// <summary>
/// <para>数据列表</para>
/// </summary>
public class SpaceInfo
{
    /// <summary>
    /// <para>知识空间名称</para>
    /// <para>必填：否</para>
    /// <para>示例值：workspace name</para>
    /// </summary>
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    /// <summary>
    /// <para>知识空间描述</para>
    /// <para>必填：否</para>
    /// <para>示例值：workspace description</para>
    /// </summary>
    [JsonPropertyName("description")]
    public string? Description { get; set; }

    /// <summary>
    /// <para>知识空间 ID</para>
    /// <para>必填：否</para>
    /// <para>示例值：123456</para>
    /// </summary>
    [JsonPropertyName("space_id")]
    public string? SpaceId { get; set; }

    /// <summary>
    /// <para>表示知识空间类型</para>
    /// <para>必填：否</para>
    /// <para>示例值：team</para>
    /// <para>可选值：<list type="bullet">
    /// <item>team：团队空间，归团队（多人）管理，可添加多个管理员</item>
    /// <item>person：个人空间（旧版，已下线），归个人管理。一人仅可拥有一个，无法添加其他管理员</item>
    /// <item>my_library：我的文档库，归个人管理。一人仅可拥有一个，无法添加其他管理员</item>
    /// </list></para>
    /// </summary>
    [JsonPropertyName("space_type")]
    public string? SpaceType { get; set; }

    /// <summary>
    /// <para>表示知识空间可见性</para>
    /// <para>必填：否</para>
    /// <para>示例值：private</para>
    /// <para>可选值：<list type="bullet">
    /// <item>public：公开空间，租户内所有用户可见，默认为成员权限。无法额外添加成员，但可以添加管理员</item>
    /// <item>private：私有空间，仅对知识空间管理员、成员可见，需要手动添加管理员、成员</item>
    /// </list></para>
    /// </summary>
    [JsonPropertyName("visibility")]
    public string? Visibility { get; set; }

    /// <summary>
    /// <para>表示知识空间的分享状态</para>
    /// <para>必填：否</para>
    /// <para>示例值：open</para>
    /// <para>最大长度：99</para>
    /// <para>最小长度：0</para>
    /// <para>可选值：<list type="bullet">
    /// <item>open：打开，即知识空间发布到互联网</item>
    /// <item>closed：关闭，即知识空间未发布到互联网</item>
    /// </list></para>
    /// </summary>
    [JsonPropertyName("open_sharing")]
    public string? OpenSharing { get; set; }
}
