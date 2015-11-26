using System.Windows.Controls;
using DevExpress.Mvvm;
using DXVcs2Git.UI.ViewModels;

namespace DXVcs2Git.UI.Views {
    /// <summary>
    /// Interaction logic for EditMergeRequestControl.xaml
    /// </summary>
    public partial class EditMergeRequestControl : UserControl {
        public EditMergeRequestViewModel EditableMergeRequest { get { return (EditMergeRequestViewModel)DataContext; } }
        public EditMergeRequestControl() {
            InitializeComponent();
            Messenger.Default.Register<Message>(this, OnMessage);
        }
        void OnMessage(Message msg) {
            EditableMergeRequest.Refresh();
        }
    }
}
