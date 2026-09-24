// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.DataModels.Aily;

namespace Mud.Feishu.Interfaces;

/// <summary>
/// 飞书 Aily SDK 是一组服务端 OpenAPI 的封装，用于让用户以编程方式调用飞书 Aily（智能伙伴/智能体）的能力，把它集成到自己的业务系统里。
/// <para>接口详细文档请参见：<see href="https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/aily-v1/agent/agentuseguide"/></para>
/// </summary>
[HttpClientApi(TokenManage = nameof(IFeishuAppManager), IsAbstract = true)]
[Token(FeishuTokenTypes.TenantAccessToken, Name = Consts.Authorization)]
public interface IFeishuV1AilySkills
{

    /// <summary>
    /// 调用技能
    /// <para>用于调用某个 Aily 应用的特定技能，支持指定技能入参；并同步返回技能执行的结果。</para>
    /// <para><see href="https://open.feishu.cn/document/aily-v1/app-skill/start">接口文档</see></para>
    /// </summary>
    /// <param name="app_id">Aily 应用 ID（spring_xxx__c），示例值：spring_e7004f87f1__c</param>
    /// <param name="skill_id">技能 ID，示例值：skill_6cc6166178ca</param>
    /// <param name="request">调用技能请求体</param>
    /// <param name="x_aily_biz_user_id">可选请求头，标识创建会话的唯一用户 ID（建议使用内部唯一 ID 或飞书账号的 user_id），最大长度 255</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns></returns>
    [Post("/open-apis/aily/v1/apps/{app_id}/skills/{skill_id}/start")]
    Task<FeishuApiResult<StartSkillResult>?> StartSkillAsync(
        [Path] string app_id,
        [Path] string skill_id,
        [Body] StartSkillRequest request,
        [Header("X-Aily-BizUserID")] string? x_aily_biz_user_id = null,
        CancellationToken cancellationToken = default);


    /// <summary>
    /// 获取技能信息
    /// <para>用于查询某个 Aily 应用的特定技能详情。</para>
    /// <para><see href="https://open.feishu.cn/document/aily-v1/app-skill/get">接口文档</see></para>
    /// </summary>
    /// <param name="app_id">Aily 应用 ID（spring_xxx__c），示例值：spring_e7004f87f1__c</param>
    /// <param name="skill_id">技能 ID，示例值：skill_6cc6166178ca</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns></returns>
    [Get("/open-apis/aily/v1/apps/{app_id}/skills/{skill_id}")]
    Task<FeishuApiResult<SkillOopsResult>?> GetSkillAsync(
        [Path] string app_id,
        [Path] string skill_id,
        CancellationToken cancellationToken = default);


    /// <summary>
    /// 查询技能列表
    /// <para>用于查询某个 Aily 应用的技能列表；包括内置的数据分析与问答技能、以及未在对话开启的技能。</para>
    /// <para><see href="https://open.feishu.cn/document/aily-v1/app-skill/list">接口文档</see></para>
    /// </summary>
    /// <param name="app_id">Aily 应用 ID（spring_xxx__c），示例值：spring_e7004f87f1__c</param>
    /// <param name="page_size">分页大小，即本次请求所返回的信息列表内的最大条目数。默认值：20</param>
    /// <param name="page_token">分页标记，第一次请求不填，表示从头开始遍历；分页查询结果还有更多项时会同时返回新的 page_token，下次遍历可采用该 page_token 获取查询结果</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns></returns>
    [Get("/open-apis/aily/v1/apps/{app_id}/skills")]
    Task<FeishuApiResult<SkillPageListResult>?> GetSkillPageListAsync(
        [Path] string app_id,
        [Query("page_size")] int page_size = Consts.PageSize_20,
        [Query("page_token")] string? page_token = null,
        CancellationToken cancellationToken = default);
}
