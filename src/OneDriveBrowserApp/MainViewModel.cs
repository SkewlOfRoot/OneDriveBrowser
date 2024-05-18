using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Text.Json;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Graph.Models;
using OneDriveBrowserApp.Extensions;

namespace OneDriveBrowserApp;

public partial class MainViewModel : ObservableObject
{
    private readonly IGraphClientWrapper _graphClientWrapper;
    private readonly IFileMatcher _fileMatcher;
    private readonly IFolderCache _folderCache;
    private readonly IThumbnailCache _thumbnailCache;
    private readonly ILogFileWriter _logFileWriter;
    private readonly List<string> _breadcrumbFileItemIds = [];
    
    [ObservableProperty] 
    private double _potentiallyClaimableSpace;

    [ObservableProperty] 
    private int _totalNumberOfFolders;

    [ObservableProperty]
    private int _currentNumberOfProcessedFolders;
    
    public MainViewModel(IGraphClientWrapper graphClientWrapper, IFileMatcher fileMatcher, IFolderCache folderCache, IThumbnailCache thumbnailCache, ILogFileWriter logFileWriter)
    {
        _graphClientWrapper = graphClientWrapper ?? throw new ArgumentNullException(nameof(graphClientWrapper));
        _fileMatcher = fileMatcher ?? throw new ArgumentNullException(nameof(fileMatcher));
        _folderCache = folderCache ?? throw new ArgumentNullException(nameof(folderCache));
        _thumbnailCache = thumbnailCache ?? throw new ArgumentNullException(nameof(thumbnailCache));
        _logFileWriter = logFileWriter ?? throw new ArgumentNullException(nameof(logFileWriter));

        HomeCommand = new AsyncRelayCommand(HomeExecute);
        BackCommand = new AsyncRelayCommand(BackExecute, BackCanExecute);
        OpenFileItemCommand = new AsyncRelayCommand<FileItem>(OpenFileItemExecute);
        FindMatchingMediaFilesCommand = new AsyncRelayCommand(FindMatchingMediaFilesExecute);
    }

    public ObservableCollection<FileItem> FileItems { get; } = [];
    public ObservableCollection<List<MatchedFileItem>> MatchingMediaFiles { get; } = [];
    public IAsyncRelayCommand HomeCommand { get; }
    public IAsyncRelayCommand BackCommand { get; }
    public IAsyncRelayCommand<FileItem> OpenFileItemCommand { get; }
    public IAsyncRelayCommand FindMatchingMediaFilesCommand { get; }

    public async Task Initialize()
    {
        await LoadFileItems("Root");
    }

    private bool BackCanExecute()
    {
        return _breadcrumbFileItemIds.Count > 1;
    }

    private async Task HomeExecute()
    {
        _breadcrumbFileItemIds.Clear();
        await LoadFileItems("Root");
    }

    private async Task BackExecute()
    {
        _breadcrumbFileItemIds.Remove(_breadcrumbFileItemIds.Last());
        await LoadFileItems(_breadcrumbFileItemIds.Last());
    }

    private async Task OpenFileItemExecute(FileItem? fileItem)
    {
        if (fileItem is not { Type: FileType.Folder })
        {
            return;
        }

        await LoadFileItems(fileItem.Id);
    }

    private async Task LoadFileItems(string driveItemId)
    {
        var children = await _graphClientWrapper.GetChildren(driveItemId);
        FileItems.Clear();
        foreach (var child in children)
        {
            FileItems.Add(new FileItem(child));
        }

        if(_breadcrumbFileItemIds.LastOrDefault() != driveItemId)
        {
            _breadcrumbFileItemIds.Add(driveItemId);
            BackCommand.NotifyCanExecuteChanged();
        }
    }

    private async Task FindMatchingMediaFilesExecute()
    {
        _logFileWriter.ClearLogFile();
        //const string id = "407BC162447A855D!84734"; // "OneDriveBrowserTest" folder ID

        var driveItems = new List<DriveItem>();

        var folders = await GetAllFolders();
        TotalNumberOfFolders = folders.Count;
        
        //var folderItems = FileItems.Where(x => x.Type == FileType.Folder && !_folderExcludeList.Contains(x.Name));
        foreach (var folder in folders)
        {
            Debug.WriteLine($"Searching in folder '{folder.Name}'");
            driveItems.AddRange(await _graphClientWrapper.GetMediaDriveItems(folder.Id));
            CurrentNumberOfProcessedFolders += 1;
            //driveItems.AddRange(await GetMediaDriveItems(id));
        }

        var matchSets = _fileMatcher.Match(driveItems);

        MatchingMediaFiles.Clear();

        foreach (var matchSet in matchSets)
        {
            var items = new List<MatchedFileItem>();
            foreach (var driveItem in matchSet)
            {
                //if (!_thumbnailCache.TryGet(driveItem.File.Hashes.Sha1Hash, out var thumbnailContent))
                //{
                //    thumbnailContent = await _graphClientWrapper.GetThumbnailContent(driveItem.Id);
                //    if (thumbnailContent.Length > 0)
                //    {
                //        await _thumbnailCache.AddAsync(driveItem.File.Hashes.Sha1Hash, thumbnailContent);
                //    }
                //}

                items.Add(new MatchedFileItem(new FileItem(driveItem)));
            }

            var potentialSizeClaimPerSetMb = Math.Round((double) (matchSet.First().Size * (matchSet.Count - 1) / 1000) / 1000, 1);
            PotentiallyClaimableSpace += potentialSizeClaimPerSetMb;

            WriteJsonToLogFile(items);
            MatchingMediaFiles.Add(items);
        }

        await LoadThumbnails();
    }

    private async Task LoadThumbnails()
    {
        foreach (var matchingMediaFileSet in MatchingMediaFiles)
        {
            var driveItem = matchingMediaFileSet.First().FileItem.DriveItem;

            if (!_thumbnailCache.TryGet(driveItem.File.Hashes.Sha1Hash, out var thumbnailContent))
            {
                thumbnailContent = await _graphClientWrapper.GetThumbnailContent(driveItem.Id);
                if (thumbnailContent.Length > 0)
                {
                    await _thumbnailCache.AddAsync(driveItem.File.Hashes.Sha1Hash, thumbnailContent);
                }
            }

            foreach (var matchedFileItem in matchingMediaFileSet)
            {
                matchedFileItem.SetThumbnailContent(thumbnailContent);
            }
        }
    }

    private async Task<List<FolderItem>> GetAllFolders()
    {
        var cachedFolders = await _folderCache.GetFolders();
        if (cachedFolders.Any())
        {
            return cachedFolders;
        }

        var folders = new List<FolderItem>();
        
        foreach (var fileItem in FileItems.Where(x => x.DriveItem.IsFolder() && !Constants.FolderExcludeList.Contains(x.Name)))
        {
            if(fileItem.Id == null) continue;
            Debug.WriteLine($"Processing folder '{fileItem.Name}'");
            var driveItems = await _graphClientWrapper.GetFolders(fileItem.Id);
            folders.AddRange(driveItems.Select(x => new FolderItem(x.Id, x.Name)));
        }

        await _folderCache.AddFolders(folders);
        return folders;
    }

    private async Task WriteJsonToLogFile(List<MatchedFileItem> items)
    {
        var json = JsonSerializer.Serialize(items);

        var text = $"---Set start---{Environment.NewLine}{json}{Environment.NewLine}---Set end---";

        await _logFileWriter.AppendLine(text);
    }
}