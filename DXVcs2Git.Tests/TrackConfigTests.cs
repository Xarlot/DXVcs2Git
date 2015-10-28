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
        public void GenerateXpfCommonConfig() {
            List<TrackItem> items = new List<TrackItem>();
            items.Add(new TrackItem() { Path = @"$/2015.2/XPF/DevExpress.Mvvm", ProjectPath = "DevExpress.Mvvm" });
            items.Add(new TrackItem() { Path = @"$/2015.2/XPF/DevExpress.Xpf.Core", ProjectPath = "DevExpress.Xpf.Core" });
            items.Add(new TrackItem() { Path = @"$/2015.2/XPF/DevExpress.Xpf.Controls", ProjectPath = "DevExpress.Xpf.Controls" });
            items.Add(new TrackItem() { Path = @"$/2015.2/XPF/DevExpress.Xpf.Grid", ProjectPath = "DevExpress.Xpf.Grid" });
            items.Add(new TrackItem() { Path = @"$/2015.2/XPF/DevExpress.Xpf.NavBar", ProjectPath = "DevExpress.Xpf.NavBar" });
            items.Add(new TrackItem() { Path = @"$/2015.2/XPF/DevExpress.Xpf.PropertyGrid", ProjectPath = "DevExpress.Xpf.PropertyGrid" });
            items.Add(new TrackItem() { Path = @"$/2015.2/XPF/DevExpress.Xpf.Ribbon", ProjectPath = "DevExpress.Xpf.Ribbon" });
            TrackBranch branch = new TrackBranch("2015.2", "$/2015.2/xpf_common_sync.config", "$/2015.2/XPF", items);

            SharpSerializerXmlSettings settings = new SharpSerializerXmlSettings();
            settings.IncludeAssemblyVersionInTypeName = false;
            settings.IncludePublicKeyTokenInTypeName = false;
            SharpSerializer serializer = new SharpSerializer(settings);
            serializer.Serialize(new List<TrackBranch>() { branch }, @"z:\trackconfig_common.config");
        }
    }
}
