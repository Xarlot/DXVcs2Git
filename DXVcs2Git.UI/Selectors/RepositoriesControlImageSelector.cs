using System;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using DevExpress.Xpf.Grid;
using DevExpress.Xpf.Grid.TreeList;
using DXVcs2Git.UI.ViewModels;

namespace DXVcs2Git.UI.Selectors {
    public class RepositoriesControlImageSelector : TreeListNodeImageSelector {
        public override ImageSource Select(TreeListRowData rowData) {
            var editItem = rowData.Node.Content as EditRepositoryItem;
            if (editItem != null && editItem.HasMergeRequest)
                return new BitmapImage(new Uri(@"/DXVcs2Git.UI;component/Images/red.png", UriKind.RelativeOrAbsolute));
            return new BitmapImage(new Uri(@"/DXVcs2Git.UI;component/Images/green.png", UriKind.RelativeOrAbsolute));
        }
    }
}
