using System.Collections.ObjectModel;
using System.Linq;
using DevExpress.Mvvm;
using DevExpress.Mvvm.Native;

namespace DXVcs2Git.UI.ViewModels {
    public class EditConfigViewModel : ViewModelBase {
        public string Token {
            get { return GetProperty(() => Token); }
            set { SetProperty(() => Token, value); }
        }
        public ObservableCollection<TrackRepository> Repositories { get; private set; }

        public EditConfigViewModel(Config config) {
            Token = config.Token;
            Repositories = config.Repositories.Return(x => new ObservableCollection<TrackRepository>(config.Repositories), () => new ObservableCollection<TrackRepository>());
        }
        public Config CreateConfig() {
            return new Config() { Token = Token, Repositories = Repositories.With(x => x.ToArray())};
        }
    }
}
