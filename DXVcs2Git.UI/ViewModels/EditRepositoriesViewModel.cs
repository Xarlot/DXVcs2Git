using System;
using System.Collections.Generic;
using System.Linq;
using DevExpress.Mvvm;
using Microsoft.Practices.ServiceLocation;

namespace DXVcs2Git.UI.ViewModels {
    public class EditRepositoriesViewModel : ViewModelBase {
        RepositoriesViewModel RepositoriesViewModel => ServiceLocator.Current.GetInstance<RepositoriesViewModel>();

        public bool IsInitialized {
            get { return GetProperty(() => IsInitialized); }
            private set { SetProperty(() => IsInitialized, value); }
        }

        public IEnumerable<EditRepositoryItem> Items {
            get { return GetProperty(() => Items); }
            private set { SetProperty(() => Items, value); }
        }

        public EditRepositoryItem SelectedItem {
            get { return GetProperty(() => SelectedItem); }
            set { SetProperty(() => SelectedItem, value, SelectedItemChanged); }
        }
        public EditRepositoriesViewModel() {
            Messenger.Default.Register<Message>(this, OnMessageReceived);
            Initialize();
        }
        void Initialize() {
            Items = new List<EditRepositoryItem>();
            IsInitialized = true;
        }

        void OnMessageReceived(Message message) {
            if (message.MessageType == MessageType.BeforeUpdate)
                BeforeUpdate();
            if (message.MessageType == MessageType.Update)
                Update();
        }
        void Update() {
            PerformUpdate();
            IsInitialized = true;
            UpdateSelectedItem();
        }
        void BeforeUpdate() {
            IsInitialized = false;
        }
        void PerformUpdate() {
            var selectedItem = SelectedItem;
            Items = CreateItems();
            SelectedItem = Items.FirstOrDefault(x => object.Equals(x, selectedItem));
        }
        IEnumerable<EditRepositoryItem> CreateItems() {
            var repositoryItems = new List<RepositoryItem>();
            foreach (var repository in RepositoriesViewModel.Repositories) {
                var branches = repository.Branches.Select(x => new BranchRepositoryItem(x)).ToList();
                var repositoryItem = new RepositoryItem(repository, branches);
                repositoryItems.Add(repositoryItem);
            }
            var root = new RootRepositoryItem(RepositoriesViewModel, repositoryItems);
            return new[] { root };
        }
        bool CanUpdate() {
            return IsInitialized;
        }
        public void Refresh() {
        }
        void SelectedItemChanged() {
            if (IsInitialized)
                UpdateSelectedItem();
        }
        void UpdateSelectedItem() {
            if (SelectedItem == null || SelectedItem.ItemType == EditRepositoryItemType.Root) {
                RepositoriesViewModel.SelectedRepository = null;
                RepositoriesViewModel.SelectedBranch = null;
                return;
            }
            if (SelectedItem.ItemType == EditRepositoryItemType.Repository) {
                var repositoryItem = (RepositoryItem)SelectedItem;
                var repositoryViewModel = repositoryItem.Item;
                RepositoriesViewModel.SelectedRepository = repositoryViewModel;
                RepositoriesViewModel.SelectedBranch = repositoryViewModel.Branches.FirstOrDefault();
                return;
            }
            if (SelectedItem.ItemType == EditRepositoryItemType.Branch) {
                var branchItem = (BranchRepositoryItem)SelectedItem;
                var branchViewModel = branchItem.Item;
                RepositoriesViewModel.SelectedRepository = branchViewModel.Repository;
                RepositoriesViewModel.SelectedBranch = branchViewModel;
                return;
            }
            throw new ArgumentException("SelectedItem");
        }
    }

    public enum EditRepositoryItemType {
        Root,
        Repository,
        Branch,
    }

    public abstract class EditRepositoryItem {
        public EditRepositoryItemType ItemType { get; }

        protected bool Equals(EditRepositoryItem other) {
            return item.Equals(other.item);
        }
        public override bool Equals(object obj) {
            if (ReferenceEquals(null, obj))
                return false;
            if (ReferenceEquals(this, obj))
                return true;
            if (obj.GetType() != this.GetType())
                return false;
            return Equals((EditRepositoryItem)obj);
        }
        public override int GetHashCode() {
            return item.GetHashCode();
        }
        readonly object item;
        protected EditRepositoryItem(EditRepositoryItemType itemType, object item) {
            ItemType = itemType;
            this.item = item;
        }
    }
    public abstract class EditRepositoryItem<T, U> : EditRepositoryItem {
        public abstract string Name { get; }
        public T Item { get; }
        public IEnumerable<U> Children { get; }

        protected EditRepositoryItem(EditRepositoryItemType itemType, T item, IEnumerable<U> children) : base(itemType, item){
            Item = item;
            Children = children;
        }
    }

    public class RootRepositoryItem : EditRepositoryItem<RepositoriesViewModel, RepositoryItem> {
        public override string Name => "Root";
        public RootRepositoryItem(RepositoriesViewModel item, IEnumerable<RepositoryItem> children) : base(EditRepositoryItemType.Root, item, children) {
        }
    }

    public class RepositoryItem : EditRepositoryItem<RepositoryViewModel, BranchRepositoryItem> {
        public override string Name => Item.Name;
        public RepositoryItem(RepositoryViewModel item, IEnumerable<BranchRepositoryItem> children) : base(EditRepositoryItemType.Repository, item, children) {
        }
    }

    public class BranchRepositoryItem : EditRepositoryItem<BranchViewModel, object> {
        public override string Name => Item.Name;
        public BranchRepositoryItem(BranchViewModel item) : base(EditRepositoryItemType.Branch, item, Enumerable.Empty<object>()) {
            
        }
    }
}
