using Polenter.Serialization;

namespace DXVcs2Git.Core {
    public class TrackItem {
        [ExcludeFromSerialization]
        public TrackBranch Branch { get; internal set; }
        public bool GoDeeper { get; set; }
        public string Path { get; set; }
        public string ProjectPath { get; set; }
        public string AdditionalOffset { get; set; }
        public bool IsFile { get; set; }
    }
}
