using System.IO;

namespace OneDriveBrowserApp;

public interface IThumbnailCache
{
    bool TryGet(string key, out byte[] thumbnailContent);
    Task AddAsync(string key, byte[] content);
}

public class ThumbnailCache : IThumbnailCache
{
    private readonly Dictionary<string, byte[]> _thumbnailCache;

    public ThumbnailCache()
    {
        if (File.Exists(Constants.ThumbnailCacheFileName))
        {
            _thumbnailCache = LoadThumbnailCacheFromFile();
        }
        else
        {
            Directory.CreateDirectory(Path.GetDirectoryName(Constants.ThumbnailCacheFileName));
            _thumbnailCache = [];
        }
    }

    public bool TryGet(string key, out byte[] thumbnailContent)
    {
        return _thumbnailCache.TryGetValue(key, out thumbnailContent);
    }

    public async Task AddAsync(string key, byte[] content)
    {
        _thumbnailCache.Add(key, content);

        var line = $"{key},{Convert.ToBase64String(content)}";
        await File.AppendAllLinesAsync(Constants.ThumbnailCacheFileName, new[] {line});
    }

    private Dictionary<string, byte[]> LoadThumbnailCacheFromFile()
    {
        var lines = File.ReadAllLines(Constants.ThumbnailCacheFileName);

        var dictionary = new Dictionary<string, byte[]>();

        foreach (var line in lines)
        {
            var values = line.Split(",");
            dictionary.Add(values[0], Convert.FromBase64String(values[1]));
        }

        return dictionary;
    }
}