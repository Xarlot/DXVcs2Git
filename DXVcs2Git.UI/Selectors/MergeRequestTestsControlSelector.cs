using System.Windows;
using System.Windows.Controls;
using DXVcs2Git.UI.ViewModels;

namespace DXVcs2Git.UI.Selectors {
    public class MergeRequestTestsControlSelector : DataTemplateSelector {
        public override DataTemplate SelectTemplate(object item, DependencyObject container) {
            if (item == null)
                return (DataTemplate)((FrameworkElement)container).FindResource("emptyMergeRequestTestsControl");
            bool supportsTesting = (bool)item;
            return supportsTesting
                ? (DataTemplate)((FrameworkElement)container).FindResource("mergeRequestTestsControl")
                : (DataTemplate)((FrameworkElement)container).FindResource("emptyMergeRequestTestsControl");
        }
    }
}
