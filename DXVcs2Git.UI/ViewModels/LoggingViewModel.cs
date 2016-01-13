using System.Windows.Threading;
using DevExpress.Mvvm;
using DXVcs2Git.Core;

namespace DXVcs2Git.UI.ViewModels {
    public class LoggingViewModel : BindableBase {
        public string Text {
            get { return GetProperty(() => Text); }
            set { SetProperty(() => Text, value); }
        }
        public LoggingViewModel() {
            
        }
    }
}
