namespace DXVcs2Git.Core {
    public class TrackItem {
        public string Branch { get; set; }
        public string Path { get; set; }
        public string RelativeLocalPath { get; set; }
        public string FullPath { get { return $"$/{Branch}/{Path}"; } }
    }
}
