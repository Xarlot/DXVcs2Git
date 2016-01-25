using System;
using System.IO;

namespace DXVcs2Git.Core.Farm {
    namespace DXVCSClient {
        public class FileSystem  {
            public void CreateFile(string path) {
                File.Create(path);
            }

            public void CreateFile(string path, FileAttributes fileAttribute, byte[] fileData) {
                File.Create(path);
                File.WriteAllBytes(path, fileData);
                File.SetAttributes(path, fileAttribute);
            }

            public void AppendAllText(string path, string contents) {
                File.AppendAllText(path, contents);
            }

            public byte[] ReadAllBytes(string path) {
                using (Stream stream = this.OpenRead(path)) {
                    byte[] buffer = new byte[stream.Length];
                    stream.Read(buffer, 0, (int)stream.Length);
                    return buffer;
                }
            }

            public Stream OpenRead(string path) {
                return (Stream)new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 65536, FileOptions.SequentialScan);
            }

            public void SetLastWriteTime(string path, DateTime date) {
                File.SetLastWriteTime(path, date);
            }

            public void SetLastWriteTimeUtc(string path, DateTime date) {
                File.SetLastWriteTimeUtc(path, date);
            }

            public DateTime GetLastWriteTime(string path) {
                return File.GetLastWriteTime(path);
            }

            public bool GetLastWriteTime(string path, out DateTime lastWriteTime) {
                FileInfo fileInfo = new FileInfo(path);
                lastWriteTime = fileInfo.Exists ? fileInfo.LastWriteTime : DateTime.MinValue;
                return fileInfo.Exists;
            }

            public DateTime GetLastWriteTimeUtc(string path) {
                return File.GetLastWriteTimeUtc(path);
            }

            public bool GetLastWriteTimeUtc(string path, out DateTime lastWriteTimeUtc) {
                FileInfo fileInfo = new FileInfo(path);
                lastWriteTimeUtc = fileInfo.Exists ? fileInfo.LastWriteTimeUtc : DateTime.MinValue;
                return fileInfo.Exists;
            }

            public void WriteAllBytes(string path, byte[] bytes) {
                if (File.Exists(path))
                    File.SetAttributes(path, FileAttributes.Normal);
                FileInfo fileInfo = new FileInfo(path);
                if (!fileInfo.Directory.Exists)
                    fileInfo.Directory.Create();
                File.WriteAllBytes(path, bytes);
            }

            public void WriteAllBytes(string path, byte[] bytes, FileAttributes attributes) {
                if (File.Exists(path))
                    File.SetAttributes(path, FileAttributes.Normal);
                File.WriteAllBytes(path, bytes);
                File.SetAttributes(path, attributes);
            }

            public bool Exists(string path) {
                return File.Exists(path);
            }

            public string[] GetFiles(string path) {
                return Directory.GetFiles(path);
            }

            public string[] GetFilesRecursive(string path) {
                return Directory.GetFiles(path, "*", SearchOption.AllDirectories);
            }

            public string[] GetDirectories(string path) {
                return Directory.GetDirectories(path);
            }

            public string[] GetDirectoriesRecursive(string path) {
                return Directory.GetDirectories(path, "*", SearchOption.AllDirectories);
            }

            public void SetAttributes(string path, FileAttributes attributes) {
                File.SetAttributes(path, attributes);
            }

            public void SetAttributes(string[] paths, FileAttributes attributes) {
                foreach (string path in paths) {
                    try {
                        if (this.Exists(path))
                            File.SetAttributes(path, attributes);
                    }
                    catch {
                    }
                }
            }

            public FileAttributes GetAttributes(string path) {
                return File.GetAttributes(path);
            }

            public bool GetAttributes(string path, out FileAttributes attributes, out DateTime lastWriteTime) {
                FileInfo fileInfo = new FileInfo(path);
                attributes = fileInfo.Exists ? fileInfo.Attributes : (FileAttributes)0;
                lastWriteTime = fileInfo.Exists ? fileInfo.LastWriteTimeUtc : DateTime.MinValue;
                return fileInfo.Exists;
            }

            public void Delete(string path) {
                File.SetAttributes(path, FileAttributes.Normal);
                File.Delete(path);
            }

            public void WriteAllLines(string path, string[] lines) {
                if (File.Exists(path))
                    File.SetAttributes(path, FileAttributes.Normal);
                File.WriteAllLines(path, lines);
            }

            public void WriteAllLines(string path, string[] lines, FileAttributes attributes) {
                if (File.Exists(path))
                    File.SetAttributes(path, FileAttributes.Normal);
                File.WriteAllLines(path, lines);
                File.SetAttributes(path, attributes);
            }

            public string[] ReadAllLines(string path) {
                return File.ReadAllLines(path);
            }

            public void CreateDirectory(string path) {
                Directory.CreateDirectory(path);
            }

            public bool DirectoryExists(string path) {
                return Directory.Exists(path);
            }
        }
    }
}
