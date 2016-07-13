using System.Collections.Generic;
using System.Linq;
using DevExpress.Mvvm;
using Microsoft.Practices.ServiceLocation;

namespace DXVcs2Git.UI.ViewModels {
    public class EditBranchChangesViewModel : ViewModelBase {
        RepositoriesViewModel Repositories => ServiceLocator.Current.GetInstance<RepositoriesViewModel>();
        BranchViewModel SelectedBranch => Repositories.SelectedBranch;

        public EditBranchChangesViewModel() {
            Messenger.Default.Register<Message>(this, OnMessageReceived);
            RefreshChanges();
        }

        public IEnumerable<MergeRequestFileDataViewModel> Changes {
            get { return GetProperty(() => Changes); }
            private set { SetProperty(() => Changes, value); }
        }

        void OnMessageReceived(Message msg) {
            if (msg.MessageType == MessageType.RefreshSelectedBranch) {
                RefreshChanges();
            }
        }
        void RefreshChanges() {
            Changes = SelectedBranch?.MergeRequest?.Changes ?? Enumerable.Empty<MergeRequestFileDataViewModel>();
        }
    }
}
