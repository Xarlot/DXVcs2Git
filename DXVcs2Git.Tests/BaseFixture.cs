using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DXVcs2Git.Tests.TestHelpers;

namespace DXVcs2Git.Tests {
    public class BaseFixture : IPostTestDirectoryRemover, IDisposable {
        private readonly List<string> directories = new List<string>();
        public static string BareTestRepoPath { get; private set; }
        public static string StandardTestRepoWorkingDirPath { get; private set; }
        public static string StandardTestRepoPath { get; private set; }
        public static string ShallowTestRepoPath { get; private set; }
        public static string MergedTestRepoWorkingDirPath { get; private set; }
        public static string MergeTestRepoWorkingDirPath { get; private set; }
        public static string MergeRenamesTestRepoWorkingDirPath { get; private set; }
        public static string RevertTestRepoWorkingDirPath { get; private set; }
        public static string SubmoduleTestRepoWorkingDirPath { get; private set; }
        private static string SubmoduleTargetTestRepoWorkingDirPath { get; set; }
        private static string AssumeUnchangedRepoWorkingDirPath { get; set; }
        public static string SubmoduleSmallTestRepoWorkingDirPath { get; set; }

        public static DirectoryInfo ResourcesDirectory { get; private set; }

        public static bool IsFileSystemCaseSensitive { get; private set; }
        static BaseFixture() {
            // Do the set up in the static ctor so it only happens once
            SetUpTestEnvironment();
        }
        private static bool IsFileSystemCaseSensitiveInternal() {
            var mixedPath = Path.Combine(Constants.TemporaryReposPath, "mIxEdCase-" + Path.GetRandomFileName());

            if (Directory.Exists(mixedPath)) {
                Directory.Delete(mixedPath);
            }

            Directory.CreateDirectory(mixedPath);
            bool isInsensitive = Directory.Exists(mixedPath.ToLowerInvariant());

            Directory.Delete(mixedPath);

            return !isInsensitive;
        }
        private static void SetUpTestEnvironment() {
            IsFileSystemCaseSensitive = IsFileSystemCaseSensitiveInternal();

            string initialAssemblyParentFolder = Directory.GetParent(new Uri(typeof(BaseFixture).Assembly.EscapedCodeBase).LocalPath).FullName;

            const string sourceRelativePath = @"../../Resources";
            ResourcesDirectory = new DirectoryInfo(Path.Combine(initialAssemblyParentFolder, sourceRelativePath));

            // Setup standard paths to our test repositories
            BareTestRepoPath = Path.Combine(sourceRelativePath, "testrepo.git");
            StandardTestRepoWorkingDirPath = Path.Combine(sourceRelativePath, "testrepo_wd");
            StandardTestRepoPath = Path.Combine(StandardTestRepoWorkingDirPath, "dot_git");
            ShallowTestRepoPath = Path.Combine(sourceRelativePath, "shallow.git");
            MergedTestRepoWorkingDirPath = Path.Combine(sourceRelativePath, "mergedrepo_wd");
            MergeRenamesTestRepoWorkingDirPath = Path.Combine(sourceRelativePath, "mergerenames_wd");
            MergeTestRepoWorkingDirPath = Path.Combine(sourceRelativePath, "merge_testrepo_wd");
            RevertTestRepoWorkingDirPath = Path.Combine(sourceRelativePath, "revert_testrepo_wd");
            SubmoduleTestRepoWorkingDirPath = Path.Combine(sourceRelativePath, "submodule_wd");
            SubmoduleTargetTestRepoWorkingDirPath = Path.Combine(sourceRelativePath, "submodule_target_wd");
            AssumeUnchangedRepoWorkingDirPath = Path.Combine(sourceRelativePath, "assume_unchanged_wd");
            SubmoduleSmallTestRepoWorkingDirPath = Path.Combine(sourceRelativePath, "submodule_small_wd");

            CleanupTestReposOlderThan(TimeSpan.FromMinutes(15));
        }
        private static void CleanupTestReposOlderThan(TimeSpan olderThan) {
            var oldTestRepos = new DirectoryInfo(Constants.TemporaryReposPath)
                .EnumerateDirectories()
                .Where(di => di.CreationTimeUtc < DateTimeOffset.Now.Subtract(olderThan))
                .Select(di => di.FullName);

            foreach (var dir in oldTestRepos) {
                DirectoryHelper.DeleteDirectory(dir);
            }
        }


        protected string Sandbox(string sourceDirectoryPath, params string[] additionalSourcePaths) {
            var scd = BuildSelfCleaningDirectory();
            var source = new DirectoryInfo(sourceDirectoryPath);

            var clonePath = Path.Combine(scd.DirectoryPath, source.Name);
            DirectoryHelper.CopyFilesRecursively(source, new DirectoryInfo(clonePath));

            foreach (var additionalPath in additionalSourcePaths) {
                var additional = new DirectoryInfo(additionalPath);
                var targetForAdditional = Path.Combine(scd.DirectoryPath, additional.Name);

                DirectoryHelper.CopyFilesRecursively(additional, new DirectoryInfo(targetForAdditional));
            }

            return clonePath;
        }
        protected SelfCleaningDirectory BuildSelfCleaningDirectory() {
            return new SelfCleaningDirectory(this);
        }
        public void Register(string directoryPath) {
            directories.Add(directoryPath);
        }
        public virtual void Dispose() {
            foreach (string directory in directories) {
                DirectoryHelper.DeleteDirectory(directory);
            }
        }
    }
}
