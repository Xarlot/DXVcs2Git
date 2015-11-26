using System.Collections.Generic;
using System.Windows.Input;
using DevExpress.Mvvm;
using DevExpress.Mvvm.Native;

namespace DXVcs2Git.UI.ViewModels {
    public class EditRepositoriesViewModel : ViewModelBase {
        public ICommand UpdateCommand { get; private set; }
        new RepositoriesViewModel Parameter { get { return (RepositoriesViewModel)base.Parameter; } }
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
        protected override void OnParameterChanged(object parameter) {
            base.OnParameterChanged(parameter);
            Update();
        }
        bool CanUpdate() {
            return IsInitialized;
        }
        public void Update() {
            if (Parameter == null) {
                Repositories = null;
                SelectedRepository = null;
                IsInitialized = false;
                return;
            }
            Repositories = Parameter.Repositories;
            SelectedRepository = Parameter.SelectedRepository;
            IsInitialized = Parameter.IsInitialized;
        }
        void SelectedRepositoryChanged() {
            Parameter.SelectedRepository = SelectedRepository;
        }
    }
}
