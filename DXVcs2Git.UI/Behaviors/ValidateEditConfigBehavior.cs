using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using DevExpress.Mvvm.UI.Interactivity;
using DevExpress.Xpf.Grid;
using DevExpress.XtraEditors.DXErrorProvider;
using DXVcs2Git.UI.ViewModels;

namespace DXVcs2Git.UI.Behaviors {
    public class ValidateEditConfigBehavior : Behavior<TableView> {
        protected override void OnAttached() {
            base.OnAttached();
            AssociatedObject.InvalidRowException += AssociatedObjectOnInvalidRowException;
            AssociatedObject.ValidateRow += AssociatedObject_ValidateRow;
            AssociatedObject.RowUpdated += AssociatedObjectOnRowUpdated;
            AssociatedObject.CellValueChanged += AssociatedObjectOnCellValueChanged;
        }
        void AssociatedObjectOnCellValueChanged(object sender, CellValueChangedEventArgs e) {
            var fe = (FrameworkElement)sender;
            var editConfig = (EditConfigViewModel)fe.DataContext;
            if (e.Column.FieldName == "ConfigName") {
                var trackRepo = (EditTrackRepository)e.Row;
                trackRepo.RepoConfig = editConfig.GetConfig((string)e.Value);
            }
        }
        void AssociatedObjectOnRowUpdated(object sender, RowEventArgs e) {
            var fe = (FrameworkElement)sender;
            var editConfig = (EditConfigViewModel)fe.DataContext;
            editConfig.UpdateTokens();
        }
        protected override void OnDetaching() {
            base.OnDetaching();
            AssociatedObject.ValidateRow -= AssociatedObject_ValidateRow;
            AssociatedObject.InvalidRowException -= AssociatedObjectOnInvalidRowException;
            AssociatedObject.RowUpdated -= AssociatedObjectOnRowUpdated;
        }
        void AssociatedObjectOnInvalidRowException(object sender, InvalidRowExceptionEventArgs e) {
            e.ExceptionMode = ExceptionMode.NoAction;
            e.Handled = true;
        }
        void AssociatedObject_ValidateRow(object sender, GridRowValidationEventArgs e) {
            var fe = (FrameworkElement)sender;
            var editConfig = (EditConfigViewModel)fe.DataContext;

            var editTrackRepository = (EditTrackRepository)e.Row;
            bool invalid = string.IsNullOrEmpty(editTrackRepository.Name) || string.IsNullOrEmpty(editTrackRepository.ConfigName) || string.IsNullOrEmpty(editTrackRepository.LocalPath) ||
                   string.IsNullOrEmpty(editTrackRepository.Token);
            e.IsValid = !invalid;
            editConfig.HasUIValidationErrors = invalid;
            e.ErrorType = ErrorType.Critical;
            e.ErrorContent = "Setup all properties";
            e.Handled = true;
        }
    }
}
