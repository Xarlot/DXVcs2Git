using System.Collections.Generic;
using System.Windows.Input;
using DevExpress.Mvvm;
using DevExpress.Mvvm.Native;
using DevExpress.Mvvm.POCO;

namespace DXVcs2Git.UI.ViewModels {
    public class EditRepositoriesViewModel : ViewModelBase {
        public ICommand UpdateCommand { get; private set; }
        public RepositoriesViewModel Parent { get { return this.GetParentViewModel<RepositoriesViewModel>(); } }
        public RepositoryViewModel SelectedRepository {
            get { return GetProperty(() => SelectedRepository); }
            set { SetProperty(() => SelectedRepository, value, SelectedRepositoryChanged); }
        }
        public IEnumerable<RepositoryViewModel> Repositories {
            get { return GetProperty(() => Repositories); }
            private set { SetProperty(() => Repositories, value); }
        }
        public bool IsInitialized {
            get { return GetProperty(() => IsInitialized); }
            private set { SetProperty(() => IsInitialized, value); }
        }

        public EditRepositoriesViewModel() {
            UpdateCommand = DelegateCommandFactory.Create(Update, CanUpdate);
        }
        bool CanUpdate() {
            return IsInitialized;
        }
        public void Update() {
            Repositories = Parent.Repositories;
            SelectedRepository = Parent.SelectedRepository;
            IsInitialized = Parent.IsInitialized;
        }
        void SelectedRepositoryChanged() {
            Parent.SelectedRepository = SelectedRepository;
        }
    }
}
