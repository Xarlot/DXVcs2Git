using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DXVcs2Git.Core {
    public class TrackItem {
        public string Branch { get; set; }
        public string Path { get; set; }
        public string FullPath { get { return $"$/{Branch}/{Path}"; } }
    }
}
