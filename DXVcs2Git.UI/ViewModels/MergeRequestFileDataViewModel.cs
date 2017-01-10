using DevExpress.Mvvm;
using NGitLab.Models;

namespace DXVcs2Git.UI.ViewModels {
    public enum FileChangeMode {
        New,
        Deleted,
        Modified,
        Renamed,
    }
    public class MergeRequestFileDataViewModel : BindableBase {
        public FileChangeMode ChangeMode { get; private set; }
        public string Path { get; private set; }
        public string Diff { get; private set; }
        public string FileName { get; private set; }
        public string DirName { get; private set; }

        readonly MergeRequestFileData fileData;
        public MergeRequestFileDataViewModel(MergeRequestFileData fileData) {
            this.fileData = fileData;
            ChangeMode = CalcChangeMode();
            Path = fileData.OldPath;
            Diff = fileData.Diff;
            FileName = System.IO.Path.GetFileName(Path);
            DirName = System.IO.Path.GetDirectoryName(Path);
        }
        FileChangeMode CalcChangeMode() {
            if (this.fileData.IsDeleted)
                return FileChangeMode.Deleted;
            if (this.fileData.IsNew)
                return FileChangeMode.New;
            if (this.fileData.IsRenamed)
                return FileChangeMode.Renamed;
            return FileChangeMode.Modified;
        }
    }
}
