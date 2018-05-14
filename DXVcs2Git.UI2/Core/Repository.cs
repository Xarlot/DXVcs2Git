using System;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using DXVcs2Git.Core.Git;

namespace DXVcs2Git.UI2.Core {
    public enum RepositoryState {
        NotInitialized,
        Initializing,
        Initialized,
        Invalid
    }

    public interface IRepository {
        IObservable<RepositoryState> RepositoryStateObservable { get; }
        Task Initialize();
    }

    public class Repository : IRepository {
        readonly IRepositoriesStorage repositories;
        readonly string gitPath;
        
        readonly BehaviorSubject<RepositoryState> repositoryStateSubject = new BehaviorSubject<RepositoryState>(RepositoryState.NotInitialized);
        public IObservable<RepositoryState> RepositoryStateObservable => this.repositoryStateSubject.AsObservable();

        public RepositoryState RepositoryState {
            get => this.repositoryStateSubject.Value;
            set => this.repositoryStateSubject.OnNext(value);
        }
        public string Origin { get; private set; }
        public string Upstream { get; private set; }

        public Repository(IRepositoriesStorage repositories, string gitPath) {
            this.repositories = repositories;
            this.gitPath = gitPath;
        }

        public async Task Initialize() {
            try {
                RepositoryState = RepositoryState.Initializing;
                var (origin, upstream) = await Task.Run(() => {
                    GitReaderWrapper wrapper = new GitReaderWrapper(this.gitPath);
                    return (origin: wrapper.GetOriginRepoPath(), upstream: wrapper.GetUpstreamRepoPath());
                });
                Origin = origin;
                Upstream = upstream;
                RepositoryState = RepositoryState.Initialized;
            }
            catch (Exception) {
                RepositoryState = RepositoryState.Invalid;
            }
        }
    }
}