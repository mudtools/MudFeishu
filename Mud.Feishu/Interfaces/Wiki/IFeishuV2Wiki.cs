// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.DataModels.Wiki;

namespace Mud.Feishu.Interfaces;


/// <summary>
/// <para>飞书知识库是一个面向组织的知识管理系统。通过结构化沉淀高价值信息，形成完整的知识体系。</para>
/// <para>此外，明确的内容分类，层级式的页面树，还能够轻松提升知识的流转和传播效率，更好地成就组织和个人。</para>
/// <para>接口详细文档请参见：<see href="https://open.feishu.cn/document/server-docs/docs/wiki-v2/wiki-overview"/></para>
/// </summary>
[HttpClientApi(TokenManage = nameof(IFeishuAppManager), IsAbstract = true)]
[Token("TenantAccessToken", Name = Consts.Authorization)]
public interface IFeishuV2Wiki : IFeishuAppContextSwitcher
{
    /// <summary>
    /// 用于获取有权限访问的知识空间列表。
    /// <para>## 注意事项</para>
    /// <para>- 使用 tenant access token 调用时，请确认应用或机器人拥有部分知识空间的访问权限，否则返回列表为空。参阅[如何将应用添加为知识库管理员（成员）](https://open.feishu.cn/document/ukTMukTMukTM/uUDN04SN0QjL1QDN/wiki-v2/wiki-qa#b5da330b)。</para>
    /// <para>- 此接口为分页接口。由于权限过滤，可能返回列表为空，但当分页标记（has_more）为 true 时，可以继续分页请求。</para>
    /// <para>- 此接口不会返回**我的文档库**。</para>
    /// </summary>
    /// <param name="page_size">分页大小，即本次请求所返回的用户信息列表内的最大条目数。默认值：10</param>
    /// <param name="page_token">分页标记，第一次请求不填，表示从头开始遍历；分页查询结果还有更多项时会同时返回新的 page_token，下次遍历可采用该 page_token 获取查询结果</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    [Get("/open-apis/wiki/v2/spaces")]
    Task<FeishuApiPageListResult<SpaceInfo>?> GetSpacesPageListAsync(
      [Query("page_size")] int page_size = Consts.PageSize_10,
      [Query("page_token")] string? page_token = null,
      CancellationToken cancellationToken = default);

    /// <summary>
    /// <para>获取知识空间信息</para>
    /// <para>此接口用于根据知识空间 ID 查询知识空间的信息，包括空间的类型、可见性、分享状态等。</para>
    /// </summary>
    /// <param name="space_id">
    /// <para>路径参数</para>
    /// <para>必填：是</para>
    /// <para>知识空间 ID。</para>
    /// <para>示例值：6870403571079249922</para>
    /// </param>
    /// <param name="lang">
    /// <para>必填：否</para>
    /// <para>当查询**我的文档库**时，指定返回的文档库名称展示语言。</para>
    /// <para>示例值：zh</para>
    /// <list type="bullet">
    /// <item>zh：简体中文</item>
    /// <item>id：印尼语</item>
    /// <item>de：德语</item>
    /// <item>en：英语</item>
    /// <item>es：西班牙语</item>
    /// <item>fr：法语</item>
    /// <item>it：意大利语</item>
    /// <item>pt：葡萄牙语</item>
    /// <item>vi：越南语</item>
    /// <item>ru：俄语</item>
    /// <item>hi：印地语</item>
    /// <item>th：泰语</item>
    /// <item>ko：韩语</item>
    /// <item>ja：日语</item>
    /// <item>zh-HK：繁体中文（中国香港）</item>
    /// <item>zh-TW：繁体中文（中国台湾）</item>
    /// </list>
    /// <para>默认值：en</para>
    /// </param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    [Get("/open-apis/wiki/v2/spaces/{space_id}")]
    Task<FeishuApiResult<SpaceInfoResult>?> GetSpaceInfoAsync(
         [Path] string space_id,
         [Query("lang")] string? lang = "en",
         CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取知识空间成员列表。
    /// </summary>
    /// <param name="space_id">
    /// <para>路径参数</para>
    /// <para>必填：是</para>
    /// <para>知识空间 ID。</para>
    /// <para>示例值：6870403571079249922</para>
    /// </param>
    /// <param name="page_size">分页大小，即本次请求所返回的用户信息列表内的最大条目数。默认值：10</param>
    /// <param name="page_token">分页标记，第一次请求不填，表示从头开始遍历；分页查询结果还有更多项时会同时返回新的 page_token，下次遍历可采用该 page_token 获取查询结果</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    [Get("/open-apis/wiki/v2/spaces/{space_id}/members")]
    Task<FeishuApiResult<SpaceMemberResult>?> GetSpaceMemberPageListAsync(
        [Path] string space_id,
        [Query("page_size")] int page_size = Consts.PageSize_10,
        [Query("page_token")] string? page_token = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 添加知识空间成员。
    /// <para>## 前提条件</para>
    /// <para>调用此接口前，请确保调用身份对应的应用或用户为知识空间的管理员。</para>
    /// <para>## 注意事项</para>
    /// <para>- 使用 tenant access token 身份操作时，无法使用部门 ID (opendepartmentid) 添加知识空间成员。</para>
    /// <para>- 公开知识空间（即 visibility [可见性](<see href="https://open.feishu.cn/document/ukTMukTMukTM/uUDN04SN0QjL1QDN/wiki-overview"/>)为 public 的空间）对租户所有用户可见，因此不支持再添加成员，但可以添加管理员。</para>
    /// <para>- 个人知识空间 （即 type [类型](<see href="https://open.feishu.cn/document/ukTMukTMukTM/uUDN04SN0QjL1QDN/wiki-overview"/>)为 person 的空间）为个人管理的知识空间，不支持添加其他管理员（包括应用/机器人）。但可以添加成员。</para>
    /// </summary>
    /// <param name="space_id">
    /// <para>路径参数</para>
    /// <para>必填：是</para>
    /// <para>知识空间 ID。</para>
    /// <para>示例值：6870403571079249922</para>
    /// </param>
    /// <param name="createSpaceMemberRequest">添加知识空间成员请求体</param>
    /// <param name="need_notification">
    /// <para>必填：否</para>
    /// <para>添加权限后是否通知对方</para>
    /// <para>示例值：true</para>
    /// <para>默认值：null</para>
    /// </param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    [Post("/open-apis/wiki/v2/spaces/{space_id}/members")]
    Task<FeishuApiResult<SpaceInfoResult>?> CreateSpaceMemberAsync(
         [Path] string space_id,
         [Body] CreateSpaceMemberRequest createSpaceMemberRequest,
         [Query("need_notification")] bool? need_notification = null,
         CancellationToken cancellationToken = default);

    /// <summary>
    /// 删除知识空间成员。
    /// </summary>
    /// <param name="space_id">
    /// <para>路径参数</para>
    /// <para>必填：是</para>
    /// <para>知识空间 ID。</para>
    /// <para>示例值：6870403571079249922</para>
    /// </param>
    /// <param name="member_id">
    /// <para>路径参数</para>
    /// <para>必填：是</para>
    /// <para>成员id，值的类型由请求体的 member_type 参数决定</para>
    /// <para>示例值：g64fb7g7</para>
    /// </param>
    /// <param name="deleteSpaceMemberRequest">删除知识空间成员请求体</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    [Delete("/open-apis/wiki/v2/spaces/{space_id}/members/{member_id}")]
    Task<FeishuApiResult<DeleteSpaceMemberResult>?> DeleteSpaceMemberAsync(
         [Path] string space_id,
         [Path] string member_id,
         [Body] DeleteSpaceMemberRequest deleteSpaceMemberRequest,
         CancellationToken cancellationToken = default);

    /// <summary>
    /// 更新知识空间设置。
    /// </summary>
    /// <param name="space_id">
    /// <para>路径参数</para>
    /// <para>必填：是</para>
    /// <para>知识空间 ID。</para>
    /// <para>示例值：6870403571079249922</para>
    /// </param>
    /// <param name="updateSpaceSettingRequest">更新知识空间设置请求体</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    [Put("/open-apis/wiki/v2/spaces/{space_id}/setting")]
    Task<FeishuApiResult<UpdateSpaceSettingResult>?> UpdateSpaceSettingAsync(
     [Path] string space_id,
     [Body] UpdateSpaceSettingRequest updateSpaceSettingRequest,
     CancellationToken cancellationToken = default);
}
