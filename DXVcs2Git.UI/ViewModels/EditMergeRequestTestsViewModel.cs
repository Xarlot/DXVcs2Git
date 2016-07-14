using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Input;
using DevExpress.Mvvm;
using DevExpress.Mvvm.Native;
using DXVcs2Git.Core.Git;
using DXVcs2Git.Core.GitLab;
using DXVcs2Git.UI.Farm;
using Microsoft.Practices.ServiceLocation;

namespace DXVcs2Git.UI.ViewModels {
    public class EditMergeRequestTestsViewModel : ViewModelBase {
        RepositoriesViewModel RepositoriesViewModel => ServiceLocator.Current.GetInstance<RepositoriesViewModel>();

        public ICommand RunTestsCommand { get; }
        public ICommand CancelTestsCommand { get; }

        public EditMergeRequestTestsViewModel() {
            Messenger.Default.Register<Message>(this, OnMessageReceived);
            RunTestsCommand = DelegateCommandFactory.Create(PerformRunTests, CanPerformRunTests);
            CancelTestsCommand = DelegateCommandFactory.Create(PerformCancelTests, CanPerformCancelTests);

            Initialize();
        }
        bool CanPerformCancelTests() {
            return false;
        }
        void PerformCancelTests() {
        }
        BranchViewModel BranchViewModel { get; set; }

        public bool IsTestsRunning {
            get { return GetProperty(() => IsTestsRunning); }
            private set { SetProperty(() => IsTestsRunning, value); }
        }
        void OnMessageReceived(Message msg) {
            if (msg.MessageType == MessageType.RefreshFarm)
                RefreshFarmStatus();
        }
        void RefreshFarmStatus() {
            foreach (var testViewModel in Tests) {
                testViewModel.RefreshFarmStatus();
            }
        }
        void Initialize() {
            BranchViewModel = RepositoriesViewModel.SelectedBranch;
            if (BranchViewModel == null) {
                Tests = Enumerable.Empty<TestViewModel>();
                return;
            }
            Tests = BranchViewModel.Repository.TestConfigs.Select(x => new TestViewModel() {Name = x.Name, DisplayName = x.DisplayName, TestConfig = x}).ToList();
        }
        bool CanPerformRunTests() {
            return BranchViewModel?.MergeRequest != null && Tests.Any(x => x.RunTest);
        }
        void PerformRunTests() {
            IsTestsRunning = true;

            var mergeRequest = BranchViewModel.MergeRequest;
            var action = new MergeRequestTestBuildAction(mergeRequest.MergeRequestId, Tests.Where(x => x.RunTest).Select(x => x.TestConfig).ToArray());
            MergeRequestOptions options = new MergeRequestOptions(action);
            BranchViewModel.UpdateMergeRequest(MergeRequestOptions.ConvertToString(options));
        }
        public IEnumerable<TestViewModel> Tests {
            get { return GetProperty(() => Tests); }
            private set { SetProperty(() => Tests, value); }
        }
    }

    public class TestViewModel : BindableBase {
        public bool RunTest {
            get { return GetProperty(() => RunTest); }
            set { SetProperty(() => RunTest, value); }
        }
        public string Name { get; set; }
        public string DisplayName { get; set; }
        public FarmStatus FarmStatus { get; set; }
        public TestConfig TestConfig { get; set; }
        public void RefreshFarmStatus() {

        }
    }
}
