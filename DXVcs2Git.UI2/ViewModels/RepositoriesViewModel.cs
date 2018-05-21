using System;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using DevExpress.Mvvm.Native;
using DXVcs2Git.UI2.Core;
using ReactiveUI;

namespace DXVcs2Git.UI2.ViewModels {
    public class RepositoriesViewModel : ReactiveObject, IDisposable {
        readonly IRepositoriesStorage repositoriesStorage;
        readonly IBranchSelector branchSelector;
        readonly IDisposable repositoriesDisposable;
        ReadOnlyObservableCollection<RepositoryViewModel> repositories = new ReadOnlyObservableCollection<RepositoryViewModel>(
            new ObservableCollection<RepositoryViewModel>());
        
        public ReadOnlyObservableCollection<RepositoryViewModel> Repositories {
            get => this.repositories;
            private set => this.RaiseAndSetIfChanged(ref this.repositories, value);
        }

        public RepositoryBranchViewModel SelectedBranch {
            get => this.selectedBranch;
            set => this.RaiseAndSetIfChanged(ref this.selectedBranch, value);
        }

        RepositoryBranchViewModel selectedBranch;
        
        public RepositoriesViewModel(IRepositoriesStorage repositoriesStorage, IBranchSelector branchSelector) {
            this.repositoriesStorage = repositoriesStorage;
            this.branchSelector = branchSelector;
            this.repositoriesDisposable = repositoriesStorage.RepositoriesObservable
                .SubscribeOn(DispatcherScheduler.Current).Subscribe(HandleRepositoriesChanged);

            this.WhenAnyValue(x => x.SelectedBranch).Subscribe(HandleSelectedBranchChanged);
        }
        void HandleSelectedBranchChanged(RepositoryBranchViewModel branchViewModel) {
            this.branchSelector.Select(branchViewModel?.Branch);
        }
        void HandleRepositoriesChanged(ImmutableArray<IRepositoryModel> changed) {
            foreach (IDisposable repositoryViewModel in Repositories)
                repositoryViewModel.Dispose();

            Repositories = changed.Select(x => new RepositoryViewModel(x)).ToReadOnlyObservableCollection();
        }
        public void Dispose() {
            this.repositoriesDisposable?.Dispose();
        }
    }
}