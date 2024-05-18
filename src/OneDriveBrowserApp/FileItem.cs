using Microsoft.Graph.Models;
using OneDriveBrowserApp.Extensions;
using System.Net;

namespace OneDriveBrowserApp;

public class FileItem
{
    public FileItem(DriveItem driveItem)
    {
        DriveItem = driveItem ?? throw new ArgumentNullException(nameof(driveItem));
    }

    public DriveItem DriveItem { get; }
    public string? Id => DriveItem.Id;
    public string Name => DriveItem.Name!;
    public string FullName => WebUtility.UrlDecode($"{DriveItem.ParentReference.Path.Replace("/drive/root:/", string.Empty)}/{DriveItem.Name}");
    public FileType Type => DriveItem.ItemType();
}

public record FolderItem(string Id, string Name);