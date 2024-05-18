using System.Globalization;
using System.IO;
using System.Windows.Data;
using System.Windows.Media.Imaging;

namespace OneDriveBrowserApp.Converters;

public class BytesToImageConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value == null)
            return null;

        if (!(value is byte[]))
            throw new ArgumentException("Only byte[] values are supported.");

        if (((byte[])value).Length == 0)
            return null;

        // return new ImageSourceConverter().ConvertFrom(value);
        BitmapImage image = new BitmapImage();
        using (MemoryStream ms = new MemoryStream((byte[])value))
        {
            ms.Position = 0;
            image.BeginInit();
            image.CreateOptions = BitmapCreateOptions.PreservePixelFormat;
            image.CacheOption = BitmapCacheOption.OnLoad;
            image.UriSource = null;
            image.StreamSource = ms;
            image.EndInit();
        }
        image.Freeze();

        return image;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}