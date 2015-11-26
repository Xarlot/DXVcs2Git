using System.Linq;
using DevExpress.Mvvm.UI.Interactivity;
using DevExpress.Xpf.Grid;
using DXVcs2Git.UI.ViewModels;

namespace DXVcs2Git.UI.Behaviors {
    public class RepositoriesBindingBehavior : Behavior<GridControl> {
        protected override void OnAttached() {
            base.OnAttached();
            AssociatedObject.CurrentItemChanged += AssociatedObjectOnCurrentItemChanged;
        }
        void AssociatedObjectOnCurrentItemChanged(object sender, CurrentItemChangedEventArgs e) {
            UpdateSelection();
        }
        void UpdateSelection() {
            var model = AssociatedObject.DataContext as EditRepositoriesViewModel;
            if (model == null)
                return;
            var treeList = (TreeListView)AssociatedObject.View;
            var node = treeList.GetNodeByRowHandle(treeList.FocusedRowHandle);
            if (node.HasChildren) {
                var repository = (RepositoryViewModel)node.Content;
                model.SelectedRepository = repository;
                repository.SelectedBranch = repository.Branches.FirstOrDefault();
                repository.Refresh();
            }
            else {
                var branch = node.Content as BranchViewModel;
                if (branch != null) {
                    var repository = (RepositoryViewModel)node.ParentNode.Content;
                    model.SelectedRepository = repository;
                    repository.SelectedBranch = repository.Branches.FirstOrDefault();
                    repository.Refresh();
                }
                else {
                    var repository = (RepositoryViewModel)node.Content;
                    model.SelectedRepository = repository;
                    repository.SelectedBranch = null;
                    repository.Refresh();
                }
            }
            model.ForceParentRefresh();
        }
        protected override void OnDetaching() {
            AssociatedObject.CurrentItemChanged -= AssociatedObjectOnCurrentItemChanged;
            base.OnDetaching();
        }
    }
}
