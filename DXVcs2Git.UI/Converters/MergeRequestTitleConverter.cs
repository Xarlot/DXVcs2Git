using System;
using System.Globalization;
using System.Linq;
using System.Windows.Data;
using DevExpress.Mvvm.Native;

namespace DXVcs2Git.UI.Views {
    public class MergeRequestTitleConverter : IValueConverter{
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
            string title = value.With(x => x.ToString());
            var candidate = title.With(x => title.Split(new[] { '\n'}, StringSplitOptions.RemoveEmptyEntries).FirstOrDefault());
            var result = candidate.With(x => x.Replace('\r', ' '));
            return result;
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
            throw new NotImplementedException();
        }
    }
}
