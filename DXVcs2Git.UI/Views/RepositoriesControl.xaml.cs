using System.Windows.Controls;
using DevExpress.Mvvm;
using DXVcs2Git.UI.ViewModels;

namespace DXVcs2Git.UI {
    /// <summary>
    /// Interaction logic for RepositoriesControl.xaml
    /// </summary>
    public partial class RepositoriesControl : UserControl {
        public EditRepositoriesViewModel EditRepositories { get { return (EditRepositoriesViewModel)DataContext; } }

        public RepositoriesControl() {
            InitializeComponent();
            Messenger.Default.Register<Message>(this, OnMessage);
        }

        void OnMessage(Message msg) {
            EditRepositories.Update();
        }
    }
}
