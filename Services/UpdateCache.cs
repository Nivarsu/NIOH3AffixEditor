namespace Nioh3AffixEditor.Services;

public sealed class UpdateCache
{
    public DateTimeOffset? LastCheckedUtc { get; set; }
    public string? RemoteVersionRaw { get; set; }
    public string? RemoteAnnouncementText { get; set; }
    public string? LatestAnnouncementVersionRaw { get; set; }
    public string? LatestAnnouncementText { get; set; }
    public List<UpdateAnnouncementCacheEntry>? Announcements { get; set; }
}

public sealed class UpdateAnnouncementCacheEntry
{
    public string VersionRaw { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
}
