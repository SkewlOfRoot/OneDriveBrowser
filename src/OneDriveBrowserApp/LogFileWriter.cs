using System.IO;

namespace OneDriveBrowserApp;

public interface ILogFileWriter
{
    void ClearLogFile();
    Task AppendLine(string line);
}
public class LogFileWriter : ILogFileWriter
{
    public LogFileWriter()
    {
        if (!File.Exists(Constants.LogFileName))
        {
            Directory.CreateDirectory(Path.GetDirectoryName(Constants.LogFileName));
        }
    }

    public void ClearLogFile()
    {
        if (File.Exists(Constants.LogFileName))
        {
            File.Delete(Constants.LogFileName);
        }
    }

    public async Task AppendLine(string line)
    {
        await File.AppendAllLinesAsync(Constants.LogFileName, new[] {line});
    }
}