using DevExpress.Mvvm;

namespace DXVcs2Git.UI.ViewModels {
    public class EditBranchViewModel : ViewModelBase {
        public EditBranchViewModel() {
            Messenger.Default.Register<Message>(this, OnMessageReceived);
        }
        void OnMessageReceived(Message msg) {

        }
    }
}
