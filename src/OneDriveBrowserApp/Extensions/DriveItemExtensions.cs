using Microsoft.Graph.Models;

namespace OneDriveBrowserApp.Extensions;

public static class DriveItemExtensions
{
    public static bool IsFolder(this DriveItem item)
    {
        return item.Folder is not null;
    }

    public static bool IsImage(this DriveItem item)
    {
        return item.Image is not null;
    }

    public static bool IsVideo(this DriveItem item)
    {
        return item.Video is not null;
    }

    public static bool IsExcel(this DriveItem item)
    {
        return item.File is {MimeType: not null} && item.File.MimeType.Contains("openxmlformats-officedocument.spreadsheetml.sheet");
    }

    public static FileType ItemType(this DriveItem item)
    {
        if(item.IsFolder())
            return FileType.Folder;
        if (item.IsImage())
            return FileType.Image;
        if (item.IsVideo())
            return FileType.Video;
        if(item.IsExcel())
            return FileType.Excel;
        return FileType.Unknown;
    }
}