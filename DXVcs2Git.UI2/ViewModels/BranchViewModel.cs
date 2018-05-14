using System;
using DXVcs2Git.UI2.Core;
using NGitLab.Models;
using ReactiveUI;

namespace DXVcs2Git.UI2.ViewModels {
    public class BranchViewModel : ReactiveObject, IDisposable {
        readonly IRepositoryModel repository;
        readonly IBranchModel branch;
        BranchModelState state;
        string name;

        public BranchModelState State {
            get => this.state;
            private set => this.RaiseAndSetIfChanged(ref this.state, value);
        }
        public string Name {
            get => this.name;
            private set => this.RaiseAndSetIfChanged(ref this.name, value);
        }

        public BranchViewModel(IRepositoryModel repository, IBranchModel branch) {
            this.repository = repository;
            this.branch = branch;
            State = BranchModelState.Initialized;
            Name = branch.Name;
        }
        
        public void Dispose() {
        }
    }
}