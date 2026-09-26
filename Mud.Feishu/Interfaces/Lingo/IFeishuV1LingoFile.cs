// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.DataModels.Lingo;

namespace Mud.Feishu.Interfaces;

/// <summary>
/// 飞书词典（Lingo）文件入口域 SDK 是一组服务端 OpenAPI 的封装，用于上传词条图片与下载词条图片。本接口全部端点支持 tenant_access_token 与 user_access_token 调用（租户态见 <see cref="IFeishuTenantV1LingoFile"/>，用户态见 <see cref="IFeishuUserV1LingoFile"/>）。
/// <para>接口详细文档请参见：<see href="https://open.feishu.cn/document/lingo-v1/overview"/></para>
/// </summary>
[HttpClientApi(TokenManage = nameof(IFeishuAppManager), IsAbstract = true)]
[Token(FeishuTokenTypes.TenantAccessToken, Name = Consts.Authorization)]
public interface IFeishuV1LingoFile : IFeishuAppContextSwitcher
{
    /// <summary>
    /// 上传词条图片
    /// <para>上传词条关联的图片文件（multipart/form-data），返回 file_token，可用于关联至词条 images[].token 或下载。仅支持 icon、bmp、gif、png、jpeg、webp 六种格式，高宽像素在 320 - 4096 像素之间，大小在 3KB - 10MB。</para>
    /// <para>限频：100 次/分钟。所需权限（任一即可）：baike:entity（查看、创建、编辑、删除词典词条）、baike:entity:exempt_review（创建、更新词典免审词条）。支持的应用类型：自建应用。</para>
    /// <para><see href="https://open.feishu.cn/document/lingo-v1/file/upload">接口文档</see></para>
    /// </summary>
    /// <param name="request">上传请求体（name 文件名称必填 1 ～ 100 字符；file 为图片文件本地绝对路径）</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns>返回文件 token（file_token）</returns>
    [Post("/open-apis/lingo/v1/files/upload")]
    Task<FeishuApiResult<UploadFileResult>?> UploadFileAsync(
        [FormContent] UploadFileRequest request,
        CancellationToken cancellationToken = default);


    /// <summary>
    /// 下载词条图片
    /// <para>根据文件 token 下载词条关联的图片，响应体为图片二进制流。</para>
    /// <para>限频：100 次/分钟。所需权限（任一即可）：baike:entity（查看、创建、编辑、删除词典词条）、baike:entity:exempt_review（创建、更新词典免审词条）、baike:entity:readonly（查看词典词条）。支持的应用类型：自建应用。</para>
    /// <para><see href="https://open.feishu.cn/document/lingo-v1/file/download">接口文档</see></para>
    /// </summary>
    /// <param name="file_token">文件 token，示例值：boxbcEcmKiD****vgqWTpvdc7jc</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns>成功时返回图片二进制内容。</returns>
    /// <exception cref="ApiException">HTTP 200 + JSON 错误体场景（如 99991400 系统繁忙、99991672 无权限等业务错误）仍可能以 200 返回，
    /// 此时反序列化将失败并抛出 <see cref="ApiException"/>，调用方需针对该残余风险做异常兜底（参见 <c>documents/ErrorHandling.md</c>）。</exception>
    [Get("/open-apis/lingo/v1/files/{file_token}/download")]
    Task<byte[]?> DownloadFileAsync(
        [Path] string file_token,
        CancellationToken cancellationToken = default);
}
