using System.Text.Json;

namespace Nioh3AffixEditor.Services;

public sealed class UpdateCacheStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
    };

    private readonly string _cachePath;

    public UpdateCacheStore(string cachePath)
    {
        _cachePath = cachePath ?? throw new ArgumentNullException(nameof(cachePath));
    }

    public UpdateCache Load()
    {
        try
        {
            if (!System.IO.File.Exists(_cachePath))
            {
                return new UpdateCache();
            }

            var json = System.IO.File.ReadAllText(_cachePath);
            return JsonSerializer.Deserialize<UpdateCache>(json, JsonOptions) ?? new UpdateCache();
        }
        catch
        {
            return new UpdateCache();
        }
    }

    public void Save(UpdateCache cache)
    {
        try
        {
            var cacheDir = System.IO.Path.GetDirectoryName(_cachePath);
            if (!string.IsNullOrWhiteSpace(cacheDir))
            {
                System.IO.Directory.CreateDirectory(cacheDir);
            }

            var json = JsonSerializer.Serialize(cache, JsonOptions);
            System.IO.File.WriteAllText(_cachePath, json);
        }
        catch
        {
            // Best-effort cache only.
        }
    }
}
