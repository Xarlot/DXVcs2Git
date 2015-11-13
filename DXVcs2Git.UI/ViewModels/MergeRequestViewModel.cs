using DevExpress.Mvvm;
using NGitLab.Models;

namespace DXVcs2Git.UI.ViewModels {
    public class MergeRequestViewModel : BindableBase {
        MergeRequest MergeRequest { get; }
        public MergeRequestViewModel(MergeRequest mergeRequest) {
            MergeRequest = mergeRequest;
        }
    }
}
