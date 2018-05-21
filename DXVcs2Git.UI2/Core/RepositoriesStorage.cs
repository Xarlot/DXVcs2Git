using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using DXVcs2Git.Core.Git;

namespace DXVcs2Git.UI2.Core {
    public interface IRepositoriesStorage {
        IObservable<ImmutableArray<IRepositoryModel>> RepositoriesObservable { get; }
        Task Initialize();
    }

    public class RepositoriesStorage : IRepositoriesStorage {
        RepoConfigsReader configReader = new RepoConfigsReader();
        const string gitserver = @"http://gitserver";
        const string auth = "y9SnbdMyzcYmxU-zxRY9";

        readonly BehaviorSubject<ImmutableArray<IRepositoryModel>> repositoriesSubject = new BehaviorSubject<ImmutableArray<IRepositoryModel>>(ImmutableArray<IRepositoryModel>.Empty);

        public IObservable<ImmutableArray<IRepositoryModel>> RepositoriesObservable => this.repositoriesSubject.AsObservable();

        public ImmutableArray<IRepositoryModel> Repositories {
            get => this.repositoriesSubject.Value;
            private set => this.repositoriesSubject.OnNext(value);
        }

        public async Task Initialize() {
            var repoName = new[] {@"c:\Work\2018.1", @"c:\Work\2017.2", @"c:\Work\2017.1"};
            Repositories = repoName.Select(x => new RepositoryModel(this, x, gitserver, auth)).ToImmutableArray<IRepositoryModel>();
            foreach (var repository in Repositories)
                await repository.Initialize();
        }
    }
}