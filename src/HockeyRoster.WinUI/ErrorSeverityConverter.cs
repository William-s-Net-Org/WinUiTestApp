using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;

namespace HockeyRoster.WinUI;

public sealed class ErrorSeverityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language) =>
        value is true ? InfoBarSeverity.Error : InfoBarSeverity.Success;
    public object ConvertBack(object value, Type targetType, object parameter, string language) => throw new NotSupportedException();
}
