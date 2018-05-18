using System;
using System.Linq;
using System.Windows;
using System.Windows.Interactivity;
using DevExpress.Xpf.Grid;
using DXVcs2Git.UI2.Core;
using DXVcs2Git.UI2.ViewModels;
using ReactiveUI.Blend;

namespace DXVcs2Git.UI2.Behaviors {

    public class TestBehavior : FollowObservableStateBehavior {
        protected override void OnAttached() {
            base.OnAttached();
        }
    }
    
    public class RepositoriesBranchBindingBehavior : Behavior<GridControl> {
        public static readonly DependencyProperty SelectedBranchProperty = DependencyProperty.Register(
            "SelectedBranch", typeof(RepositoryBranchViewModel), typeof(RepositoriesBranchBindingBehavior), 
            new PropertyMetadata(null, (o, args) => ((RepositoriesBranchBindingBehavior)o).SelectedBranchChanged((RepositoryBranchViewModel)args.NewValue)));

        public RepositoryBranchViewModel SelectedBranch {
            get => (RepositoryBranchViewModel)GetValue(SelectedBranchProperty);
            set => SetValue(SelectedBranchProperty, value);
        }

        IDisposable repositoryDisposable;
        IRepositoryModel Repository { get; set; }

        protected override void OnAttached() {
            base.OnAttached();
            AssociatedObject.CurrentItemChanged += AssociatedObjectOnCurrentItemChanged;
        }
        protected override void OnDetaching() {
            base.OnDetaching();
            AssociatedObject.CurrentItemChanged -= AssociatedObjectOnCurrentItemChanged;
        }
        void AssociatedObjectOnCurrentItemChanged(object sender, CurrentItemChangedEventArgs e) {
            var selectedItem = e.NewItem;
            if (selectedItem is RepositoryViewModel repo) {
                Repository = repo.Model;
                if (repo.State == RepositoryModelState.Initialized) {
                    var branch = repo.Branches.First();
                    SelectedBranch = branch;
                }
            }
            else if (selectedItem is RepositoryBranchViewModel bm) {
                Repository = bm.Repository;
                SelectedBranch = bm;
            }
        }
        void SelectedBranchChanged(RepositoryBranchViewModel newValue) {
            
        }
    }
}