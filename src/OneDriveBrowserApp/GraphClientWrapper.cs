using System.Diagnostics;
using System.Net.Http;
using Microsoft.Graph;
using Microsoft.Graph.Models;
using OneDriveBrowserApp.Authentication;
using OneDriveBrowserApp.Exceptions;
using OneDriveBrowserApp.Extensions;

namespace OneDriveBrowserApp;

public interface IGraphClientWrapper
{
    Task<List<DriveItem>> GetChildren(string itemId);
    Task<byte[]> GetThumbnailContent(string itemId);
    Task<List<DriveItem>> GetFolders(string itemId);
    Task<IEnumerable<DriveItem>> GetMediaDriveItems(string folderId);
    Task DeleteDriveItem(string itemId);
}

public class GraphClientWrapper : IGraphClientWrapper
{
    private readonly HttpClient _httpClient;
    private readonly ILogFileWriter _logFileWriter;
    private readonly GraphServiceClient _graphServiceClient;
    private string? _userId;
    
    public GraphClientWrapper(HttpClient httpClient, ILogFileWriter logFileWriter)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _logFileWriter = logFileWriter ?? throw new ArgumentNullException(nameof(logFileWriter));

        var scopes = new[] { "User.Read", "Files.Read", "Files.ReadWrite" };

        var credentialProvider = new InteractiveAuthenticationProvider();
        var tokenCredential = credentialProvider.GetCredential();
        _graphServiceClient = new GraphServiceClient(tokenCredential, scopes);
    }

    private async Task<string> GetUserId()
    {
        if (_userId != null) return _userId;
        var driveResponse = await _graphServiceClient.Me.Drive.GetAsync().ConfigureAwait(false);

        _userId = driveResponse?.Id ?? throw new UserIdNullException();
        return _userId;
    }

    public async Task<List<DriveItem>> GetChildren(string itemId)
    {
        var userId = await GetUserId();
        try
        {
            var response = await _graphServiceClient.Drives[userId].Items[itemId].Children.GetAsync().ConfigureAwait(false);
            return response?.Value ?? [];
        }
        catch (Exception ex)
        {
            await _logFileWriter.AppendLine(ex.ToString());
        }

        return [];
    }

    public async Task<byte[]> GetThumbnailContent(string itemId)
    {
        var userId = await GetUserId();
        var thumbnailResponse = await _graphServiceClient.Drives[userId].Items[itemId].Thumbnails.GetAsync().ConfigureAwait(false);
        if (thumbnailResponse?.Value == null || thumbnailResponse.Value.Count == 0)
        {
            return [];
        }
        var contentUrl = thumbnailResponse.Value.First().Medium.Url;
        try
        {
            var content = await _httpClient.GetByteArrayAsync(contentUrl);

            return content;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"ItemId: {itemId} ContentUrl: {contentUrl}");
            Debug.WriteLine(ex);
            return [];
        }
    }

    public async Task<List<DriveItem>> GetFolders(string itemId)
    {
        var userId = await GetUserId();
        var response = await _graphServiceClient.Drives[userId].Items[itemId].Children.GetAsync().ConfigureAwait(false);
        var items = response?.Value ?? [];

        var folders = new List<DriveItem>(items.Where(x => x.IsFolder() && !Constants.FolderExcludeList.Contains(x.Name)));
        var subFolders = new List<DriveItem>();
        foreach (var folder in folders)
        {
            if (folder.Id == null) continue;
            Debug.WriteLine($"Processing folder '{folder.Name}'");
            subFolders.AddRange(await GetFolders(folder.Id));
        }
        folders.AddRange(subFolders);
        return folders;
    }

    public async Task<IEnumerable<DriveItem>> GetMediaDriveItems(string folderId)
    {
        var driveItems = await GetChildren(folderId);
        return driveItems.Where(x => x.ItemType() == FileType.Image || x.ItemType() == FileType.Video).ToList();
    }

    public async Task DeleteDriveItem(string itemId)
    {
        var userId = await GetUserId();
        await _graphServiceClient.Drives[userId].Items[itemId].DeleteAsync().ConfigureAwait(false);
    }
}