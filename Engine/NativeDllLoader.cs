using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;

namespace Nioh3AffixEditor.Engine;

/// <summary>
/// 负责从嵌入资源中提取原生DLL并加载
/// </summary>
internal static class NativeDllLoader
{
    private static readonly object _lock = new();
    private static bool _initialized;
    private static string? _extractedDllPath;

    /// <summary>
    /// 确保原生DLL已被提取并加载
    /// </summary>
    public static void EnsureLoaded()
    {
        if (_initialized) return;

        lock (_lock)
        {
            if (_initialized) return;

            _extractedDllPath = ExtractNativeDll();
            if (_extractedDllPath != null)
            {
                // 预加载DLL，这样LibraryImport就能找到它
                NativeLibrary.Load(_extractedDllPath);
            }

            _initialized = true;
        }
    }

    /// <summary>
    /// 从嵌入资源中提取DLL到临时目录
    /// </summary>
    private static string? ExtractNativeDll()
    {
        const string resourceName = "Nioh3AffixEditor.Nioh3AffixCore.dll";
        const string dllName = "Nioh3AffixCore.dll";

        var assembly = Assembly.GetExecutingAssembly();

        // 检查是否有嵌入资源
        using var stream = assembly.GetManifestResourceStream(resourceName);
        if (stream == null)
        {
            // 没有嵌入资源，可能是开发模式，DLL在输出目录
            return null;
        }

        // 创建临时目录
        var tempDir = Path.Combine(Path.GetTempPath(), "Nioh3AffixEditor", GetAssemblyVersion());
        Directory.CreateDirectory(tempDir);

        var dllPath = Path.Combine(tempDir, dllName);

        // 如果文件已存在且大小相同，跳过提取
        if (File.Exists(dllPath))
        {
            var existingSize = new FileInfo(dllPath).Length;
            if (existingSize == stream.Length)
            {
                return dllPath;
            }
        }

        // 提取DLL
        using var fileStream = File.Create(dllPath);
        stream.CopyTo(fileStream);

        return dllPath;
    }

    private static string GetAssemblyVersion()
    {
        var version = Assembly.GetExecutingAssembly().GetName().Version;
        return version?.ToString() ?? "0.0.0.0";
    }

    /// <summary>
    /// 清理临时文件（可选，在程序退出时调用）
    /// </summary>
    public static void Cleanup()
    {
        // 不删除临时文件，因为下次启动可以复用
        // 如果需要清理，可以在这里实现
    }
}
