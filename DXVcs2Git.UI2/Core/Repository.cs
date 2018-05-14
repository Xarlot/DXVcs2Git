using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading;
using System.Threading.Tasks;
using DevExpress.CodeParser;
using DevExpress.Xpf.Core.Native;
using DXVcs2Git.Core.Git;
using DXVcs2Git.Git;
using NGitLab.Models;

namespace DXVcs2Git.UI2.Core {
    public enum RepositoryState {
        NotInitialized,
        Initializing,
        Initialized,
        Invalid,
        Empty,
    }

    public interface IRepository {
        IObservable<RepositoryState> RepositoryStateObservable { get; }
        Task Initialize();
    }

    public class Repository : IRepository {
        readonly IRepositoriesStorage repositories;
        readonly string gitPath;
        readonly string gitServer;
        readonly string token;
        readonly GitLabWrapper gitLabWrapper;
        
        readonly BehaviorSubject<RepositoryState> repositoryStateSubject = new BehaviorSubject<RepositoryState>(RepositoryState.NotInitialized);
        public IObservable<RepositoryState> RepositoryStateObservable => this.repositoryStateSubject.AsObservable();

        public RepositoryState RepositoryState {
            get => this.repositoryStateSubject.Value;
            set => this.repositoryStateSubject.OnNext(value);
        }
        public Project Origin { get; private set; }
        public Project Upstream { get; private set; }
        public ImmutableArray<Branch> Branches { get; private set; }

        public Repository(IRepositoriesStorage repositories, string gitPath, string gitServer, string token) {
            this.repositories = repositories;
            this.gitPath = gitPath;
            this.gitServer = gitServer;
            this.token = token;
            this.gitLabWrapper = new GitLabWrapper(gitServer, token);
        }

        public async Task Initialize() {
            try {
                RepositoryState = RepositoryState.Initializing;
                var (origin, upstream, branches)  = await Task.Run(() => {
                    GitReaderWrapper wrapper = new GitReaderWrapper(this.gitPath);
                    return Parse(wrapper);
                });
                Origin = origin;
                Upstream = upstream;
                Branches = branches;
                RepositoryState = branches.Any() ? RepositoryState.Initialized : RepositoryState.Empty;
            }
            catch (Exception) {
                RepositoryState = RepositoryState.Invalid;
            }
        }
        (Project origin, Project upstream, ImmutableArray<Branch> branches) Parse(GitReaderWrapper wrapper) {
            string originPath = wrapper.GetOriginRepoPath();
            string upstreamPath = wrapper.GetUpstreamRepoPath();
            var localBranches = wrapper.GetLocalBranches().ToArray();

            var origin = this.gitLabWrapper.FindProject(originPath);
            var upstream = this.gitLabWrapper.FindProject(upstreamPath);

            var remoteBranches = this.gitLabWrapper.GetBranches(origin).ToArray();

            List<Branch> branches = new List<Branch>();
            foreach (var localBranch in localBranches) {
                var name = localBranch.UpstreamBranchCanonicalName;
                var branchCandidate = remoteBranches.FirstOrDefault(x => string.Compare($@"refs/heads/{x.Name}", name, StringComparison.InvariantCultureIgnoreCase) == 0);
                if (branchCandidate != null && !branchCandidate.Protected)
                    branches.Add(branchCandidate);
            }

            return (origin, upstream, branches.ToImmutableArray());
        }
    }
}