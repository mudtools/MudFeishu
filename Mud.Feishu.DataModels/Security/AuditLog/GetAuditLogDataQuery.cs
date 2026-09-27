// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.HttpUtils;
using System.Globalization;

namespace Mud.Feishu.DataModels.Security;

/// <summary>
/// <para>获取行为审计日志数据查询参数（查询对象模式，见 AGENTS.md API-2）</para>
/// </summary>
public class GetAuditLogDataQuery : IQueryParameter
{
    /// <summary>
    /// <para>用户 ID 类型：open_id/union_id/user_id，默认 user_id；取 user_id 时需字段权限 contact:user.employee_id:readonly</para>
    /// <para>必填：否</para>
    /// <para>示例值：user_id</para>
    /// </summary>
    public string? UserIdType { get; set; }

    /// <summary>
    /// <para>日志时间范围：结束时间，秒级时间戳，默认此刻；起止日期之间相差不能超过 30 天</para>
    /// <para>必填：否</para>
    /// <para>示例值：1668700799</para>
    /// </summary>
    public int? Latest { get; set; }

    /// <summary>
    /// <para>日志时间范围：起始时间，秒级时间戳，默认 30 日前此刻；起止日期之间相差不能超过 30 天</para>
    /// <para>必填：否</para>
    /// <para>示例值：1668528000</para>
    /// </summary>
    public int? Oldest { get; set; }

    /// <summary>
    /// <para>行为审计的事件名称，可选事件名称见飞书枚举值列表附录</para>
    /// <para>必填：否</para>
    /// <para>示例值：space_create_doc</para>
    /// </summary>
    public string? EventName { get; set; }

    /// <summary>
    /// <para>过滤操作者：操作者类型，与 <see cref="OperatorValue"/> 配合使用，填写 OperatorValue 时此项必填。可选值：user（用户）/ bot（当前未开放，以 bot_id 来识别用户）</para>
    /// <para>必填：否</para>
    /// <para>示例值：user</para>
    /// </summary>
    public string? OperatorType { get; set; }

    /// <summary>
    /// <para>操作者值</para>
    /// <para>必填：否</para>
    /// <para>示例值：55ed16fe</para>
    /// </summary>
    public string? OperatorValue { get; set; }

    /// <summary>
    /// <para>行为审计的事件模块，可选事件模块见飞书枚举值列表附录</para>
    /// <para>必填：否</para>
    /// <para>示例值：1</para>
    /// </summary>
    public int? EventModule { get; set; }

    /// <summary>
    /// <para>分页标记，第一次请求不填，表示从头开始遍历；分页查询结果还有更多项时会同时返回新的 page_token，下次遍历可采用该 page_token 获取查询结果</para>
    /// <para>必填：否</para>
    /// </summary>
    public string? PageToken { get; set; }

    /// <summary>
    /// <para>分页大小，默认 20，取值范围 1~200</para>
    /// <para>必填：否</para>
    /// <para>示例值：20</para>
    /// </summary>
    public int? PageSize { get; set; }

    /// <summary>
    /// <para>用户类型，此选项为空时默认查询「组织内成员」；填写时 <see cref="OperatorType"/> 必须为 user。可选值：0（互联网上的任何人）/ 1（组织内成员）/ 2（组织外成员）</para>
    /// <para>必填：否</para>
    /// <para>示例值：1</para>
    /// </summary>
    public int? UserType { get; set; }

    /// <summary>
    /// <para>过滤操作对象：操作对象类型，与 <see cref="ObjectValue"/> 配合使用</para>
    /// <para>必填：否</para>
    /// <para>示例值：1</para>
    /// </summary>
    public int? ObjectType { get; set; }

    /// <summary>
    /// <para>过滤操作对象：操作对象 ID，与 <see cref="ObjectType"/> 配合使用</para>
    /// <para>必填：否</para>
    /// <para>示例值：55ed16fe</para>
    /// </summary>
    public string? ObjectValue { get; set; }

    /// <summary>
    /// 将对象转换为查询参数键值对集合（仅输出已设置的参数）。
    /// </summary>
    /// <returns>包含查询参数的键值对集合。</returns>
    public IEnumerable<KeyValuePair<string, string?>> ToQueryParameters()
    {
        if (!string.IsNullOrEmpty(UserIdType))
        {
            yield return new KeyValuePair<string, string?>("user_id_type", UserIdType);
        }

        if (Latest.HasValue)
        {
            yield return new KeyValuePair<string, string?>("latest", Latest.Value.ToString(CultureInfo.InvariantCulture));
        }

        if (Oldest.HasValue)
        {
            yield return new KeyValuePair<string, string?>("oldest", Oldest.Value.ToString(CultureInfo.InvariantCulture));
        }

        if (!string.IsNullOrEmpty(EventName))
        {
            yield return new KeyValuePair<string, string?>("event_name", EventName);
        }

        if (!string.IsNullOrEmpty(OperatorType))
        {
            yield return new KeyValuePair<string, string?>("operator_type", OperatorType);
        }

        if (!string.IsNullOrEmpty(OperatorValue))
        {
            yield return new KeyValuePair<string, string?>("operator_value", OperatorValue);
        }

        if (EventModule.HasValue)
        {
            yield return new KeyValuePair<string, string?>("event_module", EventModule.Value.ToString(CultureInfo.InvariantCulture));
        }

        if (!string.IsNullOrEmpty(PageToken))
        {
            yield return new KeyValuePair<string, string?>("page_token", PageToken);
        }

        if (PageSize.HasValue)
        {
            yield return new KeyValuePair<string, string?>("page_size", PageSize.Value.ToString(CultureInfo.InvariantCulture));
        }

        if (UserType.HasValue)
        {
            yield return new KeyValuePair<string, string?>("user_type", UserType.Value.ToString(CultureInfo.InvariantCulture));
        }

        if (ObjectType.HasValue)
        {
            yield return new KeyValuePair<string, string?>("object_type", ObjectType.Value.ToString(CultureInfo.InvariantCulture));
        }

        if (!string.IsNullOrEmpty(ObjectValue))
        {
            yield return new KeyValuePair<string, string?>("object_value", ObjectValue);
        }
    }
}
