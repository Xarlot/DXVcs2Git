using System.IO;
using System.Linq;

namespace DXVcs2Git.Core {
    public static class DirectoryHelper {
        public static bool IsGitDir(string path) {
            if (!Directory.Exists(path))
                return false;
            return Directory.EnumerateDirectories(path).Any(x => Path.GetFileName(x) == ".git");
        }
    }
}
