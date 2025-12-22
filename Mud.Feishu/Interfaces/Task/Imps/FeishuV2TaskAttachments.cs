// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Microsoft.Extensions.Logging;
using Mud.Feishu.DataModels.TaskAttachments;
using System.Net.Http.Headers;
using System.Text.Json;

namespace Mud.Feishu;

partial class FeishuV2TaskAttachments
{
    public async Task<FeishuApiResult<TaskAttachmentsUploadResult>?> UploadAttachmentAsync(
         UploadTaskAttachmentsRequest uploadFileRequest,
         string user_id_type = Consts.User_Id_Type,
         CancellationToken cancellationToken = default)
    {
        var access_token = await _tokenManager.GetTokenAsync();
        if (string.IsNullOrEmpty(access_token))
        {
            throw new InvalidOperationException("无法获取访问令牌");
        }
        var url = $"https://open.feishu.cn/open-apis/task/v2/attachments/upload";
        _logger.LogDebug("开始HTTP Post请求: {Url}", url);
        using var request = new HttpRequestMessage(HttpMethod.Post, url);

        request.Headers.Add("Authorization", access_token);

        ArgumentNullException.ThrowIfNull(uploadFileRequest, nameof(uploadFileRequest));

        using var formData = new MultipartFormDataContent
        {
            { new StringContent(uploadFileRequest.ResourceId), "resource_id" },
            { new StringContent(uploadFileRequest.ResourceType), "resource_type" }
        };

        var fileContent = new ByteArrayContent(File.ReadAllBytes(uploadFileRequest.File));
        fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse("application/octet-stream");
        formData.Add(fileContent, "file", Path.GetFileName(uploadFileRequest.File));
        request.Content = fileContent;
        try
        {
            _httpClient.Timeout = TimeSpan.FromMinutes(10);
            using var response = await _httpClient.SendAsync(request, cancellationToken);
            _logger.LogDebug("HTTP请求完成: {StatusCode}", (int)response.StatusCode);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError("HTTP请求失败: {StatusCode}, 响应: {Response}", (int)response.StatusCode, errorContent);
                throw new HttpRequestException($"HTTP请求失败: {(int)response.StatusCode}");
            }
            using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);

            if (stream.Length == 0)
            {
                return default;
            }

            var result = await JsonSerializer.DeserializeAsync<FeishuApiResult<TaskAttachmentsUploadResult>>(stream, _jsonSerializerOptions, cancellationToken);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "HTTP请求异常: {Url}", url);
            throw;
        }
    }
}
