using DevExpress.Mvvm.Native;
using DevExpress.Mvvm.UI.Interactivity;
using DevExpress.Xpf.Bars.Native;
using DevExpress.Xpf.Core;
using DevExpress.Xpf.Ribbon;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Markup;

namespace DXVcs2Git.UI.Behaviors {
    [ContentProperty("ContentTemplate")]
    public class RibbonWindowButtonsInjectionBehavior : Behavior<DXRibbonWindow> {
        public static readonly DependencyProperty ContentProperty = DependencyProperty.Register("Content", typeof(object), typeof(RibbonWindowButtonsInjectionBehavior), new PropertyMetadata(null));
        StackPanel controlBoxItems;                
        public DataTemplate ContentTemplate { get; set; }
        public object Content {
            get { return (object)GetValue(ContentProperty); }
            set { SetValue(ContentProperty, value); }
        }
        protected override void OnAttached() {
            base.OnAttached();
            ThemeManager.AddThemeChangedHandler(AssociatedObject, OnWindowThemeChanged);
            OnWindowThemeChanged(AssociatedObject, new ThemeChangedRoutedEventArgs(ThemeManager.GetThemeName(AssociatedObject)));
        }        
        protected override void OnDetaching() {
            ThemeManager.RemoveThemeChangedHandler(AssociatedObject, OnWindowThemeChanged);
            base.OnDetaching();
        }
        void OnWindowThemeChanged(DependencyObject sender, ThemeChangedRoutedEventArgs e) {
            Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.ApplicationIdle, new Action(OnAfterWindowThemeChanged));            
        }

        void OnAfterWindowThemeChanged() {            
            var cbc = TreeHelper.GetChild<ContentControl>(AssociatedObject, x => x.Name == "PART_ControlBoxContainer").With(x=>x.Content as StackPanel);
            if (controlBoxItems == cbc)
                return;
            if (controlBoxItems != null)
                controlBoxItems.Children.RemoveAt(0);
            if (cbc != null) {
                var cctrl = new ContentControl();
                cctrl.SetBinding(ContentControl.ContentProperty, new Binding() { Path = new PropertyPath(ContentProperty), Source = this });
                cctrl.ContentTemplate = ContentTemplate;
                cbc.Children.Insert(0, cctrl);
            }                
            controlBoxItems = cbc;
        }
    }
}
