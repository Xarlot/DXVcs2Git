using System;
using System.Globalization;
using System.Windows.Data;

namespace DXVcs2Git.UI.Views {
    public class EditBranchDescriptionConverter : IMultiValueConverter {
        public string StringFormat { get; set; }

        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture) {
            if(values.Length != 2 || String.IsNullOrEmpty(values[0] as string) || String.IsNullOrEmpty(values[1] as string))
                return string.Empty;
            return String.Format(StringFormat, values[0], values[1]);
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture) {
            throw new NotImplementedException();
        }
    }
}
