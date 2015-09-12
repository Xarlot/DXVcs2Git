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
    public class TrackConfigTest {
        readonly List<string> directories = new List<string>();
        string GenerateConfig() {
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
        [Test]
        public void ReadConfig() {
            string config = GenerateConfig();
            TrackConfig track = new TrackConfig(config); 
            Assert.AreEqual(1, track.TrackItems.Count);
        }
    }
}
