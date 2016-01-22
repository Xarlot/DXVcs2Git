using System.Linq;
using DevExpress.Mvvm.UI.Interactivity;
using DevExpress.Xpf.Grid;
using DXVcs2Git.UI.ViewModels;
using DevExpress.Mvvm;
using System;
using DevExpress.Xpf.Core;

namespace DXVcs2Git.UI.Behaviors {
    public class RepositoriesBindingBehavior : Behavior<GridControl> {
        readonly Locker selectionLocker = new Locker();
        int focusedHandle = 0;
        public RepositoriesBindingBehavior() {
            selectionLocker.Unlocked += OnSelectionUnlocked;
        }
        protected override void OnAttached() {
            base.OnAttached();
            AssociatedObject.CurrentItemChanged += AssociatedObjectOnCurrentItemChanged;            
            Messenger.Default.Register<Message>(this, new Action<Message>(OnMessageReceived));
        }

        void OnSelectionUnlocked(object sender, EventArgs e) {            
            AssociatedObject.View.FocusedRowHandle = focusedHandle;
        }

        void OnMessageReceived(Message message) {
            if (message.MessageType == MessageType.BeginUpdate) {
                focusedHandle = AssociatedObject?.View.FocusedRowHandle ?? 0;
                selectionLocker.Lock();
            }                
            if (message.MessageType == MessageType.Update)
                selectionLocker.Unlock();
        }
        void AssociatedObjectOnCurrentItemChanged(object sender, CurrentItemChangedEventArgs e) {
            selectionLocker.DoIfNotLocked(UpdateSelection);
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
                    repository.SelectedBranch = branch;
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

            Messenger.Default.Unregister(this);
            base.OnDetaching();
        }
    }
}
