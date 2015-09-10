using System;
using System.IO;

namespace DXVcs2Git.DXVcs {
    public class FileSystem {
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
            using (Stream r = OpenRead(path)) {
                byte[] buffer = new byte[r.Length];
                r.Read(buffer, 0, (int)r.Length);
                return buffer;
            }
        }
        /*public class CachedStream : Stream {
            byte[] buffer;
            int bufferSize;
            int len;
            int pos;
            Stream stream;

            public CachedStream(Stream stream, int bufferSize) {
                this.stream = stream;
                this.bufferSize = bufferSize;
            }
            protected override void Dispose(bool disposing) {
                if(disposing)
                    stream.Dispose();
                base.Dispose(disposing);
            }
            public override void Flush() {
            }
            public override int Read(byte[] array, int offset, int count) {
                if(count > bufferSize) {
                    int i;
                    for(i = 0; i < count - bufferSize; i += bufferSize) {
                        int n = Read(array, offset + i, bufferSize);
                        if(n < bufferSize)
                            return i + n;
                    }
                    return i + Read(array, offset + i, count - i);
                }
                if(buffer == null) {
                    buffer = new byte[bufferSize];
                    len = stream.Read(buffer, 0, bufferSize);
                    pos = 0;
                }
                int num = len - pos;
                if(num < count) {
                    if(num > 0)
                        Array.Copy(buffer, pos, array, offset, num);
                    if(len < bufferSize) {
                        pos += num;
                        return num;
                    }
                    num = stream.Read(buffer, 0, bufferSize);
                    pos = 0;
                    len = num;
                }
                if(num > count)
                    num = count;
                Array.Copy(buffer, pos, array, offset, num);
                pos += num;
                return num;
            }
            public override long Seek(long offset, SeekOrigin origin) {
                throw new NotSupportedException();
            }
            public override void SetLength(long value) {
                throw new NotSupportedException();
            }
            public override void Write(byte[] array, int offset, int count) {
                throw new NotSupportedException();
            }
            public override bool CanRead { get { return true; } }
            public override bool CanSeek { get { return false; } }
            public override bool CanWrite { get { return false; } }
            public override long Length { get { return stream.Length; } }
            public override long Position {
                get {
                    return stream.Position + pos - len;
                }
                set {
                    throw new NotSupportedException();
                }
            }
        }*/
        public Stream OpenRead(string path) {
            const int BufferSize = 64 * 1024;
            /*const int FILE_FLAG_NO_BUFFERING = 0x20000000;
            return new CachedStream(new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, BufferSize, (FileOptions)FILE_FLAG_NO_BUFFERING | FileOptions.SequentialScan), BufferSize);*/
            return new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, BufferSize, FileOptions.SequentialScan);
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
            FileInfo fi = new FileInfo(path);
            lastWriteTime = fi.Exists ? fi.LastWriteTime : DateTime.MinValue;
            return fi.Exists;
        }

        public DateTime GetLastWriteTimeUtc(string path) {
            return File.GetLastWriteTimeUtc(path);
        }

        public bool GetLastWriteTimeUtc(string path, out DateTime lastWriteTimeUtc) {
            FileInfo fi = new FileInfo(path);
            lastWriteTimeUtc = fi.Exists ? fi.LastWriteTimeUtc : DateTime.MinValue;
            return fi.Exists;
        }

        public void WriteAllBytes(string path, byte[] bytes) {
            if (File.Exists(path)) {
                File.SetAttributes(path, FileAttributes.Normal);
            }
            FileInfo fi = new FileInfo(path);
            if (!fi.Directory.Exists) { fi.Directory.Create(); }
            File.WriteAllBytes(path, bytes);
        }

        public void WriteAllBytes(string path, byte[] bytes, FileAttributes attributes) {
            if (File.Exists(path)) {
                File.SetAttributes(path, FileAttributes.Normal);
            }
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
                    if (Exists(path)) {
                        File.SetAttributes(path, attributes);
                    }
                }
                catch {
                    continue;
                }
            }
        }

        public FileAttributes GetAttributes(string path) {
            return File.GetAttributes(path);
        }
        public bool GetAttributes(string path, out FileAttributes attributes, out DateTime lastWriteTime) {
            FileInfo fi = new FileInfo(path);
            attributes = fi.Exists ? fi.Attributes : 0;
            lastWriteTime = fi.Exists ? fi.LastWriteTimeUtc : DateTime.MinValue;
            return fi.Exists;
        }
        public void Delete(string path) {
            File.SetAttributes(path, FileAttributes.Normal);
            File.Delete(path);
        }
        public void WriteAllLines(string path, string[] lines) {
            if (File.Exists(path)) {
                File.SetAttributes(path, FileAttributes.Normal);
            }
            File.WriteAllLines(path, lines);
        }

        public void WriteAllLines(string path, string[] lines, FileAttributes attributes) {
            if (File.Exists(path)) {
                File.SetAttributes(path, FileAttributes.Normal);
            }
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
