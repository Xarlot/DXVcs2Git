using System.IO;
using System.Text;
using DevExpress.Mvvm;
using Ionic.Zip;
using NGitLab.Models;
using System.IO.Compression;

namespace DXVcs2Git.UI.ViewModels {
    public class ArtifactsViewModel : BindableBase {
        const string workerlogpath = @".patch/workerlog.xml";
        const string modificationspath = @".patch/modifications.xml";
        readonly ArtifactsFile file;
        readonly byte[] fileContent;
        readonly byte[] trace;
        public bool HasContent => file != null && fileContent != null;
        public bool HasTrace => trace != null;


        public string WorkerLog { get; private set; }
        public string Modifications { get; private set; }
        public string Trace { get; private set; }
        public ArtifactsViewModel(ArtifactsFile file, byte[] content = null, byte[] trace = null) {
            this.file = file;
            this.fileContent = content;
            this.trace = trace;
            Parse();
        }
        void Parse() {
            if (HasContent) {
                using (var stream = new MemoryStream(fileContent)) {
                    using (ZipFile zipFile = ZipFile.Read(stream)) {
                        WorkerLog = GetPartContent(zipFile, workerlogpath);
                        Modifications = GetPartContent(zipFile, modificationspath);
                    }
                }
            }
            if (HasTrace) {
                using (MemoryStream stream = new MemoryStream(trace)) {
                    using (GZipStream compressed = new GZipStream(stream, CompressionMode.Decompress)) {
                        const int size = 4096;
                        byte[] buffer = new byte[size];
                        using (MemoryStream memory = new MemoryStream()) {
                            int count = 0;
                            do {
                                count = stream.Read(buffer, 0, size);
                                if (count > 0) {
                                    memory.Write(buffer, 0, count);
                                }
                            }
                            while (count > 0);
                            Trace = Encoding.UTF8.GetString(memory.ToArray());
                        }
                    }
                }
            }
        }
        string GetPartContent(ZipFile zip, string partPath) {
            var part = zip[partPath];
            if (part == null)
                return null;
            using (var memoryStream = new MemoryStream()) {
                part.Extract(memoryStream);
                var str =  Encoding.UTF8.GetString(memoryStream.ToArray());
                return str;
            }
        }
    }
}
