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
        readonly IDisposable repositoryStateDisposable;
        RepositoryModelState repositoryState;
        ObservableCollection<RepositoryBranchViewModel> branches;
        string name;

        public RepositoryModelState State {
            get => this.repositoryState;
            private set => this.RaiseAndSetIfChanged(ref this.repositoryState, value);
        }
        public string Name {
            get => this.name;
            private set => this.RaiseAndSetIfChanged(ref this.name, value);
        }

        public ObservableCollection<RepositoryBranchViewModel> Branches {
            get => this.branches;
            private set => this.RaiseAndSetIfChanged(ref this.branches, value);
        }
        public IRepositoryModel Model { get; }
        public RepositoryViewModel(IRepositoryModel repository) {
            Model = repository;
            this.name = repository.Name;
            this.repositoryStateDisposable = repository.RepositoryStateObservable
                .SubscribeOn(DispatcherScheduler.Current)
                .Subscribe(HandleRepositoryStateChanged);
        }
        void HandleRepositoryStateChanged(RepositoryModelState state) {
            State = state;
            Name = Model.Name;
            Branches = this.repositoryState == RepositoryModelState.Initialized 
                ? new ObservableCollection<RepositoryBranchViewModel>(Model.Branches.Select(x => new RepositoryBranchViewModel(Model, x))) 
                : new ObservableCollection<RepositoryBranchViewModel>();
        }
        public void Dispose() {
            this.repositoryStateDisposable?.Dispose();
        }
    }
}