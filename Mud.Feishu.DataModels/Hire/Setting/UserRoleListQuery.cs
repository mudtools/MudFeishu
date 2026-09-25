// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.HttpUtils;
using System.Globalization;

namespace Mud.Feishu.DataModels.Hire;

/// <summary>
/// <para>获取用户角色列表查询参数（查询对象模式，见 AGENTS.md API-2）</para>
/// </summary>
public class UserRoleListQuery : IQueryParameter
{
    /// <summary>
    /// <para>每页数量，默认 10，最大 100</para>
    /// <para>必填：否</para>
    /// </summary>
    public int? PageSize { get; set; }

    /// <summary>
    /// <para>分页标记，首次请求不填，翻页时取上一次返回的 page_token</para>
    /// <para>必填：否</para>
    /// </summary>
    public string? PageToken { get; set; }

    /// <summary>
    /// <para>用户 ID，与入参 user_id_type 类型一致</para>
    /// <para>必填：否</para>
    /// </summary>
    public string? UserId { get; set; }

    /// <summary>
    /// <para>角色 ID，可通过获取角色列表接口获取</para>
    /// <para>必填：否</para>
    /// </summary>
    public string? RoleId { get; set; }

    /// <summary>
    /// <para>最早更新时间，毫秒时间戳</para>
    /// <para>必填：否</para>
    /// <para>示例值：1618500278663</para>
    /// </summary>
    public string? UpdateStartTime { get; set; }

    /// <summary>
    /// <para>最晚更新时间，毫秒时间戳</para>
    /// <para>必填：否</para>
    /// <para>示例值：1618500278663</para>
    /// </summary>
    public string? UpdateEndTime { get; set; }

    /// <summary>
    /// <para>用户 ID 类型：open_id/union_id/user_id，默认 open_id；取 user_id 时需 contact:user.employee_id:readonly 字段权限</para>
    /// <para>必填：否</para>
    /// </summary>
    public string? UserIdType { get; set; }

    /// <summary>
    /// 将对象转换为查询参数键值对集合（仅输出已设置的参数）。
    /// </summary>
    /// <returns>包含查询参数的键值对集合。</returns>
    public IEnumerable<KeyValuePair<string, string?>> ToQueryParameters()
    {
        if (PageSize.HasValue)
        {
            yield return new KeyValuePair<string, string?>("page_size", PageSize.Value.ToString(CultureInfo.InvariantCulture));
        }

        if (!string.IsNullOrEmpty(PageToken))
        {
            yield return new KeyValuePair<string, string?>("page_token", PageToken);
        }

        if (!string.IsNullOrEmpty(UserId))
        {
            yield return new KeyValuePair<string, string?>("user_id", UserId);
        }

        if (!string.IsNullOrEmpty(RoleId))
        {
            yield return new KeyValuePair<string, string?>("role_id", RoleId);
        }

        if (!string.IsNullOrEmpty(UpdateStartTime))
        {
            yield return new KeyValuePair<string, string?>("update_start_time", UpdateStartTime);
        }

        if (!string.IsNullOrEmpty(UpdateEndTime))
        {
            yield return new KeyValuePair<string, string?>("update_end_time", UpdateEndTime);
        }

        if (!string.IsNullOrEmpty(UserIdType))
        {
            yield return new KeyValuePair<string, string?>("user_id_type", UserIdType);
        }
    }
}
