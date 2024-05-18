using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;

namespace OneDriveBrowserApp;

public partial class MatchedFileItem : ObservableObject
{
    private readonly IGraphClientWrapper _graphClientWrapper;
    private readonly ILogFileWriter _logFileWriter;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(RemoveFileCommand))]
    private bool _isRemoved;

    [ObservableProperty]
    private byte[] _thumbnailContent;

    public MatchedFileItem(FileItem fileItem)
    {
        _graphClientWrapper = App.Current.Services.GetRequiredService<IGraphClientWrapper>();
        _logFileWriter = App.Current.Services.GetRequiredService<ILogFileWriter>();

        FileItem = fileItem ?? throw new ArgumentNullException(nameof(fileItem));

        RemoveFileCommand = new AsyncRelayCommand(RemoveFileExecute, () => IsRemoved == false);
    }

    public IAsyncRelayCommand RemoveFileCommand { get; }

    public FileItem FileItem { get; }

    //[JsonIgnore] 
    //public byte[] ThumbnailContent { get; private set; } = [];

    public bool HasThumbnail { get; private set; }

    public void SetThumbnailContent(byte[] content)
    {
        ThumbnailContent = content;
        HasThumbnail = true;
    }

    private async Task RemoveFileExecute()
    {
        if (IsRemoved || FileItem.Id == null)
        {
            return;
        }

        var result = MessageBox.Show("Are you sure you want to remove the file from OneDrive?", "Remove file?", MessageBoxButton.YesNo, MessageBoxImage.Question);

        if (result == MessageBoxResult.Yes)
        {
            try
            {
                await _graphClientWrapper.DeleteDriveItem(FileItem.Id);
                IsRemoved = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Failed to delete media file from OneDrive.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                await _logFileWriter.AppendLine(ex.ToString());
            }
        }
    }
}