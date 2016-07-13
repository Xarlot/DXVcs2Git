using System.Windows;
using System.Windows.Controls;
using DXVcs2Git.UI.ViewModels;

namespace DXVcs2Git.UI.Selectors {
    public class MergeRequestTestsControlSelector : DataTemplateSelector {
        public override DataTemplate SelectTemplate(object item, DependencyObject container) {
            var branchViewModel = (EditBranchViewModel)item;
            if (branchViewModel == null || branchViewModel.SupportsTesting)
                return (DataTemplate)((FrameworkElement)container).FindResource("mergeRequestTestsControl");
            return (DataTemplate)((FrameworkElement)container).FindResource("emptyMergeRequestTestsControl");
        }
    }
}
