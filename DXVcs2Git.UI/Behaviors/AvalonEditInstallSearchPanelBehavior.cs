using DevExpress.Mvvm.UI.Interactivity;
using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.Search;

namespace DXVcs2Git.UI.Behaviors {
    public class AvalonEditInstallSearchPanelBehavior : Behavior<TextEditor> {
        protected override void OnAttached() {
            base.OnAttached();
            SearchPanel.Install(AssociatedObject);
        }
        protected override void OnDetaching() {
            base.OnDetaching();
        }
    }
}
