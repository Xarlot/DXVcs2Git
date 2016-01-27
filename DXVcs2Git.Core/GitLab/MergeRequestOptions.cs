using System.IO;

namespace DXVcs2Git.Core.GitLab {

    public class MergeRequestOptions {
        public static string ConvertToString(MergeRequestOptions options) {
            if (options == null)
                return null;

            using (MemoryStream stream = new MemoryStream()) {
                Serializer.Serialize(stream, options);
                stream.Flush();
                stream.Seek(0, SeekOrigin.Begin);
                using (StreamReader reader = new StreamReader(stream))
                    return reader.ReadToEnd();
            }
        }
        public static MergeRequestOptions ConvertFromString(string str) {
            if (string.IsNullOrEmpty(str))
                return null;
            using (MemoryStream stream = new MemoryStream()) {
                using (StreamWriter writer = new StreamWriter(stream)) {
                    writer.Write(str);
                    writer.Flush();
                    stream.Seek(0, SeekOrigin.Begin);
                    return Serializer.Deserialize<MergeRequestOptions>(stream);
                }
            }
        }
        public bool Force { get; set; }
        public string WatchTask { get; set; }
        public string SyncTask { get; set; }
    }
}