using System.Linq;
using DevExpress.Mvvm.UI.Interactivity;
using DevExpress.Xpf.Grid;
using DXVcs2Git.UI.ViewModels;
using DevExpress.Mvvm;
using System;
using DevExpress.Xpf.Core;
using DevExpress.Mvvm.UI.Native;
using System.Collections;
using System.Collections.Generic;
using System.Windows;

namespace DXVcs2Git.UI.Behaviors {
    public class RepositoriesBindingBehavior : Behavior<GridControl> {
        public static readonly DependencyProperty ItemsSourceProperty;        
        static RepositoriesBindingBehavior() {
            DependencyPropertyRegistrator<RepositoriesBindingBehavior>.New()
                .Register(x => x.ItemsSource, out ItemsSourceProperty, null, (x, oldValue, newValue) => x.OnItemsSourceChanged(oldValue, newValue));
        }

        void OnItemsSourceChanged(IEnumerable oldValue, IEnumerable newValue) {
            if (AssociatedObject == null)
                return;
            var focusedHandle = AssociatedObject?.View.FocusedRowHandle ?? 0;
            AssociatedObject.ItemsSource = newValue;
            if (focusedHandle < 0)
                return;
            AssociatedObject.View.FocusedRowHandle = focusedHandle;
        }

        public IEnumerable ItemsSource {
            get { return (IEnumerable)GetValue(ItemsSourceProperty); }
            set { SetValue(ItemsSourceProperty, value); }
        }
        protected override void OnAttached() {
            base.OnAttached();
            AssociatedObject.ItemsSource = ItemsSource;
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
            base.OnDetaching();
        }
    }
}
