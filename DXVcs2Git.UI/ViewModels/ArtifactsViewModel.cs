using System;
using System.IO;
using System.IO.Packaging;
using System.Text;
using DevExpress.Mvvm;
using Ionic.Zip;
using NGitLab.Models;

namespace DXVcs2Git.UI.ViewModels {
    public class ArtifactsViewModel : BindableBase {
        const string buildlogpath = @".patch/buildlog.xml";
        const string testlogpath = @".patch/testlog.xml";
        const string modificationspath = @".patch/modifications.xml";
        readonly ArtifactsFile file;
        readonly byte[] fileContent;
        public bool HasContent => file != null && fileContent != null;

        public string BuildLog { get; private set; }
        public string TestLog { get; private set; }
        public string Modifications { get; private set; }
        public ArtifactsViewModel(ArtifactsFile file, byte[] content = null) {
            this.file = file;
            this.fileContent = content;
            Parse();
        }
        void Parse() {
            if (!HasContent)
                return;
            using (var stream = new MemoryStream(fileContent)) {
                using (ZipFile zipFile = ZipFile.Read(stream)) {
                    BuildLog = GetPartContent(zipFile, buildlogpath);
                    TestLog = GetPartContent(zipFile, testlogpath);
                    Modifications = GetPartContent(zipFile, modificationspath);
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
