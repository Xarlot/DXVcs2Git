using System.Windows.Controls;
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
