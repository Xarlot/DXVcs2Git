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
        public void GenerateXpfCommon152Config() {
            GenerateXpfCommonCongfig("2015.2");
        }
        [Test, Explicit]
        public void GenerateXpfCommon151Config() {
            GenerateXpfCommonCongfig("2015.1");
        }
        [Test, Explicit]
        public void GenerateXpfCommon142Config() {
            GenerateXpfCommonCongfig("2014.2");
        }
        void GenerateXpfCommonCongfig(string branchName) {
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
            TrackBranch branch = new TrackBranch($"{branchName}", $@"$/{branchName}/xpf_common_sync.config", $@"$/{branchName}/XPF", items);

            SharpSerializerXmlSettings settings = new SharpSerializerXmlSettings();
            settings.IncludeAssemblyVersionInTypeName = false;
            settings.IncludePublicKeyTokenInTypeName = false;
            SharpSerializer serializer = new SharpSerializer(settings);
            serializer.Serialize(new List<TrackBranch>() { branch }, $@"z:\trackconfig_common_{branchName}.config");

        }
    }
}
