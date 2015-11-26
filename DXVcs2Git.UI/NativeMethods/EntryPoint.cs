using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DXVcs2Git.UI.NativeMethods {
    public class EntryPoint {
        static Mutex mutex = new Mutex(true, "{440DD7EE-560B-4657-8FA9-390A6CEC1D3C}");
        [STAThread]
        public static void Main(string[] args) {
            if(mutex.WaitOne(TimeSpan.Zero, true)) {
                var app = new App();
                app.InitializeComponent();
                app.Run();
                mutex.ReleaseMutex();
            } else {
                NativeMethods.PostMessage(
                (IntPtr)NativeMethods.HWND_BROADCAST,
                NativeMethods.WM_SHOWGITTOOLS,
                IntPtr.Zero,
                IntPtr.Zero);
            }
        }
    }
}
