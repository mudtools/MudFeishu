// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.DataModels.Bitable;

namespace Mud.Feishu.Interfaces;


/// <summary>
/// <para>自动化流程 workflows是用户给多维表格设定的自动运行规则。设定“触发条件”和“执行操作”以后，多维表格会根据数据变更，自动执行下一步操作。</para>
/// <para>接口详细文档请参见：<see href="https://open.feishu.cn/document/docs/bitable-v1/app-workflow/list"/></para>
/// </summary>
[HttpClientApi(TokenManage = nameof(IFeishuAppManager), IsAbstract = true)]
[Token("TenantAccessToken", Name = Consts.Authorization)]
public interface IFeishuV1BitableWorkflow : IFeishuAppContextSwitcher
{

    /// <summary>
    /// 列出自动化流程
    /// <para>用于列出多维表格的自动化流程。</para>
    /// <para><see href="https://open.feishu.cn/document/docs/bitable-v1/app-workflow/list">接口文档</see></para>
    /// </summary>
    /// <param name="app_token">
    /// <para>多维表格 App 的唯一标识。不同形态的多维表格，其 app_token 的获取方式不同，参考[<see href="https://open.feishu.cn/document/ukTMukTMukTM/uUDN04SN0QjL1QDN/bitable-overview">多维表格 app_token 获取方式</see>]获取。</para>
    /// <para>示例值：AW3Qbtr2cakCnesXzXVbbsrIcVT</para>
    /// </param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    [Get("/open-apis/bitable/v1/apps/{app_token}/workflows")]
    Task<FeishuApiResult<GetAppWorkflowListResult>?> GetAppWorkflowListAsync(
         [Path] string app_token,
         CancellationToken cancellationToken = default);


    /// <summary>
    /// 更新自动化流程状态
    /// <para>开启或关闭自动化流程。</para>
    /// <para><see href="https://open.feishu.cn/document/docs/bitable-v1/app-workflow/update">接口文档</see></para>
    /// </summary>
    /// <param name="app_token">
    /// <para>多维表格 App 的唯一标识。不同形态的多维表格，其 app_token 的获取方式不同，参考[<see href="https://open.feishu.cn/document/ukTMukTMukTM/uUDN04SN0QjL1QDN/bitable-overview">多维表格 app_token 获取方式</see>]获取。</para>
    /// <para>示例值：AW3Qbtr2cakCnesXzXVbbsrIcVT</para>
    /// </param>
    /// <param name="workflow_id">
    /// <para>路径参数</para>
    /// <para>必填：是</para>
    /// <para>自动化工作流 ID，通过[列出自动化流程](https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/reference/bitable-v1/app-workflow/list)接口获取。</para>
    /// <para>示例值：730887xxxx552638996</para>
    /// </param>
    /// <param name="updateAppWorkflowRequest">更新自动化流程状态请求体</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    [Put("/open-apis/bitable/v1/apps/{app_token}/workflows/{workflow_id}")]
    Task<FeishuNullDataApiResult?> UpdateAppWorkflowAsync(
         [Path] string app_token,
         [Path] string workflow_id,
         [Body] UpdateAppWorkflowRequest updateAppWorkflowRequest,
         CancellationToken cancellationToken = default);


    /// <summary>
    /// 列出工作流
    /// <para>用于返回多维表格中所有工作流，多维表格管理员可通过此接口来管理表中的工作流。</para>
    /// <para><see href="https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/reference/bitable-v1/app-block_workflow/list">接口文档</see></para>
    /// </summary>
    /// <param name="app_token">
    /// <para>多维表格 App 的唯一标识。不同形态的多维表格，其 app_token 的获取方式不同，参考[<see href="https://open.feishu.cn/document/ukTMukTMukTM/uUDN04SN0QjL1QDN/bitable-overview">多维表格 app_token 获取方式</see>]获取。</para>
    /// <para>示例值：AW3Qbtr2cakCnesXzXVbbsrIcVT</para>
    /// </param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    [Get("/open-apis/bitable/v1/apps/{app_token}/block_workflows")]
    Task<FeishuApiResult<GetAppWorkflowListResult>?> GetAppBlockWorkflowListAsync(
         [Path] string app_token,
         CancellationToken cancellationToken = default);
}