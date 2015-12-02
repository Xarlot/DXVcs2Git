using System;
using System.Collections.ObjectModel;
using System.Linq;
using DevExpress.Mvvm;
using DevExpress.Mvvm.Native;
using DXVcs2Git.Core.Configuration;
using System.Windows.Input;

namespace DXVcs2Git.UI.ViewModels {
    public class EditConfigViewModel : ViewModelBase {
        readonly Config config;
        public string Token {
            get { return GetProperty(() => Token); }
            set { SetProperty(() => Token, value); }
        }
        public int UpdateDelay {
            get { return GetProperty(() => UpdateDelay); }
            set { SetProperty(() => UpdateDelay, value, OnUpdateDelayChanged); }
        }
        public ICommand RefreshUpdateCommand { get; private set; }

        void OnUpdateDelayChanged() {
            AtomFeed.FeedWorker.UpdateDelay = UpdateDelay;
        }

        public ObservableCollection<TrackRepository> Repositories { get; private set; }

        public EditConfigViewModel(Config config) {
            this.config = config;
            UpdateDelay = AtomFeed.FeedWorker.UpdateDelay;
            RefreshUpdateCommand = DelegateCommandFactory.Create(AtomFeed.FeedWorker.Update);
            Token = config.Token;
            Repositories = config.Repositories.Return(x => new ObservableCollection<TrackRepository>(config.Repositories), () => new ObservableCollection<TrackRepository>());
        }
        public void UpdateConfig() {
            config.Token = Token;
            config.Repositories = Repositories.With(x => x.ToArray());
            config.UpdateDelay = UpdateDelay;
        }
    }
}
