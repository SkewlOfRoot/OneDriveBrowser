using System.IO;
using System.Text.Json;

namespace OneDriveBrowserApp;

public interface IFolderCache
{
    Task<List<FolderItem>> GetFolders();
    Task AddFolders(List<FolderItem> folders);
}

public class FolderCache : IFolderCache
{
    private List<FolderItem>? _folders;

    public FolderCache()
    {
        if (!File.Exists(Constants.FolderCacheFileName))
        {
            Directory.CreateDirectory(Path.GetDirectoryName(Constants.FolderCacheFileName));
        }   
    }

    public async Task<List<FolderItem>> GetFolders()
    {
        if (_folders == null)
        {
            if (File.Exists(Constants.FolderCacheFileName))
            {
                var text = await File.ReadAllTextAsync(Constants.FolderCacheFileName);
                _folders = JsonSerializer.Deserialize<List<FolderItem>>(text) ?? [];
            }
            else
            {
                _folders = [];
            }
        }

        return _folders;
    }

    public async Task AddFolders(List<FolderItem> folders)
    {
        ArgumentNullException.ThrowIfNull(folders);

        var json = JsonSerializer.Serialize(folders);
        await File.WriteAllTextAsync(Constants.FolderCacheFileName, json);
    }
}