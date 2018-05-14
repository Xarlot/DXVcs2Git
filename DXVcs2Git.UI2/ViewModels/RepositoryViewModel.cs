using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using DXVcs2Git.UI2.Core;
using DXVcs2Git.UI2.Views;
using ReactiveUI;

namespace DXVcs2Git.UI2.ViewModels {
    public class RepositoryViewModel : ReactiveObject, IDisposable {
        readonly IRepositoryModel repository;
        readonly IDisposable repositoryStateDisposable;
        RepositoryModelState repositoryState;
        ObservableCollection<BranchViewModel> branches;
        string name;

        public RepositoryModelState State {
            get => this.repositoryState;
            private set => this.RaiseAndSetIfChanged(ref this.repositoryState, value);
        }
        public string Name {
            get => this.name;
            private set => this.RaiseAndSetIfChanged(ref this.name, value);
        }

        public ObservableCollection<BranchViewModel> Branches {
            get => this.branches;
            private set => this.RaiseAndSetIfChanged(ref this.branches, value);
        }
        public RepositoryViewModel(IRepositoryModel repository) {
            this.repository = repository;
            this.name = repository.Name;
            this.repositoryStateDisposable = repository.RepositoryStateObservable
                .SubscribeOn(DispatcherScheduler.Current).Subscribe(HandleRepositoryStateChanged);
        }
        void HandleRepositoryStateChanged(RepositoryModelState state) {
            State = state;
            Name = this.repository.Name;
            Branches = this.repositoryState == RepositoryModelState.Initialized 
                ? new ObservableCollection<BranchViewModel>(this.repository.Branches.Select(x => new BranchViewModel(this.repository, x))) 
                : new ObservableCollection<BranchViewModel>();
        }
        public void Dispose() {
            this.repositoryStateDisposable?.Dispose();
        }
    }
}