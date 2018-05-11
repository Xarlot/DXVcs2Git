using DXVcs2Git.UI2.Core;
using ReactiveUI;

namespace DXVcs2Git.UI2.ViewModels {
    public class RepositoriesViewModel : ReactiveObject {
        readonly IRepositories repositories;
        
        public RepositoriesViewModel(IRepositories repositories) {
            this.repositories = repositories;
        }
    }
}