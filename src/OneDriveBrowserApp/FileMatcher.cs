using Microsoft.Graph.Models;
using OneDriveBrowserApp.Extensions;

namespace OneDriveBrowserApp;

public interface IFileMatcher
{
    List<HashSet<DriveItem>> Match(List<DriveItem> driveItems);
}

public class FileMatcher : IFileMatcher
{
    public List<HashSet<DriveItem>> Match(List<DriveItem> driveItems)
    {
        var matchSets = new List<HashSet<DriveItem>>();

        //var images = new List<DriveItem>();
        //var videos = new List<DriveItem>();
        var dictionary = new Dictionary<string, HashSet<DriveItem>>();

        foreach (var driveItem in driveItems)
        {
            if (!dictionary.TryAdd(driveItem.File.Hashes.Sha1Hash, [driveItem]))
            {
                dictionary[driveItem.File.Hashes.Sha1Hash].Add(driveItem);
            }

            //if (driveItem.IsImage())
            //{
            //    images.Add(driveItem);
            //}
            //else if (driveItem.IsVideo())
            //{
            //    videos.Add(driveItem);
            //}
        }
        matchSets.AddRange(dictionary.Values.Where(x => x.Count > 1));
        //matchSets.AddRange(MatchImages(images));
        //matchSets.AddRange(MatchVideos(videos));
        return matchSets;
    }

    //private IEnumerable<HashSet<DriveItem>> MatchImages(List<DriveItem> images)
    //{
    //    var dictionary = new Dictionary<string, HashSet<DriveItem>>();

    //    foreach (var driveItem in images)
    //    {
    //        if (!dictionary.TryAdd(driveItem.File.Hashes.Sha1Hash, [driveItem]))
    //        {
    //            dictionary[driveItem.File.Hashes.Sha1Hash].Add(driveItem);
    //        }
    //    }

    //    return dictionary.Values.Where(x => x.Count > 1);
    //}

    //private IEnumerable<HashSet<DriveItem>> MatchVideos(List<DriveItem> videos)
    //{
        
    //}
}