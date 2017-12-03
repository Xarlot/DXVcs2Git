using System.Collections.Generic;
using DXVcs2Git.Core;
using NUnit.Framework;
using Polenter.Serialization;

namespace DXVcs2Git.Tests {
    [TestFixture]
    public class TrackConfigTests {
        [Test]
        public void SparseCheckoutTest() {
            string branchName = "2017.2";
            List<TrackItem> items = new List<TrackItem>();
            items.Add(new TrackItem() { Path = $@"$/{branchName}/XPF/DevExpress.Mvvm", ProjectPath = "DevExpress.Mvvm" });
            items.Add(new TrackItem() { Path = $@"$/{branchName}/XPF/DevExpress.Xpf.Core", ProjectPath = "DevExpress.Xpf.Core" });

            var trackBranch = new TrackBranch(branchName, "test", "test", items);
            foreach (var trackItem in items) {
                trackItem.Branch = trackBranch;
            }

            var result = trackBranch.GetRepoRoot(items[0]);
        }

        [Test, Explicit]
        public void GenerateTestConfig() {
            List<TrackItem> items = new List<TrackItem>();
            items.Add(new TrackItem() { Path = @"$/Sandbox/litvinov/XPF/DevExpress.Xpf.Core", ProjectPath = "DevExpress.Xpf.Core" });
            TrackBranch branch = new TrackBranch("2015.2", "$/Sandbox/litvinov/XPF/track2015.2.config", "$/Sandbox/litvinov/XPF", items);

            SharpSerializerXmlSettings settings = new SharpSerializerXmlSettings();
            settings.IncludeAssemblyVersionInTypeName = false;
            settings.IncludePublicKeyTokenInTypeName = false;
            SharpSerializer serializer = new SharpSerializer(settings);
            serializer.Serialize(new List<TrackBranch>() { branch }, @"c:\1\trackconfig_testxpf.config");
        }
        [Test, Explicit]
        public void GenerateXpfCommon161Config() {
            GenerateXpfCommonConfig("2016.1");
        }
        [Test, Explicit]
        public void GenerateXpfCommon152Config() {
            GenerateXpfCommonConfig("2015.2");
        }
        [Test, Explicit]
        public void GenerateXpfCommon151Config() {
            GenerateXpfCommonConfig("2015.1");
        }
        [Test, Explicit]
        public void GenerateXpfCommon142Config() {
            GenerateXpfCommonConfig("2014.2");
        }
        [Test, Explicit]
        public void GenerateXpfCommon141Config() {
            GenerateXpfCommonConfig("2014.1");
        }
        void GenerateXpfCommonConfig(string branchName) {
            List<TrackItem> items = new List<TrackItem>();
            items.Add(new TrackItem() { Path = $@"$/{branchName}/XPF/DevExpress.Mvvm", ProjectPath = "DevExpress.Mvvm" });
            items.Add(new TrackItem() { Path = $@"$/{branchName}/XPF/DevExpress.Xpf.Core", ProjectPath = "DevExpress.Xpf.Core" });
            items.Add(new TrackItem() { Path = $@"$/{branchName}/XPF/DevExpress.Xpf.Controls", ProjectPath = "DevExpress.Xpf.Controls" });
            items.Add(new TrackItem() { Path = $@"$/{branchName}/XPF/DevExpress.Xpf.Grid", ProjectPath = "DevExpress.Xpf.Grid" });
            items.Add(new TrackItem() { Path = $@"$/{branchName}/XPF/DevExpress.Xpf.NavBar", ProjectPath = "DevExpress.Xpf.NavBar" });
            items.Add(new TrackItem() { Path = $@"$/{branchName}/XPF/DevExpress.Xpf.PropertyGrid", ProjectPath = "DevExpress.Xpf.PropertyGrid" });
            items.Add(new TrackItem() { Path = $@"$/{branchName}/XPF/DevExpress.Xpf.Ribbon", ProjectPath = "DevExpress.Xpf.Ribbon" });
            items.Add(new TrackItem() { Path = $@"$/{branchName}/XPF/DevExpress.Xpf.Layout", ProjectPath = "DevExpress.Xpf.Layout" });
            items.Add(new TrackItem() { Path = $@"$/{branchName}/XPF/DevExpress.Xpf.LayoutControl", ProjectPath = "DevExpress.Xpf.LayoutControl" });
            TrackBranch branch = new TrackBranch($"{branchName}", $@"$/{branchName}/Common/xpf_common_sync.config", $@"$/{branchName}/XPF", items);

            SharpSerializerXmlSettings settings = new SharpSerializerXmlSettings();
            settings.IncludeAssemblyVersionInTypeName = false;
            settings.IncludePublicKeyTokenInTypeName = false;
            SharpSerializer serializer = new SharpSerializer(settings);
            serializer.Serialize(new List<TrackBranch>() { branch }, $@"z:\trackconfig_common_{branchName}.config");

        }
        [Test, Explicit]
        public void GenerateXpfDiagram161Config() {
            GenerateXpfDiagramConfig("2016.1");
        }
        [Test, Explicit]
        public void GenerateXpfDiagram152Config() {
            GenerateXpfDiagramConfig("2015.2");
        }
        void GenerateXpfDiagramConfig(string branchName) {
            List<TrackItem> items = new List<TrackItem>();
            items.Add(new TrackItem() { Path = $@"$/{branchName}/Win/DevExpress.XtraDiagram", ProjectPath = "DevExpress.XtraDiagram", AdditionalOffset = "Win" });
            items.Add(new TrackItem() { Path = $@"$/{branchName}/XPF/DevExpress.Xpf.Diagram", ProjectPath = "DevExpress.Xpf.Diagram", AdditionalOffset = "XPF" });
            items.Add(new TrackItem() { Path = $@"$/{branchName}/XPF/DevExpress.Xpf.ReportDesigner", ProjectPath = "DevExpress.Xpf.ReportDesigner", AdditionalOffset = "XPF" });
            items.Add(new TrackItem() { Path = $@"$/{branchName}/XPF/DevExpress.Xpf.PdfViewer", ProjectPath = "DevExpress.Xpf.PdfViewer", AdditionalOffset = "XPF" });
            items.Add(new TrackItem() { Path = $@"$/{branchName}/XPF/DevExpress.Xpf.Printing", ProjectPath = "DevExpress.Xpf.Printing", AdditionalOffset = "XPF" });
            TrackBranch branch = new TrackBranch($"{branchName}", $@"$/{branchName}/Diagram/xpf_common_sync.config", $@"$/{branchName}", items);

            SharpSerializerXmlSettings settings = new SharpSerializerXmlSettings();
            settings.IncludeAssemblyVersionInTypeName = false;
            settings.IncludePublicKeyTokenInTypeName = false;
            SharpSerializer serializer = new SharpSerializer(settings);
            serializer.Serialize(new List<TrackBranch>() { branch }, $@"z:\trackconfig_diagram_{branchName}.config");

        }

        [Test, Explicit]
        public void GenerateAsp142Config() {
            GenerateAspConfig("2014.2");
        }
        [Test, Explicit]
        public void GenerateAsp151Config() {
            GenerateAspConfig("2015.1");
        }
        [Test, Explicit]
        public void GenerateAsp152Config() {
            GenerateAspConfig("2015.2");
        }

        void GenerateAspConfig(string branchName) {
            List<TrackItem> items = new List<TrackItem>();
            items.Add(new TrackItem() { Path = $@"$/{branchName}/ASP/ASPxThemeBuilder", ProjectPath = "ASPxThemeBuilder" });
            items.Add(new TrackItem() { Path = $@"$/{branchName}/ASP/ASPxThemeDeployer", ProjectPath = "ASPxThemeDeployer" });
            items.Add(new TrackItem() { Path = $@"$/{branchName}/ASP/DevExpress.Web", ProjectPath = "DevExpress.Web" });
            items.Add(new TrackItem() { Path = $@"$/{branchName}/ASP/DevExpress.Web.ASPxHtmlEditor", ProjectPath = "DevExpress.Web.ASPxHtmlEditor" });
            items.Add(new TrackItem() { Path = $@"$/{branchName}/ASP/DevExpress.Web.ASPxRichEdit", ProjectPath = "DevExpress.Web.ASPxRichEdit" });
            items.Add(new TrackItem() { Path = $@"$/{branchName}/ASP/DevExpress.Web.ASPxRichEdit.Tests", ProjectPath = "DevExpress.Web.ASPxRichEdit.Tests" });
            items.Add(new TrackItem() { Path = $@"$/{branchName}/ASP/DevExpress.Web.ASPxScheduler", ProjectPath = "DevExpress.Web.ASPxScheduler" });
            items.Add(new TrackItem() { Path = $@"$/{branchName}/ASP/DevExpress.Web.ASPxSpellChecker", ProjectPath = "DevExpress.Web.ASPxSpellChecker" });
            items.Add(new TrackItem() { Path = $@"$/{branchName}/ASP/DevExpress.Web.ASPxSpreadsheet", ProjectPath = "DevExpress.Web.ASPxSpreadsheet" });
            items.Add(new TrackItem() { Path = $@"$/{branchName}/ASP/DevExpress.Web.ASPxThemes", ProjectPath = "DevExpress.Web.ASPxThemes" });
            items.Add(new TrackItem() { Path = $@"$/{branchName}/ASP/DevExpress.Web.ASPxTreeList", ProjectPath = "DevExpress.Web.ASPxTreeList" });
            items.Add(new TrackItem() { Path = $@"$/{branchName}/ASP/DevExpress.Web.Design", ProjectPath = "DevExpress.Web.Design" });
            items.Add(new TrackItem() { Path = $@"$/{branchName}/ASP/DevExpress.Web.Mvc", ProjectPath = "DevExpress.Web.Mvc" });
            items.Add(new TrackItem() { Path = $@"$/{branchName}/ASP/DevExpress.Web.Projects", ProjectPath = "DevExpress.Web.Projects" });

            TrackBranch branch = new TrackBranch($"{branchName}", $@"$/{branchName}/Diagram/xpf_common_sync.config", $@"$/{branchName}", items);

            SharpSerializerXmlSettings settings = new SharpSerializerXmlSettings();
            settings.IncludeAssemblyVersionInTypeName = false;
            settings.IncludePublicKeyTokenInTypeName = false;
            SharpSerializer serializer = new SharpSerializer(settings);
            serializer.Serialize(new List<TrackBranch>() { branch }, $@"z:\trackconfig_asp_{branchName}.config");
        }

        [Test, Explicit]
        public void GenerateDataAccess161Config() {
            GenerateDataAccessConfig("2016.1");
        }
        [Test, Explicit]
        public void GenerateDataAccess152Config() {
            GenerateDataAccessConfig("2015.2");
        }
        [Test, Explicit]
        public void GenerateDataAccess151Config() {
            GenerateDataAccessConfig("2015.1");
        }
        void GenerateDataAccessConfig(string branchName) {
            List<TrackItem> items = new List<TrackItem>();
            items.Add(new TrackItem() { Path = $@"$/{branchName}/Win/DevExpress.DataAccess", ProjectPath = "DevExpress.DataAccess", AdditionalOffset = "Win" });
            items.Add(new TrackItem() { Path = $@"$/{branchName}/Tests.DataAccess/BigQueryTests", ProjectPath = "BigQueryTests", AdditionalOffset = "Tests.DataAccess" });
            items.Add(new TrackItem() { Path = $@"$/{branchName}/Tests.DataAccess/DataAccessTests", ProjectPath = "DataAccessTests", AdditionalOffset = "Tests.DataAccess" });
            items.Add(new TrackItem() { Path = $@"$/{branchName}/Tests.DataAccess/EntityFramework7Tests", ProjectPath = "EntityFramework7Tests", AdditionalOffset = "Tests.DataAccess" });
            items.Add(new TrackItem() { Path = $@"$/{branchName}/Tests.DataAccess/MsSqlCEEF7Tests", ProjectPath = "MsSqlCEEF7Tests", AdditionalOffset = "Tests.DataAccess" });
            items.Add(new TrackItem() { Path = $@"$/{branchName}/Tests.DataAccess/MsSqlEF5Tests", ProjectPath = "MsSqlEF5Tests", AdditionalOffset = "Tests.DataAccess" });
            items.Add(new TrackItem() { Path = $@"$/{branchName}/Tests.DataAccess/MsSqlEF6Tests", ProjectPath = "MsSqlEF6Tests", AdditionalOffset = "Tests.DataAccess" });
            items.Add(new TrackItem() { Path = $@"$/{branchName}/Tests.DataAccess/MsSqlEF7Tests", ProjectPath = "MsSqlEF7Tests", AdditionalOffset = "Tests.DataAccess" });
            items.Add(new TrackItem() { Path = $@"$/{branchName}/Tests.DataAccess/MySqlEF6Tests", ProjectPath = "MySqlEF6Tests", AdditionalOffset = "Tests.DataAccess" });
            items.Add(new TrackItem() { Path = $@"$/{branchName}/Tests.DataAccess/PostgreSqlEF7Tests", ProjectPath = "PostgreSqlEF7Tests", AdditionalOffset = "Tests.DataAccess" });
            items.Add(new TrackItem() { Path = $@"$/{branchName}/Tests.DataAccess/PostgreSqlEF7Tests", ProjectPath = "PostgreSqlEF7Tests", AdditionalOffset = "Tests.DataAccess" });
            items.Add(new TrackItem() { Path = $@"$/{branchName}/Tests.DataAccess/SqliteEF7Tests", ProjectPath = "SqliteEF7Tests", AdditionalOffset = "Tests.DataAccess" });
            items.Add(new TrackItem() { Path = $@"$/{branchName}/Tests.DataAccess/WizardTests", ProjectPath = "WizardTests", AdditionalOffset = "Tests.DataAccess" });
            TrackBranch branch = new TrackBranch($"{branchName}", $@"$/{branchName}/DataAccess/sync.config", $@"$/{branchName}", items);

            SharpSerializerXmlSettings settings = new SharpSerializerXmlSettings();
            settings.IncludeAssemblyVersionInTypeName = false;
            settings.IncludePublicKeyTokenInTypeName = false;
            SharpSerializer serializer = new SharpSerializer(settings);
            serializer.Serialize(new List<TrackBranch>() { branch }, $@"z:\trackconfig_dataaccess_{branchName}.config");

        }
        [Test, Explicit]
        public void GenerateUWP141Config() {
            GenerateWinRTConfig("2014.1");
        }
        void GenerateWinRTConfig(string branchName) {
            List<TrackItem> items = new List<TrackItem>();
            items.Add(new TrackItem() { Path = $@"$/{branchName}/WinRT/DevExpress.Core", ProjectPath = "DevExpress.Core" });
            items.Add(new TrackItem() { Path = $@"$/{branchName}/WinRT/DevExpress.Drawing", ProjectPath = "DevExpress.Drawing" });
            items.Add(new TrackItem() { Path = $@"$/{branchName}/WinRT/DevExpress.Pdf.Core", ProjectPath = "DevExpress.Pdf.Core" });
            items.Add(new TrackItem() { Path = $@"$/{branchName}/WinRT/DevExpress.TestFramework", ProjectPath = "DevExpress.TestFramework" });
            items.Add(new TrackItem() { Path = $@"$/{branchName}/WinRT/DevExpress.TestRunner", ProjectPath = "DevExpress.TestRunner" });
            items.Add(new TrackItem() { Path = $@"$/{branchName}/WinRT/DevExpress.TestsKey", ProjectPath = "DevExpress.TestsKey" });
            items.Add(new TrackItem() { Path = $@"$/{branchName}/WinRT/DevExpress.TestUtils", ProjectPath = "DevExpress.TestUtils" });
            items.Add(new TrackItem() { Path = $@"$/{branchName}/WinRT/DevExpress.UI.Xaml", ProjectPath = "DevExpress.UI.Xaml" });
            items.Add(new TrackItem() { Path = $@"$/{branchName}/WinRT/DevExpress.UI.Xaml.Charts", ProjectPath = "DevExpress.UI.Xaml.Charts" });
            items.Add(new TrackItem() { Path = $@"$/{branchName}/WinRT/DevExpress.UI.Xaml.Controls", ProjectPath = "DevExpress.UI.Xaml.Controls" });
            items.Add(new TrackItem() { Path = $@"$/{branchName}/WinRT/DevExpress.UI.Xaml.Editors", ProjectPath = "DevExpress.UI.Xaml.Editors" });
            items.Add(new TrackItem() { Path = $@"$/{branchName}/WinRT/DevExpress.UI.Xaml.Gauges", ProjectPath = "DevExpress.UI.Xaml.Gauges" });
            items.Add(new TrackItem() { Path = $@"$/{branchName}/WinRT/DevExpress.UI.Xaml.Grid", ProjectPath = "DevExpress.UI.Xaml.Grid" });
            items.Add(new TrackItem() { Path = $@"$/{branchName}/WinRT/DevExpress.UI.Xaml.Layout", ProjectPath = "DevExpress.UI.Xaml.Layout" });
            items.Add(new TrackItem() { Path = $@"$/{branchName}/WinRT/DevExpress.UI.Xaml.Map", ProjectPath = "DevExpress.UI.Xaml.Map" });
            items.Add(new TrackItem() { Path = $@"$/{branchName}/WinRT/DevExpress.WinRT.Projects.Installers", ProjectPath = "DevExpress.WinRT.Projects.Installers" });
            items.Add(new TrackItem() { Path = $@"$/{branchName}/WinRT/DevExpress.WinRT.Projects.Wizards", ProjectPath = "DevExpress.WinRT.Projects.Wizards" });
            items.Add(new TrackItem() { Path = $@"$/{branchName}/WinRT/Shared", ProjectPath = "Shared" });
            items.Add(new TrackItem() { Path = $@"$/{branchName}/WinRT/Tools", ProjectPath = "Tools" });
            TrackBranch branch = new TrackBranch($"{branchName}", $@"$/{branchName}/UWP.GIT/sync.config", $@"$/{branchName}", items);

            SharpSerializerXmlSettings settings = new SharpSerializerXmlSettings();
            settings.IncludeAssemblyVersionInTypeName = false;
            settings.IncludePublicKeyTokenInTypeName = false;
            SharpSerializer serializer = new SharpSerializer(settings);
            serializer.Serialize(new List<TrackBranch>() { branch }, $@"z:\trackconfig_uwp_{branchName}.config");

        }

        [Test, Explicit]
        public void GenerateUWPDemosConfig() {
            GenerateUWPDemoConfig("2016.1");
        }
        void GenerateUWPDemoConfig(string branchName) {
            List<TrackItem> items = new List<TrackItem>();
            items.Add(new TrackItem() { Path = $@"$/{branchName}/Demos.UWP/DemoLauncher", ProjectPath = "DemoLauncher" });
            items.Add(new TrackItem() { Path = $@"$/{branchName}/Demos.UWP/DevExpress.PackageRegistrator", ProjectPath = "DevExpress.PackageRegistrator" });
            items.Add(new TrackItem() { Path = $@"$/{branchName}/Demos.UWP/DXCRM", ProjectPath = "DXCRM" });
            items.Add(new TrackItem() { Path = $@"$/{branchName}/Demos.UWP/FeatureDemo", ProjectPath = "FeatureDemo" });
            items.Add(new TrackItem() { Path = $@"$/{branchName}/Demos.UWP/FinanceTracker", ProjectPath = "FinanceTracker" });
            items.Add(new TrackItem() { Path = $@"$/{branchName}/Demos.UWP/HybridDemo", ProjectPath = "HybridDemo" });
            TrackBranch branch = new TrackBranch($"{branchName}", $@"$/{branchName}/Demos.UWP.GIT/sync.config", $@"$/{branchName}", items);

            SharpSerializerXmlSettings settings = new SharpSerializerXmlSettings();
            settings.IncludeAssemblyVersionInTypeName = false;
            settings.IncludePublicKeyTokenInTypeName = false;
            SharpSerializer serializer = new SharpSerializer(settings);
            serializer.Serialize(new List<TrackBranch>() { branch }, $@"z:\trackconfig_uwp_demos_{branchName}.config");

        }
    }
}
