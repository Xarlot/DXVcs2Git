using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DXVcs2Git.Core.Configuration {
    public class TrackRepository {
        public bool Watch { get; set; }
        public string Name { get; set; }
        public string LocalPath { get; set; }
    }
}
