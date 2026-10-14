// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using System.Reflection;
using Microsoft.CodeAnalysis;

namespace Mud.AI.Generator.Tests;

public static class TestHelper
{
    private static readonly Assembly GeneratorAssembly;

    static TestHelper()
    {
        var assemblyName = "Mud.AI.Generator";

        GeneratorAssembly = AppDomain.CurrentDomain.GetAssemblies()
            .FirstOrDefault(a => a.GetName().Name == assemblyName);

        if (GeneratorAssembly == null)
        {
            var assemblyPath = Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory,
                $"{assemblyName}.dll");

            if (File.Exists(assemblyPath))
            {
                GeneratorAssembly = Assembly.LoadFrom(assemblyPath);
            }
            else
            {
                throw new InvalidOperationException(
                    $"无法找到 {assemblyName} 程序集。搜索路径: {assemblyPath}");
            }
        }
    }

    public static Type GetType(string typeName)
    {
        return GeneratorAssembly.GetType(typeName)
            ?? throw new InvalidOperationException($"无法在 Mud.AI.Generator 程序集中找到类型: {typeName}");
    }

    public static MethodInfo GetMethod(Type type, string methodName, BindingFlags bindingFlags = BindingFlags.Public | BindingFlags.Static)
    {
        return type.GetMethod(methodName, bindingFlags)
            ?? throw new InvalidOperationException($"无法在类型 {type.Name} 中找到方法: {methodName}");
    }
}

public static class BasicReferenceAssemblies
{
    public static List<MetadataReference> GetReferences()
    {
        var references = new List<MetadataReference>
        {
            MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(Task).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(Task<>).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(List<>).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(System.Text.Json.JsonSerializer).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(Microsoft.Extensions.Logging.ILogger).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(System.Net.Http.HttpClient).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(Mud.HttpUtils.HttpClientUtils).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(System.IO.Stream).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(System.Collections.IEnumerable).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(System.Collections.Generic.IAsyncEnumerable<>).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(System.Runtime.CompilerServices.AsyncIteratorMethodBuilder).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(System.Linq.Enumerable).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(Microsoft.Extensions.Caching.Memory.IMemoryCache).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(Microsoft.Extensions.Options.IOptions<>).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(System.Attribute).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(Mud.AI.Attributes.SdkToolProfileAttribute).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(Mud.HttpUtils.Attributes.GetAttribute).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(Mud.HttpUtils.Attributes.PathAttribute).Assembly.Location),
        };

        // [M0] 全量运行时框架引用：编译断言需要「输入 + 生成产物」整体可编译（F15）。
        // 仅靠上述最小引用无法满足 Task<>/Uri 等在 .NET 8 共享框架中的转发定义，
        // 会令编译断言产生与生成器无关的假阴性。此处按运行时基目录枚举全部托管运行时程序集，
        // 消除对个别 DLL 名称的依赖（如 System.Private.Uri / System.Threading.Tasks 跨版本变化）。
        // 用 AssemblyName.GetAssemblyName 鉴别仅含托管元数据的 PE（跳过 Native/资源 DLL）。
        var runtimeDir = Path.GetDirectoryName(typeof(object).Assembly.Location)!;
        if (Directory.Exists(runtimeDir))
        {
            // [M1] 去重必须按**程序集简单名**而不是文件路径：上表显式引用的程序集（如
            // System.Text.Json）可能来自 NuGet 包的更高版本（10.0.x），而运行时基目录里还有
            // 共享框架的同名低版本（8.0.x）。按路径去参会同时把两者加进编译 ⇒ CS0433
            // 「类型同时存在于两个版本」——那是引用装配缺陷，不是生成器的产物问题（会让
            // 「生成产物可编译」断言对全部用例假红）。同名即视为已提供，显式引用优先。
            var addedPaths = new HashSet<string>(
                references.Select(r => r.Display),
                StringComparer.OrdinalIgnoreCase);
            var addedNames = new HashSet<string>(
                references.Select(r => Path.GetFileNameWithoutExtension(r.Display)),
                StringComparer.OrdinalIgnoreCase);
            foreach (var dll in Directory.EnumerateFiles(runtimeDir, "*.dll", SearchOption.TopDirectoryOnly))
            {
                if (!addedPaths.Add(dll)
                    || !addedNames.Add(Path.GetFileNameWithoutExtension(dll)))
                {
                    continue;
                }

                try
                {
                    _ = AssemblyName.GetAssemblyName(dll); // 非托管 PE 或损坏文件会抛 BadImageFormatException
                    references.Add(MetadataReference.CreateFromFile(dll));
                }
                catch
                {
                    // 跳过无托管元数据的文件（Native 运行时 / 资源 DLL）
                }
            }
        }

        return references;
    }
}