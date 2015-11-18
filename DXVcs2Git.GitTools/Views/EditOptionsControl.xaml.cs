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
using DevExpress.Xpf.Core;

namespace DXVcs2Git.GitTools.Views {
    /// <summary>
    /// Interaction logic for EditOptionsControl.xaml
    /// </summary>
    public partial class EditOptionsControl : UserControl {
        public EditOptionsControl() {
            InitializeComponent();
            ThemeManager.SetThemeName(this, "Office2013");
        }
    }
}
