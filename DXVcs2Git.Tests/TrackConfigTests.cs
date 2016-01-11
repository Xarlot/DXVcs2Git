using System.Collections.Generic;
using DXVcs2Git.Core;
using NUnit.Framework;
using Polenter.Serialization;

namespace DXVcs2Git.Tests {
    [TestFixture]
    public class TrackConfigTests {
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
            items.Add(new TrackItem() { Path = $@"$/{branchName}/Win/DevExpress.XtraDiagram", ProjectPath = "DevExpress.XtraDiagram", AdditionalOffset = "Win"});
            items.Add(new TrackItem() { Path = $@"$/{branchName}/XPF/DevExpress.Xpf.Diagram", ProjectPath = "DevExpress.Xpf.Diagram", AdditionalOffset = "XPF"});
            items.Add(new TrackItem() { Path = $@"$/{branchName}/XPF/DevExpress.Xpf.ReportDesigner", ProjectPath = "DevExpress.Xpf.ReportDesigner", AdditionalOffset = "XPF"});
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
            items.Add(new TrackItem() { Path = $@"$/{branchName}/Tests.DataAccess", ProjectPath = "Tests.DataAccess" });
            TrackBranch branch = new TrackBranch($"{branchName}", $@"$/{branchName}/DataAccess/sync.config", $@"$/{branchName}", items);

            SharpSerializerXmlSettings settings = new SharpSerializerXmlSettings();
            settings.IncludeAssemblyVersionInTypeName = false;
            settings.IncludePublicKeyTokenInTypeName = false;
            SharpSerializer serializer = new SharpSerializer(settings);
            serializer.Serialize(new List<TrackBranch>() { branch }, $@"z:\trackconfig_dataaccess_{branchName}.config");

        }


    }
}
