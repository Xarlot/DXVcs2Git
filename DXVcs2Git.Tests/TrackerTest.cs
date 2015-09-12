using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DXVcs2Git.Core;
using DXVcs2Git.Tests.TestHelpers;
using NUnit.Framework;

namespace DXVcs2Git.Tests {
    [TestFixture]
    public class TrackerTest {
        readonly List<string> directories = new List<string>();
        string GenerateTestConfig() {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine(@"$\test\");
            sb.AppendLine(@"$$\test\");
            sb.AppendLine(@"$$\test$\");
            sb.AppendLine(@"$\2015\");
            sb.AppendLine(@"$\2015.1\");
            sb.AppendLine(@"$\2015.1\XPF");
            sb.AppendLine(@"$/test/");
            sb.AppendLine(@"$$/test/");
            sb.AppendLine(@"$$/test$/");
            sb.AppendLine(@"$/2015/");
            sb.AppendLine(@"$/2015.1/");
            sb.AppendLine(@"$/2015.1/XPF");
            sb.AppendLine(@"$/20 15.1/");
            return sb.ToString();
        }
        string GenerateTest2Config() {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine(@"$/2015.1/XPF/DevExpress.Mvvm/DevExpress.Mvvm/Controllers/WizardController/Interfaces/");
            sb.AppendLine(@"$/2015.2/XPF/DevExpress.Mvvm/DevExpress.Mvvm/Controllers/WizardController/Interfaces/");
            return sb.ToString();
        }
        [Test]
        public void ReadTestConfig() {
            string config = GenerateTestConfig();
            TrackConfig track = new TrackConfig(config); 
            Assert.AreEqual(1, track.TrackItems.Count);
            Assert.AreEqual("XPF", track.TrackItems[0].Path);
            Assert.AreEqual("2015.1", track.TrackItems[0].Branch);
        }
        [Test]
        public void CreateTracker() {
            string config = GenerateTest2Config();
            TrackConfig track = new TrackConfig(config);
            Tracker tracker = new Tracker(track.TrackItems);
            Assert.AreEqual(2, tracker.Branches.Count);
            Assert.AreEqual("2015.1", tracker.Branches[0].Name);
            Assert.AreEqual("2015.2", tracker.Branches[1].Name);
        }
    }
}
