using DevExpress.Mvvm.UI.Interactivity;
using DevExpress.Xpf.Core;
using Hardcodet.Wpf.TaskbarNotification;
using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Threading;

namespace DXVcs2Git.UI.Behaviors {
    public class TaskBarIconBehavior : Behavior<Window> {
        TaskbarIcon icon;
        HwndSource hwndSource;
        readonly Locker preventClosingLocker = new Locker();
        bool CanCloseWindow {
            get {
#if DEBUG
                return true;
#else
                return Keyboard.IsKeyDown(Key.LeftCtrl) || preventClosingLocker.IsLocked;
#endif
            }
        }
        protected override void OnAttached() {
            base.OnAttached();
            icon = new TaskbarIcon();
            AssignProperties();
        }

        void AssignProperties() {
            icon.SetBinding(TaskbarIcon.IconSourceProperty, new Binding() { Path = new PropertyPath(Window.IconProperty), Source = AssociatedObject });
            InitalizeContextMenu();
            SubscribeEvetns();
        }

        void SubscribeEvetns() {
            icon.TrayMouseDoubleClick += OnTrayDoubleClick;
            AssociatedObject.Closing += OnWindowClosing;
            Dispatcher.BeginInvoke(DispatcherPriority.ApplicationIdle, new Action(() => {
                hwndSource = PresentationSource.FromVisual(AssociatedObject) as HwndSource;
                hwndSource.AddHook(OnHwndSourceHook);
            }));
        }

        void InitalizeContextMenu() {
            ContextMenu cm = new ContextMenu();
            MenuItem closeItem = new MenuItem() { Header = "Exit" };
            closeItem.Click += (o, e) => {
                using (preventClosingLocker.Lock())
                    AssociatedObject.Close();
            };
            cm.Items.Add(closeItem);
            icon.ContextMenu = cm;
        }

        IntPtr OnHwndSourceHook(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled) {
            if (msg == NativeMethods.NativeMethods.WM_SHOWGITTOOLS) {
                AssociatedObject.Show();
                AssociatedObject.WindowState = WindowState.Normal;
                AssociatedObject.Activate();                
            }                
            return IntPtr.Zero;
        }

        void OnTrayDoubleClick(object sender, RoutedEventArgs e) {
            AssociatedObject.Show();
        }
        void OnWindowClosing(object sender, System.ComponentModel.CancelEventArgs e) {
            if (CanCloseWindow)
                return;
            e.Cancel = true;
            AssociatedObject.Hide();            
        }

        protected override void OnDetaching() {
            base.OnDetaching();
            icon.TrayMouseDoubleClick -= OnTrayDoubleClick;
            icon.Dispose();            
            AssociatedObject.Closing -= OnWindowClosing;
            hwndSource.RemoveHook(OnHwndSourceHook);
        }
    }
}
