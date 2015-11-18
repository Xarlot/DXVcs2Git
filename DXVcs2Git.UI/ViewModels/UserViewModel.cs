using DevExpress.Mvvm;
using NGitLab.Models;


namespace DXVcs2Git.UI.ViewModels {
    public class UserViewModel : BindableBase {
        public string Name { get { return User.Name; } }
        public User User { get; }
        public UserViewModel(User user) {
            User = user;
        }
    }
}
