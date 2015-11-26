using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using DevExpress.Mvvm;
using DXVcs2Git.UI.ViewModels;

namespace DXVcs2Git.UI.Views {
    /// <summary>
    /// Interaction logic for BranchChangesControl.xaml
    /// </summary>
    public partial class BranchChangesControl : UserControl {
        EditBranchChangesViewModel BranchChanged { get { return (EditBranchChangesViewModel)DataContext; } }
        public BranchChangesControl() {
            InitializeComponent();
            Messenger.Default.Register<Message>(this, OnMessage);
        }
        void OnMessage(Message obj) {
            BranchChanged.Refresh();
        }
    }
}
