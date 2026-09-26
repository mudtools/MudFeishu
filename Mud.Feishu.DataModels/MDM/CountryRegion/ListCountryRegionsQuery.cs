// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.HttpUtils;
using System.Globalization;

namespace Mud.Feishu.DataModels.MDM;

/// <summary>
/// <para>分页批量查询国家/地区查询参数（查询对象模式，见 AGENTS.md API-2）</para>
/// </summary>
public class ListCountryRegionsQuery : IQueryParameter
{
    /// <summary>
    /// <para>希望返回的语言种类（必填，0~100 个），支持格式：zh-CN（中文）/ en-US（英文）/ ja-JP（日文）；对于多语文本字段，传入特定语言将会返回对应语言文本</para>
    /// <para>必填：是</para>
    /// <para>示例值：["en-US"]</para>
    /// </summary>
    public string[]? Languages { get; set; }

    /// <summary>
    /// <para>需要的查询字段集（必填，1~100 个），可选字段见国家/地区资源定义（如 name、full_name、alpha_3_code 等）</para>
    /// <para>必填：是</para>
    /// <para>示例值：["name"]</para>
    /// </summary>
    public string[]? Fields { get; set; }

    /// <summary>
    /// <para>查询页大小，取值范围 1~1000</para>
    /// <para>必填：否</para>
    /// <para>示例值：10</para>
    /// </summary>
    public int? Limit { get; set; }

    /// <summary>
    /// <para>查询起始位置，取值范围 0~100000</para>
    /// <para>必填：否</para>
    /// <para>示例值：0</para>
    /// </summary>
    public int? Offset { get; set; }

    /// <summary>
    /// <para>是否返回总数</para>
    /// <para>必填：否</para>
    /// <para>示例值：true</para>
    /// </summary>
    public bool? ReturnCount { get; set; }

    /// <summary>
    /// <para>分页标记，第一次请求不填，表示从头开始遍历；分页查询结果还有更多项时会同时返回新的 page_token，下次遍历可采用该 page_token 获取查询结果</para>
    /// <para>必填：否</para>
    /// </summary>
    public string? PageToken { get; set; }

    /// <summary>
    /// 将对象转换为查询参数键值对集合（仅输出已设置的参数；数组参数逐元素展开为同名键）。
    /// </summary>
    /// <returns>包含查询参数的键值对集合。</returns>
    public IEnumerable<KeyValuePair<string, string?>> ToQueryParameters()
    {
        if (Languages is { Length: > 0 })
        {
            foreach (var language in Languages)
            {
                yield return new KeyValuePair<string, string?>("languages", language);
            }
        }

        if (Fields is { Length: > 0 })
        {
            foreach (var field in Fields)
            {
                yield return new KeyValuePair<string, string?>("fields", field);
            }
        }

        if (Limit.HasValue)
        {
            yield return new KeyValuePair<string, string?>("limit", Limit.Value.ToString(CultureInfo.InvariantCulture));
        }

        if (Offset.HasValue)
        {
            yield return new KeyValuePair<string, string?>("offset", Offset.Value.ToString(CultureInfo.InvariantCulture));
        }

        if (ReturnCount.HasValue)
        {
            yield return new KeyValuePair<string, string?>("return_count", ReturnCount.Value ? "true" : "false");
        }

        if (!string.IsNullOrEmpty(PageToken))
        {
            yield return new KeyValuePair<string, string?>("page_token", PageToken);
        }
    }
}
