using DevExpress.Mvvm;
using DXVcs2Git.Core.Git;

namespace DXVcs2Git.UI.ViewModels {
    public class EditTrackRepository : BindableBase {
        public string Name {
            get { return GetProperty(() => Name); }
            set { SetProperty(() => Name, value); }
        }
        public string ConfigName {
            get { return GetProperty(() => ConfigName); }
            set { SetProperty(() => ConfigName, value); }
        }
        public RepoConfig RepoConfig {
            get { return GetProperty(() => RepoConfig); }
            set { SetProperty(() => RepoConfig, value); }
        }
        public string Token {
            get { return GetProperty(() => Token); }
            set { SetProperty(() => Token, value); }
        }
        public string LocalPath {
            get { return GetProperty(() => LocalPath); }
            set { SetProperty(() => LocalPath, value); }
        }

        public override string ToString() {
            return Name;
        }
    }
}
