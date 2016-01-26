using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace DXVcs2Git.UI.NativeMethods {
    public class NativeMethods {
        public const int HWND_BROADCAST = 0xffff;
        public static readonly int WM_SHOWGITTOOLS = RegisterWindowMessage("WM_SHOWGITTOOLS");
        public static readonly int WM_HOTKEY = 0x0312;
        [DllImport("user32")]
        public static extern bool PostMessage(IntPtr hwnd, int msg, IntPtr wparam, IntPtr lparam);
        [DllImport("user32")]
        public static extern int RegisterWindowMessage(string message); 
        [DllImport("user32.dll", EntryPoint ="RegisterHotKey", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);
        [DllImport("user32.dll", EntryPoint = "UnregisterHotKey", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern bool UnregisterHotKey(IntPtr hWnd, int id);
        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern short GlobalAddAtom(string lpString);
        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern short GlobalDeleteAtom(short atom);
    }
}
