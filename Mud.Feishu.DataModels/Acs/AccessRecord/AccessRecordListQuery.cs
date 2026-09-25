// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.HttpUtils;
using System.Globalization;

namespace Mud.Feishu.DataModels.Acs;

/// <summary>
/// <para>获取门禁记录列表查询参数（查询对象模式，见 AGENTS.md API-2）</para>
/// </summary>
public class AccessRecordListQuery : IQueryParameter
{
    /// <summary>
    /// <para>分页大小，默认 100，最大 500</para>
    /// <para>必填：否</para>
    /// <para>示例值：100</para>
    /// </summary>
    public int? PageSize { get; set; }

    /// <summary>
    /// <para>分页标记，第一次请求不填，表示从头开始遍历；分页查询结果还有更多项时会同时返回新的 page_token，下次遍历可采用该 page_token 获取查询结果</para>
    /// <para>必填：否</para>
    /// </summary>
    public string? PageToken { get; set; }

    /// <summary>
    /// <para>门禁设备 ID</para>
    /// <para>必填：否</para>
    /// <para>示例值：7091146989218002577</para>
    /// </summary>
    public string? DeviceId { get; set; }

    /// <summary>
    /// <para>此次调用中使用的用户 ID 的类型：open_id/union_id/user_id</para>
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

        if (!string.IsNullOrEmpty(DeviceId))
        {
            yield return new KeyValuePair<string, string?>("device_id", DeviceId);
        }

        if (!string.IsNullOrEmpty(UserIdType))
        {
            yield return new KeyValuePair<string, string?>("user_id_type", UserIdType);
        }
    }
}
