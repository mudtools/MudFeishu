// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.DataModels.CardElements;

namespace Mud.Feishu;

/// <summary>
/// 飞书卡片是应用的一种能力，包括构建卡片内容所需的组件和发送卡片所需的能力，并提供了可视化搭建工具。
/// <para>飞书开放平台针对飞书卡片提供了一系列 OpenAPI，使用这些 OpenAPI 你可以在卡片和组件维度，局部或流式更新卡片。</para>
/// <para>当前接口使用租户令牌访问，适应于租户应用场景。</para>
/// 接口详细文档请参见：<see href="https://open.feishu.cn/document/cardkit-v1/feishu-card-resource-overview"/>
/// </summary>
[HttpClientApi(RegistryGroupName = "Cards", TokenManage = nameof(ITenantTokenManager))]
[Header(Consts.Authorization)]
public interface IFeishuTenantV1CardElements
{
    /// <summary>
    /// 为指定卡片实体新增组件，以扩展卡片内容，如在卡片中添加一个点击按钮。
    /// </summary>
    /// <param name="card_id">卡片实体 ID。示例值："7355372766134157313"</param>
    /// <param name="cardElementRequest">新增卡片组件请求体</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns></returns>
    [Post("/open-apis/cardkit/v1/cards/{card_id}/elements")]
    Task<FeishuNullDataApiResult?> CreateCardElementAsync(
         [Path] string card_id,
         [Body] CreateCardElementRequest cardElementRequest,
         CancellationToken cancellationToken = default);


    /// <summary>
    /// 更新卡片实体中的指定组件为新组件。
    /// </summary>
    /// <param name="card_id">卡片实体 ID。示例值："7355372766134157313"</param>
    /// <param name="element_id">要更新的组件 ID。对应卡片 JSON 中组件的 element_id 属性，由开发者自定义。示例值："markdown_1"</param>
    /// <param name="cardElementRequest">更新卡片组件请求体</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns></returns>
    [Put("/open-apis/cardkit/v1/cards/{card_id}/elements/{element_id}")]
    Task<FeishuNullDataApiResult?> UpdateCardElementByIdAsync(
        [Path] string card_id,
        [Path] string element_id,
        [Body] UpdateCardElementRequest cardElementRequest,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 通过传入 card_id（卡片实体 ID）和 element_id（组件 ID），更新卡片实体中对应组件的属性。
    /// </summary>
    /// <param name="card_id">卡片实体 ID。示例值："7355372766134157313"</param>
    /// <param name="element_id">要更新的组件 ID。对应卡片 JSON 中组件的 element_id 属性，由开发者自定义。示例值："markdown_1"</param>
    /// <param name="cardElementRequest">更新卡片组件属性请求体</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    [Patch("/open-apis/cardkit/v1/cards/{card_id}/elements/{element_id}")]
    Task<FeishuNullDataApiResult?> UpdateCardElementAttributeByIdAsync(
       [Path] string card_id,
       [Path] string element_id,
       [Body] UpdateCardElementAttributeRequest cardElementRequest,
       CancellationToken cancellationToken = default);

    /// <summary>
    /// 对卡片中的普通文本元素（tag 为 plain_text 的元素）或富文本组件（tag 为 markdown 的组件）传入全量文本内容，以实现“打字机”式的文字输出效果。
    /// </summary>
    /// <param name="card_id">卡片实体 ID。示例值："7355372766134157313"</param>
    /// <param name="element_id">要更新的组件 ID。对应卡片 JSON 中组件的 element_id 属性，由开发者自定义。示例值："markdown_1"</param>
    /// <param name="cardElementRequest">流式更新文本请求体</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    [Put("/open-apis/cardkit/v1/cards/{card_id}/elements/{element_id}/content")]
    Task<FeishuNullDataApiResult?> StreamUpdateCardTextByIdAsync(
      [Path] string card_id,
      [Path] string element_id,
      [Body] StreamUpdateTextRequest cardElementRequest,
      CancellationToken cancellationToken = default);

    /// <summary>
    /// 删除指定卡片实体中的组件。
    /// </summary>
    /// <param name="card_id">卡片实体 ID。示例值："7355372766134157313"</param>
    /// <param name="element_id">要更新的组件 ID。对应卡片 JSON 中组件的 element_id 属性，由开发者自定义。示例值："markdown_1"</param>
    /// <param name="cardElementRequest">删除卡片组件请求体</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    [Delete("/open-apis/cardkit/v1/cards/{card_id}/elements/{element_id}")]
    Task<FeishuNullDataApiResult?> DeleteCardElementByIdAsync(
    [Path] string card_id,
    [Path] string element_id,
    [Body] DeleteCardElementRequest cardElementRequest,
    CancellationToken cancellationToken = default);
}
