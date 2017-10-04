using DevExpress.Mvvm.UI.Interactivity;
using DevExpress.Xpf.Grid;
using DevExpress.Xpf.Grid.Native;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DXVcs2Git.UI.Behaviors {
    public class FixedTotalSummaryBehavior : Behavior<TableView> {
        ScrollInfoBase VerticalScrollInfo;

        protected override void OnAttached() {
            base.OnAttached();
            if(!AssociatedObject.IsLoaded)
                AssociatedObject.Loaded += AssociatedObject_Loaded;
            else
                SetUp();
        }

        void SetUp() {
            AssociatedObject.LayoutUpdated += ContentChanged;
            VerticalScrollInfo = GridControlHelper.GetDataPresenter(AssociatedObject).ScrollInfoCore.VerticalScrollInfo;
            UpdateTotalSummaryState();
        }

        void UpdateTotalSummaryState() {
            AssociatedObject.ShowFixedTotalSummary = VerticalScrollInfo.Viewport < VerticalScrollInfo.Extent;
        }

        void ContentChanged(object sender, EventArgs e) {
            UpdateTotalSummaryState();
        }

        private void AssociatedObject_Loaded(object sender, EventArgs e) {
            AssociatedObject.Loaded -= AssociatedObject_Loaded;
            SetUp();
        }
    }
}
