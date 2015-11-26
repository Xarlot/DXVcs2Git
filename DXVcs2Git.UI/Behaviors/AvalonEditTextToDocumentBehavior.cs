using System.Windows;
using DevExpress.Mvvm.UI.Interactivity;
using DXVcs2Git.UI.Views;
using ICSharpCode.AvalonEdit;

namespace DXVcs2Git.UI.Behaviors {
    public class AvalonEditTextBindingBehavior : Behavior<TextEditor> {
        public static readonly DependencyProperty TextProperty;

        static AvalonEditTextBindingBehavior() {
            TextProperty = DependencyProperty.Register("Text", typeof(string), typeof(AvalonEditTextBindingBehavior),
                new PropertyMetadata(null, (o, args) => ((AvalonEditTextBindingBehavior)o).TextChanged((string)args.NewValue)));
        }
        public string Text {
            get { return (string)GetValue(TextProperty); }
            set { SetValue(TextProperty, value); }
        }

        void TextChanged(string newValue) {
            if (IsAttached)
                AssociatedObject.Text = newValue;
        }
        protected override void OnAttached() {
            base.OnAttached();
            AssociatedObject.Text = Text;
            AssociatedObject.TextArea.TextView.BackgroundRenderers.Add(new DiffLineBackgroundRenderer(AssociatedObject));
        }
    }
}
