using Polenter.Serialization;

namespace DXVcs2Git.Core {
    public class TrackItem {
        [ExcludeFromSerialization]
        public string Branch { get; internal set; }
        public string Path { get; set; }
        public string ProjectPath { get; set; }
    }
}
