namespace Nioh3AffixEditor.Services;

public sealed class UpdateCheckResult
{
    public required Version LocalVersion { get; init; }
    public Version? RemoteVersion { get; init; }
    public string? RemoteVersionRaw { get; init; }
    public string? RemoteAnnouncementsText { get; init; }
    public string? RemoteAnnouncementText { get; init; }
    public string? LatestAnnouncementVersionRaw { get; init; }
    public string? LatestAnnouncementText { get; init; }
    public string? Error { get; init; }
    public required Uri PostUri { get; init; }

    public bool HasUpdate => RemoteVersion is not null && RemoteVersion > LocalVersion;
}
