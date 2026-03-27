// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.DataModels.Bitable;

namespace Mud.Feishu.Interfaces;

/// <summary>
/// <para>飞书多维表格（Base）是全新的业务管理工具，帮助用户重构工作应用和团队协同模式，高效在线协同数据，随心构建个性化应用，轻松掌控全盘业务数据，和团队一起创造效率的无限可能。</para>
/// <para>多维表格可以是一个表格，也可以是无数个应用。它拥有强大的底层开放能力，你可以通过多维表格 API 轻松打通内部其他业务系统，让业务数据通畅流转，实时同步。</para>
/// <para>接口详细文档请参见：<see href="https://open.feishu.cn/document/server-docs/docs/bitable-v1/bitable-overview"/></para>
/// </summary>
[HttpClientApi(TokenManage = nameof(IFeishuAppManager), IsAbstract = true)]
[Header(Consts.Authorization)]
public interface IFeishuV1Bitable : IFeishuAppContextSwitcher
{
    /// <summary>
    /// 创建多维表格
    /// <para>在指定文件夹中创建一个多维表格，包含一个空白的数据表。</para>
    /// <para><see href="https://open.feishu.cn/document/server-docs/docs/bitable-v1/app/create">接口文档</see></para>
    /// </summary>
    /// <param name="createAppRequest">创建多维表格应用请求体</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    [Post("/open-apis/bitable/v1/apps")]
    Task<FeishuApiResult<CreateBitableAppResult>?> CreateBitableAppAsync(
      [Body] CreateBitableAppRequest createAppRequest,
      CancellationToken cancellationToken = default);


    /// <summary>
    /// 复制多维表格
    /// <para>复制一个多维表格，可以指定复制到某个有权限的文件夹下。</para>
    /// <para><see href="https://open.feishu.cn/document/server-docs/docs/bitable-v1/app/copy">接口文档</see></para>
    /// </summary>
    /// <param name="copyBitableAppRequest">复制多维表格应用请求体</param>
    /// <param name="app_token">
    /// <para>要复制的多维表格 App 的唯一标识。不同形态的多维表格，其 app_token 的获取方式不同，参考[<see href="https://open.feishu.cn/document/ukTMukTMukTM/uUDN04SN0QjL1QDN/bitable-overview">多维表格 app_token 获取方式</see>]获取。</para>
    /// <para>示例值：AW3Qbtr2cakCnesXzXVbbsrIcVT</para>
    /// </param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    [Post("/open-apis/bitable/v1/apps/{app_token}/copy")]
    Task<FeishuApiResult<CopyBitableResult>?> CopyBitableAppAsync(
        [Path] string app_token,
        [Body] CopyBitableAppRequest copyBitableAppRequest,
        CancellationToken cancellationToken = default);
}
