using System.Windows.Controls;
using DevExpress.Mvvm;
using DXVcs2Git.UI.ViewModels;

namespace DXVcs2Git.UI {
    /// <summary>
    /// Interaction logic for ProcessMergeRequestControl.xaml
    /// </summary>
    public partial class ProcessMergeRequestControl : UserControl {
        EditSelectedRepositoryViewModel SelectedRepository { get { return (EditSelectedRepositoryViewModel)DataContext; } }
        public ProcessMergeRequestControl() {
            InitializeComponent();
            Messenger.Default.Register<Message>(this, OnMessage);
        }
        void OnMessage(Message msg) {
            if (msg.MessageType == MessageType.Update) {
                SelectedRepository.Update();
            }
            else if (msg.MessageType == MessageType.Refresh) {
                SelectedRepository.Refresh();
            }
        }
    }
}
