using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using DXVcs2Git.UI.ViewModels;

namespace DXVcs2Git.UI.Views {
    public class BranchesTemplateSelector : DataTemplateSelector {
        public override DataTemplate SelectTemplate(object item, DependencyObject container) {
            var branchViewModel = item as BranchViewModel;
            if (branchViewModel == null)
                return base.SelectTemplate(item, container);
            var fe = (FrameworkElement)container;
            if (branchViewModel.HasMergeRequest)
                return (DataTemplate)fe.FindResource("editMergeRequestTemplate");
            return (DataTemplate)fe.FindResource("noMergeRequestTemplate");
        }
    }
}
