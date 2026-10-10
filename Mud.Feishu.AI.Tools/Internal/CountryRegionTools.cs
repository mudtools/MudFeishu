// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.DataModels.MDM;

namespace Mud.Feishu.AI.Tools.Internal;

/// <summary>
/// MDM「国家/地区」只读工具执行器（F-1 首个补域切片：<c>mdm.get_countries</c>）。
/// </summary>
/// <remarks>
/// <para>
/// <b>软依赖</b>：<c>IFeishuTenantV3MDMCountryRegion</c> 为可空注入——宿主未启用 MDM API 时本执行器
/// 软缺席（该域工具不在注册表），与 <c>MinutesReadTools</c> 同口径（S-13 教训：硬依赖会让整域静默消失，
/// 而"绑定生成"与"注册器装配"是两段，后者失败被软缺席机制吞掉）。
/// </para>
/// <para>
/// <b>投影纪律</b>：只回填模型判断所需的标识与事实（编码 / 名称 / ISO 代码 / 区号 / 大洲 / 状态），
/// 不回传原始 DTO；多语言文本取 <c>value</c>（由调用方给出的 <c>languages</c> 排序决定），
/// 不铺开 <c>multilingual_value</c>（多语言全量是上下文开销，模型已知道要哪几种语言）。
/// </para>
/// <para>
/// <b>参数前置校验</b>：ids / fields / languages 的长度上限（100）在本地拒绝，不消耗下游调用，
/// 也不让模型等一个必然失败的请求（F-8 纪律）。
/// </para>
/// </remarks>
internal sealed class CountryRegionTools(
    IOptions<FeishuAgentOptions> options,
    Mud.Feishu.IFeishuTenantV3MDMCountryRegion? countryRegionClient = null)
{
    /// <summary>批量查询的编码/字段/语言上限（对齐平台事实：1~100）。</summary>
    private const int MaxBatchSize = 100;

    private readonly Mud.Feishu.IFeishuTenantV3MDMCountryRegion? _countryRegionClient = countryRegionClient;

    /// <summary>
    /// 结果预算（R-4 单一取值口）：必须显式取 <see cref="FeishuAgentOptions.MaxToolResultLength"/>，
    /// 否则「无预算 + 截断出口」的组合会被 <c>ToolExecutorSkeletonGuards</c> 判为绕过预算的隐患。
    /// </summary>
    private readonly int _maxResultLength = ToolExecutor.Require(options).MaxToolResultLength;

    /// <summary>mdm.get_countries：按 mdmcode 批量查国家/地区主数据。</summary>
    [FeishuToolHandler(typeof(IFeishuTenantMdmGetCountriesTool))]
    public Task<FeishuToolResult> GetCountriesAsync(IReadOnlyDictionary<string, object?> arguments, CancellationToken cancellationToken)
    {
        var executor = new ToolExecutor(FeishuToolNames.MdmGetCountries, _maxResultLength);
        var client = _countryRegionClient;
        if (client is null)
        {
            // 软缺席（与 BoardTools / MailTools / DocxSheetsDriveWriteTools 同口径）：
            // 这是**可执行的错误结果**而不是异常——异常会逃出执行链抛给宿主，而这里要的是
            // "模型看到一条明确的不可用说明"，且不影响其它域工具。
            return Task.FromResult(FeishuToolResult.FromError(FeishuToolBinding.StructuredError(
                executor.ToolName,
                "未注册 IFeishuTenantV3MDMCountryRegion 客户端——MDM 国家/地区能力需宿主启用该 API（软缺席，不影响其它域工具）")));
        }

        return executor.RunAsync(async () =>
        {
            var args = MdmGetCountriesArgs.Unpack(arguments);

            ValidateBatch("ids", args.Ids.Length);
            ValidateBatch("fields", args.Fields.Length);
            ValidateBatch("languages", args.Languages.Length);

            var outcome = FeishuApiResultReader.Read(await client
                .GetBatchCountryRegionAsync(args.Fields, args.Ids, args.Languages, cancellationToken)
                .ConfigureAwait(false));

            return executor.FromApi(outcome, Project);
        });
    }

    /// <summary>本地拒绝越界批量（1~100）——错误在调用前给出，模型可立即改参数重试。</summary>
    private static void ValidateBatch(string parameter, int count)
    {
        if (count is < 1 or > MaxBatchSize)
        {
            throw new ArgumentException(
                $"{parameter} 数量须在 1~{MaxBatchSize.ToString(System.Globalization.CultureInfo.InvariantCulture)} 之间，实际 {count.ToString(System.Globalization.CultureInfo.InvariantCulture)} 个");
        }
    }

    /// <summary>白名单投影：编码 / 名称 / ISO 代码 / 区号 / 大洲 / 状态；空结果给显式"未找到"哨兵。</summary>
    private static JsonObject Project(GetBatchCountryRegionResult result)
    {
        var items = new JsonArray();
        foreach (var item in result.Data ?? [])
        {
            // AddNode（而非 Add）：见 ToolResultText.AddNode——泛型 Add<T>(T) 的裁剪/AOT 注解会红。
            items.AddNode(new JsonObject
            {
                ["mdm_code"] = item.MdmCode,
                ["name"] = item.Name?.Value,
                ["full_name"] = item.FullName?.Value,
                ["alpha_2_code"] = item.Alpha2Code,
                ["alpha_3_code"] = item.Alpha3Code,
                ["numeric_code"] = item.NumericCode,
                ["global_code"] = item.GlobalCode,
                ["continents"] = item.Continents?.Value,
                ["status"] = item.Status,
            });
        }

        var payload = new JsonObject
        {
            ["total"] = items.Count,
            ["items"] = items,
        };

        if (items.Count == 0)
        {
            // 空结果哨兵（F-5）：显式"没找到"，而不是返回空数组让模型猜。
            payload["found"] = false;
            payload["message"] = "未查询到任何国家/地区主数据，请确认 mdm_code 是否为 MDCT + 8 位数字格式，"
                + "且 fields / languages 为平台支持的取值（zh-CN / en-US / ja-JP）。";
        }

        return payload;
    }
}
