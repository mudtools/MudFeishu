// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.DataModels.Bitable;

namespace Mud.Feishu.Interfaces;

/// <summary>
/// <para>表单视图 form，表单视图是多维表格的一种视图类型，形式类似于问卷，可以用来收集信息和数据。</para>
/// <para>每个表单都有唯一标识 form_id，即当前视图的 view_id。form_id 的获取方式和 view_id 的获取方式相同。</para>
/// <para>接口详细文档请参见：<see href="https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/reference/bitable-v1/app-table-form/upgrade"/></para>
/// </summary>
[HttpClientApi(TokenManage = nameof(IFeishuAppManager), IsAbstract = true)]
[Token(TokenType.TenantAccessToken, Name = Consts.Authorization)]
public interface IFeishuV1BitableForm : IFeishuAppContextSwitcher
{

    /// <summary>
    /// 升级表单
    /// <para>升级旧版表单至收集表。</para>
    /// <para><see href="https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/reference/bitable-v1/app-table-form/upgrade">接口文档</see></para>
    /// </summary>
    /// <param name="app_token">
    /// <para>多维表格 App 的唯一标识。不同形态的多维表格，其 app_token 的获取方式不同，参考[<see href="https://open.feishu.cn/document/ukTMukTMukTM/uUDN04SN0QjL1QDN/bitable-overview">多维表格 app_token 获取方式</see>]获取。</para>
    /// <para>示例值：AW3Qbtr2cakCnesXzXVbbsrIcVT</para>
    /// </param>
    /// <param name="table_id">
    /// <para>多维表格数据表的唯一标识。</para>
    /// <para>示例值：tbl1TkhyTWDkSoZ3</para>
    /// </param>
    /// <param name="form_id">
    /// <para>多维表格中表单的唯一标识。</para>
    /// <para>示例值：vew6oMbAa4</para>
    /// </param>
    /// <param name="upgradeFormRequest">升级表单请求体</param> 
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    [Post("/open-apis/bitable/v1/apps/{app_token}/tables/{table_id}/forms/{form_id}/upgrade")]
    Task<FeishuApiResult<UpgradeFormResult>?> UpgradeFormAsync(
      [Path] string app_token,
      [Path] string table_id,
      [Path] string form_id,
      [Body] UpgradeFormRequest upgradeFormRequest,
      CancellationToken cancellationToken = default);


    /// <summary>
    /// 更新表单元数据
    /// <para>更新表单视图中的元数据，包括表单名称、描述、是否共享等。</para>
    /// <para><see href="https://open.feishu.cn/document/server-docs/docs/bitable-v1/form/patch-2">接口文档</see></para>
    /// </summary>
    /// <param name="app_token">
    /// <para>多维表格 App 的唯一标识。不同形态的多维表格，其 app_token 的获取方式不同，参考[<see href="https://open.feishu.cn/document/ukTMukTMukTM/uUDN04SN0QjL1QDN/bitable-overview">多维表格 app_token 获取方式</see>]获取。</para>
    /// <para>示例值：AW3Qbtr2cakCnesXzXVbbsrIcVT</para>
    /// </param>
    /// <param name="table_id">
    /// <para>多维表格数据表的唯一标识。</para>
    /// <para>示例值：tbl1TkhyTWDkSoZ3</para>
    /// </param>
    /// <param name="form_id">
    /// <para>多维表格中表单的唯一标识。</para>
    /// <para>示例值：vew6oMbAa4</para>
    /// </param>
    /// <param name="updateFormRequest">更新表单请求体</param> 
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    [Patch("/open-apis/bitable/v1/apps/{app_token}/tables/{table_id}/forms/{form_id}")]
    Task<FeishuApiResult<UpdateFormResult>?> UpdateFormAsync(
      [Path] string app_token,
      [Path] string table_id,
      [Path] string form_id,
      [Body] UpdateFormRequest updateFormRequest,
      CancellationToken cancellationToken = default);
}