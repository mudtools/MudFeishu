// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.DataModels.Wiki;

namespace Mud.Feishu;

/// <summary>
/// <para>飞书知识库是一个面向组织的知识管理系统。通过结构化沉淀高价值信息，形成完整的知识体系。</para>
/// <para>此外，明确的内容分类，层级式的页面树，还能够轻松提升知识的流转和传播效率，更好地成就组织和个人。</para>
/// <para>接口详细文档请参见：<see href="https://open.feishu.cn/document/docs/drive-v1/file/file-overview"/></para>
/// </summary>
[HttpClientApi(TokenManage = nameof(IFeishuAppManager), RegistryGroupName = "Wiki", InheritedFrom = nameof(FeishuV2Wiki))]
[Token(FeishuTokenTypes.UserAccessToken, Name = Consts.Authorization)]
public interface IFeishuUserV2Wiki : IFeishuV2Wiki, ICurrentUserId
{
    /// <summary>
    /// 创建知识空间。
    /// </summary>
    /// <param name="createSpaceRequest">创建知识空间请求体</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns></returns>
    [Post("/open-apis/wiki/v2/spaces")]
    Task<FeishuApiResult<SpaceInfoResult>?> CreateSpaceAsync(
         [Body] CreateSpaceRequest createSpaceRequest,
         CancellationToken cancellationToken = default);
}
