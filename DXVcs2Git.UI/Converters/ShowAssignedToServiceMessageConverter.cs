using System;
using System.Globalization;
using System.Windows.Data;

namespace DXVcs2Git.UI.Converters {
    public class ShowAssignedToServiceMessageConverter : IValueConverter {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
            var performTesting = (bool)value;
            return performTesting ? "Accept merge request if build succeed" : "Accept merge request";
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
            throw new NotImplementedException();
        }
    }
}
