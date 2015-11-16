using System.Linq;
using System.Windows.Input;
using DevExpress.Mvvm;
using DevExpress.Mvvm.Native;
using NGitLab.Models;

namespace DXVcs2Git.UI.ViewModels {
    public class BranchViewModel : BindableBase {
        public Branch Branch { get; }
        public MergeRequestsViewModel MergeRequests { get; }
        public string Name { get; }

        public ICommand CreateNewMergeRequestCommand { get; private set; }
        public MergeRequestViewModel MergeRequest { get; private set; }
        public EditMergeRequestViewModel EditMergeRequest {
            get { return GetProperty(() => EditMergeRequest); }
            private set { SetProperty(() => EditMergeRequest, value); }
        }
        public BranchViewModel(MergeRequestsViewModel mergeRequests, Branch branch) {
            Branch = branch;
            Name = branch.Name;
            MergeRequests = mergeRequests;
            MergeRequest = mergeRequests.MergeRequests.FirstOrDefault(x => x.SourceBranch == branch.Name);

            CreateNewMergeRequestCommand = DelegateCommandFactory.Create<MergeRequestsViewModel>(CreateNewMergeRequest, CanCreateNewMergeRequest);
        }
        bool CanCreateNewMergeRequest(MergeRequestsViewModel model) {
            return MergeRequest == null && EditMergeRequest == null;
        }
        public void CreateNewMergeRequest(MergeRequestsViewModel model) {
            EditMergeRequest = new EditMergeRequestViewModel(this);
        }
    }
}
