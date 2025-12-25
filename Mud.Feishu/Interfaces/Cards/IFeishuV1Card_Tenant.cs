// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.DataModels.Cards;

namespace Mud.Feishu;

/// <summary>
/// 飞书卡片是应用的一种能力，包括构建卡片内容所需的组件和发送卡片所需的能力，并提供了可视化搭建工具。
/// <para>飞书开放平台针对飞书卡片提供了一系列 OpenAPI，使用这些 OpenAPI 你可以在卡片和组件维度，局部或流式更新卡片。</para>
/// <para>当前接口使用租户令牌访问，适应于租户应用场景。</para>
/// 接口详细文档请参见：<see href="https://open.feishu.cn/document/cardkit-v1/feishu-card-resource-overview"/>
/// </summary>
[HttpClientApi(RegistryGroupName = "Cards", TokenManage = nameof(ITenantTokenManager))]
[Header("Authorization")]
public interface IFeishuTenantV1Card
{
    /// <summary>
    /// 基于卡片 JSON 代码或卡片搭建工具搭建的卡片，创建卡片实体。用于后续通过卡片实体 ID（card_id）发送卡片、更新卡片等。
    /// </summary>
    /// <param name="createCardRequest">创建卡片实体请求体</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns></returns>
    [Post("/open-apis/cardkit/v1/cards")]
    Task<FeishuApiResult<CreateCardResult>?> CreateCardAsync(
         [Body] CreateCardRequest createCardRequest,
         CancellationToken cancellationToken = default);

    /// <summary>
    /// 更新指定卡片实体的配置，支持卡片配置 config 字段和卡片跳转链接 card_link 字段。
    /// </summary>
    /// <param name="updateCardRequest">更新卡片实体配置请求体</param>
    /// <param name="card_id">卡片实体 ID。示例值："7355372766134157313"</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns></returns>
    [Patch("/open-apis/cardkit/v1/cards/{card_id}/settings")]
    Task<FeishuNullDataApiResult?> UpdateCardSettingsByIdAsync(
        [Path] string card_id,
        [Body] UpdateCardSettingsRequest updateCardRequest,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 更新卡片实体局部内容，包括配置和组件。支持同时对多个组件进行增删改等不同操作。
    /// </summary>
    /// <param name="partialUpdateCardRequest">局部更新卡片实体配置请求体</param>
    /// <param name="card_id">卡片实体 ID。示例值："7355372766134157313"</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns></returns>
    [Post("/open-apis/cardkit/v1/cards/{card_id}/batch_update")]
    Task<FeishuNullDataApiResult?> PartialUpdateCardByIdAsync(
        [Path] string card_id,
        [Body] PartialUpdateCardRequest partialUpdateCardRequest,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 传入新的卡片 JSON 代码，覆盖更新指定的卡片实体的所有内容。
    /// </summary>
    /// <param name="card_id">卡片实体 ID。示例值："7355372766134157313"</param>
    /// <param name="updateCardRequest">全量更新卡片实体请求体</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns></returns>
    [Patch("/open-apis/cardkit/v1/cards/{card_id}")]
    Task<FeishuNullDataApiResult?> UpdateCardByIdAsync(
       [Path] string card_id,
       [Body] UpdateCardRequest updateCardRequest,
       CancellationToken cancellationToken = default);
}
