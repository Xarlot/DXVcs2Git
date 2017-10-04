using DevExpress.Data;
using DevExpress.Mvvm.UI.Interactivity;
using DevExpress.Xpf.Grid;
using DXVcs2Git.UI.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace DXVcs2Git.UI.Behaviors {
    public class GridSummaryChangeModeBehavior : Behavior<GridControl> {
        public static readonly DependencyProperty SummaryItemAddedProperty;
        public static readonly DependencyProperty SummaryItemRemovedProperty;
        public static readonly DependencyProperty SummaryItemChangedProperty;
        public static readonly DependencyProperty SummaryItemRenamedProperty;

        static GridSummaryChangeModeBehavior() {
            Type ownerType = typeof(GridSummaryChangeModeBehavior);
            SummaryItemAddedProperty = DependencyProperty.Register("SummaryItemAdded", typeof(GridSummaryItem), ownerType, new PropertyMetadata(null));
            SummaryItemRemovedProperty = DependencyProperty.Register("SummaryItemRemoved", typeof(GridSummaryItem), ownerType, new PropertyMetadata(null));
            SummaryItemChangedProperty = DependencyProperty.Register("SummaryItemChanged", typeof(GridSummaryItem), ownerType, new PropertyMetadata(null));
            SummaryItemRenamedProperty = DependencyProperty.Register("SummaryItemRenamed", typeof(GridSummaryItem), ownerType, new PropertyMetadata(null));
        }

        public GridSummaryItem SummaryItemAdded {
            get { return (GridSummaryItem)GetValue(SummaryItemAddedProperty); }
            set { SetValue(SummaryItemAddedProperty, value); }
        }
        public GridSummaryItem SummaryItemRemoved {
            get { return (GridSummaryItem)GetValue(SummaryItemRemovedProperty); }
            set { SetValue(SummaryItemRemovedProperty, value); }
        }
        public GridSummaryItem SummaryItemChanged {
            get { return (GridSummaryItem)GetValue(SummaryItemChangedProperty); }
            set { SetValue(SummaryItemChangedProperty, value); }
        }
        public GridSummaryItem SummaryItemRenamed {
            get { return (GridSummaryItem)GetValue(SummaryItemRenamedProperty); }
            set { SetValue(SummaryItemRenamedProperty, value); }
        }

        protected override void OnAttached() {
            base.OnAttached();
            AssociatedObject.CustomSummary += AssociatedObject_CustomSummary;
        }
        protected override void OnDetaching() {
            base.OnDetaching();
            AssociatedObject.CustomSummary -= AssociatedObject_CustomSummary;
        }

        void AssociatedObject_CustomSummary(object sender, CustomSummaryEventArgs e) {
            if(SummaryItemAdded == null || SummaryItemRemoved == null || SummaryItemChanged == null || SummaryItemRenamed == null)
                return;
            if(!e.IsTotalSummary)
                return;
            switch(e.SummaryProcess) {
                case CustomSummaryProcess.Start:
                    e.TotalValue = 0;
                    return;
                case CustomSummaryProcess.Calculate:
                    if(!(e.FieldValue is FileChangeMode))
                        return;
                    switch((FileChangeMode)e.FieldValue) {
                        case FileChangeMode.New:
                            if(e.Item == SummaryItemAdded)
                                IncrementSummaryValue(e);
                            return;
                        case FileChangeMode.Deleted:
                            if(e.Item == SummaryItemRemoved)
                                IncrementSummaryValue(e);
                            return;
                        case FileChangeMode.Modified:
                            if(e.Item == SummaryItemChanged)
                                IncrementSummaryValue(e);
                            return;
                        case FileChangeMode.Renamed:
                            if(e.Item == SummaryItemRenamed)
                                IncrementSummaryValue(e);
                            return;
                    }
                    return;
                case CustomSummaryProcess.Finalize:
                    ((GridSummaryItem)e.Item).Visible = (int)e.TotalValue > 0;
                    return;
            }
        }

        void IncrementSummaryValue(CustomSummaryEventArgs e) {
            e.TotalValue = (int)e.TotalValue + 1;
        }
    }
}
