using System;
using System.Windows.Input;
using System.Windows.Threading;
using DevExpress.Mvvm;
using DevExpress.Mvvm.Native;
using DXVcs2Git.Core;
using DXVcs2Git.UI.Farm;

namespace DXVcs2Git.UI.ViewModels {
    public class RootViewModel : ViewModelBase {
        public RepositoriesViewModel Repositories { get; private set; }
        public ICommand SettingsCommand { get; private set; }
        public IDialogService SettingsDialogService { get { return GetService<IDialogService>("settingsDialogService"); } }
        public Config Config { get; private set; }
        public string Version { get; private set; }

        public RootViewModel() {
            Repositories = new RepositoriesViewModel();
            ISupportParentViewModel supportParent = Repositories;
            supportParent.ParentViewModel = this;
            Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.ContextIdle, new Action(Initialize));
            FarmIntegrator.Start(Dispatcher.CurrentDispatcher, FarmRefreshed);

            SettingsCommand = DelegateCommandFactory.Create(ShowSettings, CanShowSettings);
            Config = ConfigSerializer.GetConfig();
            Version = $"Git tools {VersionInfo.Version}";
        }
        void FarmRefreshed() {
            Repositories.RefreshFarm();
        }
        public void Initialize() {
            Repositories.Update();
        }
        public void Refresh() {
            Repositories.Refresh();
        }
        void ShowSettings() {
            var viewModel = new EditConfigViewModel(Config);
            if (SettingsDialogService.ShowDialog(MessageButton.OKCancel, "Settings", viewModel) == MessageResult.OK) {
                Config = viewModel.CreateConfig();
                ConfigSerializer.SaveConfig(Config);
                Initialize();
            }
        }
        bool CanShowSettings() {
            return true;
        }
    }
}
