using System.Net.Http;
using System.Reflection;

namespace Nioh3AffixEditor.Services;

public sealed class UpdateCheckService
{
    private static readonly Uri DisabledUpdateUri = new("about:blank");

    public UpdateCheckService(HttpClient httpClient, UpdateCacheStore cacheStore)
    {
        _ = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _ = cacheStore ?? throw new ArgumentNullException(nameof(cacheStore));
    }

    public Task<UpdateCheckResult> CheckAsync(CancellationToken cancellationToken)
    {
        _ = cancellationToken;

        var localVersion = Assembly.GetExecutingAssembly().GetName().Version ?? new Version(0, 0, 0, 0);
        return Task.FromResult(new UpdateCheckResult
        {
            LocalVersion = localVersion,
            RemoteVersion = null,
            RemoteVersionRaw = null,
            RemoteAnnouncementsText = null,
            RemoteAnnouncementText = null,
            LatestAnnouncementVersionRaw = null,
            LatestAnnouncementText = null,
            Error = "Update check is disabled in open-source build.",
            PostUri = DisabledUpdateUri,
        });
    }
}
