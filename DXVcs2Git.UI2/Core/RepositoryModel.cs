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
    public enum RepositoryModelState {
        NotInitialized,
        Initializing,
        Initialized,
        Invalid,
        Empty,
    }

    public interface IRepositoryModel {
        IObservable<RepositoryModelState> RepositoryStateObservable { get; }
        Task Initialize();
        IEnumerable<BranchModel> Branches { get; }
        string Name { get; }
    }

    public class RepositoryModel : IRepositoryModel {
        readonly IRepositoriesStorage repositories;
        readonly string gitPath;
        readonly string gitServer;
        readonly string token;
        readonly GitLabWrapper gitLabWrapper;
        
        readonly BehaviorSubject<RepositoryModelState> repositoryStateSubject = new BehaviorSubject<RepositoryModelState>(RepositoryModelState.NotInitialized);
        public IObservable<RepositoryModelState> RepositoryStateObservable => this.repositoryStateSubject.AsObservable();

        public RepositoryModelState RepositoryState {
            get => this.repositoryStateSubject.Value;
            set => this.repositoryStateSubject.OnNext(value);
        }
        public Project Origin { get; private set; }
        public Project Upstream { get; private set; }
        public string Name { get; private set; }
        public IEnumerable<BranchModel> Branches { get; private set; }

        public RepositoryModel(IRepositoriesStorage repositories, string gitPath, string gitServer, string token) {
            this.repositories = repositories;
            this.gitPath = gitPath;
            this.gitServer = gitServer;
            this.token = token;
            this.gitLabWrapper = new GitLabWrapper(gitServer, token);
        }

        public async Task Initialize() {
            try {
                RepositoryState = RepositoryModelState.Initializing;
                (Origin, Upstream, Branches)  = await Task.Run(() => {
                    GitReaderWrapper wrapper = new GitReaderWrapper(this.gitPath);
                    return Parse(wrapper);
                });
                Name = Origin.Name;
                RepositoryState = Branches.Any() ? RepositoryModelState.Initialized : RepositoryModelState.Empty;
            }
            catch (Exception) {
                RepositoryState = RepositoryModelState.Invalid;
            }
        }
        (Project origin, Project upstream, ImmutableArray<BranchModel> branches) Parse(GitReaderWrapper wrapper) {
            string originPath = wrapper.GetOriginRepoPath();
            string upstreamPath = wrapper.GetUpstreamRepoPath();
            var localBranches = wrapper.GetLocalBranches().ToArray();

            var origin = this.gitLabWrapper.FindProject(originPath);
            var upstream = this.gitLabWrapper.FindProject(upstreamPath);

            var remoteBranches = this.gitLabWrapper.GetBranches(origin).ToArray();

            List<BranchModel> branches = new List<BranchModel>();
            foreach (var localBranch in localBranches) {
                var name = localBranch.UpstreamBranchCanonicalName;
                var branchCandidate = remoteBranches.FirstOrDefault(x => string.Compare($@"refs/heads/{x.Name}", name, StringComparison.InvariantCultureIgnoreCase) == 0);
                if (branchCandidate != null && !branchCandidate.Protected)
                    branches.Add(new BranchModel(branchCandidate));
            }

            return (origin, upstream, branches.ToImmutableArray());
        }
    }
}