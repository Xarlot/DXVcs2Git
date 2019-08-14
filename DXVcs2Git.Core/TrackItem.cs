using Polenter.Serialization;

namespace DXVcs2Git.Core {
    public class TrackItem {
        [ExcludeFromSerialization]
        public TrackBranch Branch { get; set; }
        public bool GoDeeper { get; set; }
        protected bool Equals(TrackItem other) {
            return GoDeeper == other.GoDeeper && string.Equals(Path, other.Path) && string.Equals(ProjectPath, other.ProjectPath) && string.Equals(AdditionalOffset, other.AdditionalOffset) && IsFile == other.IsFile;
        }
        public override bool Equals(object obj) {
            if (ReferenceEquals(null, obj))
                return false;
            if (ReferenceEquals(this, obj))
                return true;
            if (obj.GetType() != this.GetType())
                return false;
            return Equals((TrackItem)obj);
        }
        public override int GetHashCode() {
            unchecked {
                var hashCode = GoDeeper.GetHashCode();
                hashCode = (hashCode * 397) ^ (Path != null ? Path.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (ProjectPath != null ? ProjectPath.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (AdditionalOffset != null ? AdditionalOffset.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ IsFile.GetHashCode();
                hashCode = (hashCode * 397) ^ Force.GetHashCode();
                return hashCode;
            }
        }
        public string Path { get; set; }
        public string ProjectPath { get; set; }
        public string AdditionalOffset { get; set; }
        public bool IsFile { get; set; }
        public bool Force { get; set; }
    }
}
