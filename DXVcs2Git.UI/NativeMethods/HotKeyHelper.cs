using DevExpress.Mvvm.Native;
using DXVcs2Git.Core.Configuration;
using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;

namespace DXVcs2Git.UI.NativeMethods {
    public static class HotKeyHelper {
        static readonly KeyGestureConverter kgConverter = new KeyGestureConverter();
        static readonly short atom;
        static IntPtr Handle {
            get { return new WindowInteropHelper(Application.Current.MainWindow).Do(x => x.EnsureHandle()).Handle; }
        }
        static HotKeyHelper() {
            atom = NativeMethods.GlobalAddAtom("GittToolsHotKey");
            Initialize();
        }
        public static string GetString(Key key, ModifierKeys modifiers) {
            try {
                return (string)kgConverter.ConvertTo(new KeyGesture(key, modifiers), typeof(string));
            } catch {
                return null;
            }
        }
        public static void Initialize() {
            RegisterHotKey(ConfigSerializer.GetConfig().KeyGesture);
        }
        public static bool GetKeyAndModifiers(string source, out Key key, out ModifierKeys modifiers) {
            try {
                var gesture = (KeyGesture)kgConverter.ConvertFrom(source);
                key = gesture.Key;
                modifiers = gesture.Modifiers;
            } catch {
                key = Key.None;
                modifiers = ModifierKeys.None;
            }
            return key != Key.None || modifiers != ModifierKeys.None;
        }
        public static bool RegisterHotKey(string hotkey) {
            if (string.IsNullOrEmpty(hotkey))
                return true;
            Key key;
            ModifierKeys modifiers;
            if (!GetKeyAndModifiers(hotkey, out key, out modifiers))
                return false;
            return NativeMethods.RegisterHotKey(Handle, atom, (uint)modifiers, (uint)KeyInterop.VirtualKeyFromKey(key));
        }
        public static bool UnregisterHotKey() {
            return NativeMethods.UnregisterHotKey(Handle, atom);
        }
        public static bool ProcessMessage(int msg, IntPtr wParam) {
            return msg == NativeMethods.WM_HOTKEY && ((short)wParam.ToInt32()) == atom;
        }
    }
}
