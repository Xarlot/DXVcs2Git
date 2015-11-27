using DevExpress.Mvvm.Native;
using DevExpress.Xpf.Core;
using DevExpress.Xpf.Core.Internal;
using DevExpress.Xpf.Editors;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Interop;

namespace DXVcs2Git.UI.Services {
    public class FolderBrowserCustomDialogService : ICustomDialogService {
        public bool ShowDialog(string title, object viewModel) {
            var editorValue = ((UITypeEditorValue)viewModel);
            FolderBrowserDialog dialog = new FolderBrowserDialog();
            var get_Owner = ReflectionHelper.CreateInstanceMethodHandler<UITypeEditorValue, Func<UITypeEditorValue, DependencyObject>>(editorValue, "get_Owner", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            dialog.SelectedPath = Convert.ToString(editorValue.OriginalValue);
            var result = dialog.ShowDialog(new Win32Window(get_Owner(editorValue)));                        
            if (result == DialogResult.Cancel)
                return false;
            editorValue.Value = dialog.SelectedPath;
            return true;
        }
        class Win32Window : System.Windows.Forms.IWin32Window {
            IntPtr handle;
            public Win32Window(DependencyObject dObj) {
                handle = (PresentationSource.FromDependencyObject(dObj) as HwndSource).Return(x => x.Handle, () => IntPtr.Zero);
            }
            public IntPtr Handle {
                get { return handle; }
            }
        }
    }
}
