using System.Windows.Controls;
using DevExpress.Mvvm;
using DXVcs2Git.UI.ViewModels;

namespace DXVcs2Git.UI.Views {
    /// <summary>
    /// Interaction logic for BranchDescriptionControl.xaml
    /// </summary>
    public partial class BranchDescriptionControl : UserControl {
        EditBranchDescriptionViewModel BranchDescription { get { return (EditBranchDescriptionViewModel)DataContext; } }

        public BranchDescriptionControl() {
            InitializeComponent();
            Messenger.Default.Register<Message>(this, OnMessage);
        }
        void OnMessage(Message msg) {
            BranchDescription.Refresh();
        }
    }
}
