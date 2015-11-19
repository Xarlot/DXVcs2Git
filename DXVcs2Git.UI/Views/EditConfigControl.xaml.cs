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
using DevExpress.Xpf.Grid;

namespace DXVcs2Git.UI.Views {
    /// <summary>
    /// Interaction logic for EditConfigControl.xaml
    /// </summary>
    public partial class EditConfigControl : UserControl {
        public EditConfigControl() {
            InitializeComponent();
        }
        void TableView_OnInitNewRow(object sender, InitNewRowEventArgs e) {
            e.Handled = true;
        }
    }
}
