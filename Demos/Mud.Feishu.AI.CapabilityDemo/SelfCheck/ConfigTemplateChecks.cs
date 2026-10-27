// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
// -----------------------------------------------------------------------

using Mud.Feishu.AI.CapabilityDemo.Common;

namespace Mud.Feishu.AI.CapabilityDemo.SelfCheck;

/// <summary>组 3：配置模板（键不漂移、密钥为空、无 <c>FeishuAgent</c> 双真相源）。</summary>
internal static class ConfigTemplateChecks
{
    public static IEnumerable<CheckResult> Run(string baseDirectory)
    {
        const string group = "config-template";
        var path = Path.Combine(baseDirectory, DemoSettings.AppSettingsFile);

        if (!File.Exists(path))
        {
            yield return new CheckResult(group, "模板存在", false, path, "文件不存在");
            yield break;
        }

        string json = File.ReadAllText(path);
        JsonDocument? document = null;
        string? parseError = null;
        try
        {
            document = JsonDocument.Parse(json, new JsonDocumentOptions
            {
                CommentHandling = JsonCommentHandling.Skip,
                AllowTrailingCommas = true,
            });
        }
        catch (JsonException ex)
        {
            parseError = ex.Message;
        }

        if (document is null)
        {
            yield return new CheckResult(group, "模板可解析", false, "合法 JSON（允许注释）", parseError);
            yield break;
        }

        using (document)
        {
            var root = document.RootElement;

            yield return new CheckResult(group, "模板可解析", true);

            var hasSection = root.TryGetProperty(DemoSettings.SectionName, out var section)
                             && section.ValueKind == JsonValueKind.Object;
            yield return new CheckResult(group, "含 FeishuCapabilityDemo 节", hasSection);

            if (!hasSection)
            {
                yield break;
            }

            var templateKeys = section.EnumerateObject()
                .Select(static p => p.Name)
                .ToHashSet(StringComparer.Ordinal);
            var expectedKeys = DemoSettings.ConfigurationKeys.ToHashSet(StringComparer.Ordinal);

            var unknown = templateKeys.Except(expectedKeys, StringComparer.Ordinal).ToArray();
            var absent = expectedKeys.Except(templateKeys, StringComparer.Ordinal).ToArray();

            yield return new CheckResult(group, "无未知键", unknown.Length == 0, "无", string.Join(",", unknown));
            yield return new CheckResult(group, "无缺失键", absent.Length == 0, "无", string.Join(",", absent));

            foreach (var secretKey in new[]
                     {
                         DemoSettings.KeyApiKey, DemoSettings.KeyAppId, DemoSettings.KeyAppSecret,
                     })
            {
                var value = section.TryGetProperty(secretKey, out var element) ? element.GetString() : null;
                yield return new CheckResult(
                    group,
                    $"模板 {secretKey} 为空",
                    string.IsNullOrEmpty(value),
                    "空串",
                    value is null ? "（缺键）" : value);
            }

            yield return new CheckResult(
                group,
                "无 FeishuAgent 节（代码是唯一白名单真相源）",
                !root.TryGetProperty("FeishuAgent", out _));
        }
    }
}
