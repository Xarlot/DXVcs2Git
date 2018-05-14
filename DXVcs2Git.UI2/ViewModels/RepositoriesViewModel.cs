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
        readonly IDisposable repositoriesDisposable;
        ReadOnlyObservableCollection<RepositoryViewModel> repositories = new ReadOnlyObservableCollection<RepositoryViewModel>(
            new ObservableCollection<RepositoryViewModel>());
        
        public ReadOnlyObservableCollection<RepositoryViewModel> Repositories {
            get => this.repositories;
            private set => this.RaiseAndSetIfChanged(ref this.repositories, value);
        }
        
        public RepositoriesViewModel(IRepositoriesStorage repositoriesStorage) {
            this.repositoriesStorage = repositoriesStorage;
            
            this.repositoriesDisposable = repositoriesStorage.RepositoriesObservable
                .SubscribeOn(DispatcherScheduler.Current).Subscribe(HandleRepositoriesChanged);
        }
        void HandleRepositoriesChanged(ImmutableArray<IRepository> changed) {
            foreach (IDisposable repositoryViewModel in Repositories)
                repositoryViewModel.Dispose();

            Repositories = changed.Select(x => new RepositoryViewModel(x)).ToReadOnlyObservableCollection();
        }
        public void Dispose() {
            this.repositoriesDisposable?.Dispose();
        }
    }
}