namespace Nioh3AffixEditor.Models;

public sealed record ProcessInfo(int Id, string Name, string? MainWindowTitle)
{
    public override string ToString()
        => string.IsNullOrWhiteSpace(MainWindowTitle)
            ? $"{Name} (PID {Id})"
            : $"{Name} (PID {Id}) — {MainWindowTitle}";
}

