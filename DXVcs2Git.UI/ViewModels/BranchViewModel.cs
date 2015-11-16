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

        public BranchViewModel(MergeRequestsViewModel mergeRequests, Branch branch) {
            Branch = branch;
            Name = branch.Name;
            MergeRequests = mergeRequests;

            CreateNewMergeRequestCommand = DelegateCommandFactory.Create<MergeRequestsViewModel>(CreateNewMergeRequest, CanCreateNewMergeRequest);
        }
        bool CanCreateNewMergeRequest(MergeRequestsViewModel model) {
            if (model == null)
                return false;
            return model.MergeRequests.All(x => x.MergeRequest.SourceBranch != Branch.Name);
        }
        void CreateNewMergeRequest(MergeRequestsViewModel model) {
            NewMergeRequestViewModel newMergeRequest = new NewMergeRequestViewModel(this);
            MergeRequests.AddMergeRequest(newMergeRequest);
        }
    }
}
