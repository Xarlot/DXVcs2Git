using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using DevExpress.Data.Filtering;

namespace DXVcs2Git.UI.Converters {
    public class TestsLogPassedToCriteriaConverter : IValueConverter {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
            bool passed = (bool)value;
            return passed ? null : CriteriaOperator.Parse("[Passed] = false");
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
            throw new NotImplementedException();
        }
    }
}
