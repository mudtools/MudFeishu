// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.DataModels.Drive.Files;

namespace Mud.Feishu;


/// <summary>
/// 文件是云空间内各种类型的文件的统称，泛指云空间内所有的文件。包括在云空间创建的在线文档、电子表格、多维表格、思维笔记、知识库中的文档等，也包括从本地环境上传的各类文件。
/// <para>接口详细文档请参见：<see href="https://open.feishu.cn/document/docs/drive-v1/file/file-overview"/></para>
/// </summary>
[HttpClientApi(TokenManage = nameof(IFeishuAppManager), RegistryGroupName = "Drive", InheritedFrom = nameof(FeishuV1DriveFiles))]
[Token("UserAccessToken", Name = Consts.Authorization)]
public interface IFeishuUserV1DriveFiles : IFeishuV1DriveFiles, ICurrentUserId
{
    /// <summary>
    /// 用于根据搜索关键词（search_key）对当前用户可见的云文档进行搜索。。
    /// </summary>
    /// <param name="searchFileObjectRequest">搜索云文档请求体</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns></returns>
    [Post("/open-apis/suite/docs-api/search/object")]
    Task<FeishuApiResult<SearchFileObjectResult>?> SearchFilesAsync(
        [Body] SearchFileObjectRequest searchFileObjectRequest,
        CancellationToken cancellationToken = default);
}
