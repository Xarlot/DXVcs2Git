using DXVcs2Git.Core.GitLab;

namespace DXVcs2Git.UI.ViewModels {
    public class EditMergeRequestData {
        public string Comment { get; set; }
        public bool AssignToService { get; set; }
        public MergeRequestOptions Options { get; set; }
    }
}
