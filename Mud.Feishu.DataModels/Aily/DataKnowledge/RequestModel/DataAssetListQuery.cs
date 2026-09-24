// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.HttpUtils;
using System.Globalization;

namespace Mud.Feishu.DataModels.Aily;

/// <summary>
/// <para>获取数据知识列表查询参数（查询对象模式，见 AGENTS.md API-2）</para>
/// </summary>
public class DataAssetListQuery : IQueryParameter
{
    /// <summary>
    /// <para>分页参数：分页大小，默认 20，最大 100</para>
    /// <para>必填：否</para>
    /// <para>示例值：10</para>
    /// </summary>
    public int? PageSize { get; set; }

    /// <summary>
    /// <para>分页标记，第一次请求不填，表示从头开始遍历；分页查询结果还有更多项时会同时返回新的 page_token，下次遍历可采用该 page_token 获取查询结果</para>
    /// <para>必填：否</para>
    /// </summary>
    public string? PageToken { get; set; }

    /// <summary>
    /// <para>模糊匹配关键词</para>
    /// <para>必填：否</para>
    /// <para>示例值：电影</para>
    /// <para>最大长度：255</para>
    /// </summary>
    public string? Keyword { get; set; }

    /// <summary>
    /// <para>根据数据知识 ID 进行过滤</para>
    /// <para>必填：否</para>
    /// <para>示例值：asset_aadg2b5os5wjg</para>
    /// </summary>
    public string[]? DataAssetIds { get; set; }

    /// <summary>
    /// <para>根据数据知识分类 ID 进行过滤</para>
    /// <para>必填：否</para>
    /// <para>示例值：spring_5862e4fea8__c__asset_tag_aadg2b5ql4gbs</para>
    /// </summary>
    public string[]? DataAssetTagIds { get; set; }

    /// <summary>
    /// <para>结果是否包含数据与知识项</para>
    /// <para>必填：否</para>
    /// <para>示例值：true</para>
    /// </summary>
    public bool? WithDataAssetItem { get; set; }

    /// <summary>
    /// <para>结果是否包含数据连接状态</para>
    /// <para>必填：否</para>
    /// <para>示例值：false</para>
    /// </summary>
    public bool? WithConnectStatus { get; set; }

    /// <inheritdoc />
    public IEnumerable<KeyValuePair<string, string?>> ToQueryParameters()
    {
        if (PageSize is int pageSize)
        {
            yield return new KeyValuePair<string, string?>(
                "page_size",
                pageSize.ToString(CultureInfo.InvariantCulture));
        }

        if (!string.IsNullOrEmpty(PageToken))
        {
            yield return new KeyValuePair<string, string?>("page_token", PageToken);
        }

        if (!string.IsNullOrEmpty(Keyword))
        {
            yield return new KeyValuePair<string, string?>("keyword", Keyword);
        }

        if (DataAssetIds is { Length: > 0 })
        {
            yield return new KeyValuePair<string, string?>(
                "data_asset_ids",
                string.Join(",", DataAssetIds));
        }

        if (DataAssetTagIds is { Length: > 0 })
        {
            yield return new KeyValuePair<string, string?>(
                "data_asset_tag_ids",
                string.Join(",", DataAssetTagIds));
        }

        if (WithDataAssetItem is bool withItem)
        {
            yield return new KeyValuePair<string, string?>(
                "with_data_asset_item",
                withItem ? "true" : "false");
        }

        if (WithConnectStatus is bool withConnect)
        {
            yield return new KeyValuePair<string, string?>(
                "with_connect_status",
                withConnect ? "true" : "false");
        }
    }
}
