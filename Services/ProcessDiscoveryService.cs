using System.Diagnostics;
using Nioh3AffixEditor.Models;

namespace Nioh3AffixEditor.Services;

public sealed class ProcessDiscoveryService
{
    public IReadOnlyList<ProcessInfo> FindByProcessName(string processName)
    {
        if (string.IsNullOrWhiteSpace(processName))
        {
            return Array.Empty<ProcessInfo>();
        }

        var name = processName;
        if (name.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
        {
            name = name[..^4];
        }

        try
        {
            return Process
                .GetProcessesByName(name)
                .OrderBy(p => p.Id)
                .Select(p => new ProcessInfo(p.Id, p.ProcessName + ".exe", SafeGetMainWindowTitle(p)))
                .ToList();
        }
        catch
        {
            return Array.Empty<ProcessInfo>();
        }
    }

    private static string? SafeGetMainWindowTitle(Process p)
    {
        try
        {
            var title = p.MainWindowTitle;
            return string.IsNullOrWhiteSpace(title) ? null : title;
        }
        catch
        {
            return null;
        }
    }
}

