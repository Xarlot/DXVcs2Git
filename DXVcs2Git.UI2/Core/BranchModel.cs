using NGitLab.Models;

namespace DXVcs2Git.UI2.Core {
    public enum BranchModelState {
        NotInitialized,
        Initializing,
        Initialized,
        Invalid,
    }    
    public interface IBranchModel {
        string Name { get; }
    }
    
    public class BranchModel : IBranchModel {
        public string Name => this.branch.Name;

        readonly Branch branch;
        
        public BranchModel(Branch branch) {
            this.branch = branch;
        }
    }
}