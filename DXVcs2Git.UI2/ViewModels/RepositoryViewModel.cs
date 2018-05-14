using System;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using DXVcs2Git.UI2.Core;
using ReactiveUI;

namespace DXVcs2Git.UI2.ViewModels {
    public class RepositoryViewModel : ReactiveObject, IDisposable {
        readonly IRepository repository;
        readonly IDisposable repositoryStateDisposable;
        RepositoryState repositoryState;

        
        public RepositoryState RepositoryState {
            get => this.repositoryState;
            private set => this.RaiseAndSetIfChanged(ref this.repositoryState, value);
        }
        
        public RepositoryViewModel(IRepository repository) {
            this.repository = repository;

            this.repositoryStateDisposable = repository.RepositoryStateObservable
                .SubscribeOn(DispatcherScheduler.Current).Subscribe(HandleRepositoryStateChanged);
        }
        void HandleRepositoryStateChanged(RepositoryState state) {
            RepositoryState = state;
        }
        public void Dispose() {
            this.repositoryStateDisposable?.Dispose();
        }
    }
}