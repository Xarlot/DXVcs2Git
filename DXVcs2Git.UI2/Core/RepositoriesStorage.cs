using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using DXVcs2Git.Core.Git;

namespace DXVcs2Git.UI2.Core {
    public interface IRepositoriesStorage {
        IObservable<ImmutableArray<IRepository>> RepositoriesObservable { get; }
        Task Initialize();
    }

    public class RepositoriesStorage : IRepositoriesStorage {
        RepoConfigsReader configReader = new RepoConfigsReader();

        readonly BehaviorSubject<ImmutableArray<IRepository>> repositoriesSubject = new BehaviorSubject<ImmutableArray<IRepository>>(ImmutableArray<IRepository>.Empty);

        public IObservable<ImmutableArray<IRepository>> RepositoriesObservable => this.repositoriesSubject.AsObservable();

        public ImmutableArray<IRepository> Repositories {
            get => this.repositoriesSubject.Value;
            private set => this.repositoriesSubject.OnNext(value);
        }

        public async Task Initialize() {
            var repoName = new[] {@"c:\Work\2018.1", @"c:\Work\2017.2", @"c:\Work\2017.1"};
            List<IRepository> list = new List<IRepository>();
            foreach (var name in repoName) {
                var repo = new Repository(this, name);
                list.Add(repo);
                Repositories = list.ToImmutableArray();
                await repo.Initialize();
            }
        }
    }
}