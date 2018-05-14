using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using DXVcs2Git.Tests.TestHelpers;
using LibGit2Sharp;
using NUnit.Framework;

namespace DXVcs2Git.Tests {
    [TestFixture]
    public class GitServiceTests : BaseFixture {
        const string testUrl = @"http://litvinov-lnx/tester/testproject.git";
        static readonly GitCredentials testCredentials = new GitCredentials() {Password = "q1w2e3r4t5y6", User = Constants.Identity.Name};
        [Test]
        public void InitNewRepo() {
            string repoPath = InitNewRepository();
            string configPath = CreateConfigurationWithDummyUser(Constants.Identity);
            var options = new RepositoryOptions {};

            using (var repo = new Repository(repoPath, options)) {
                string dir = repo.Info.Path;
                Assert.IsTrue(Path.IsPathRooted(dir));
                Assert.IsTrue(Directory.Exists(dir));
            }
        }
        string CreateConfigurationWithDummyUser(Identity identity) {
            return CreateConfigurationWithDummyUser(identity.Name, identity.Email);
        }
        [Test]
        public void InitNewRepoTwice() {
            SelfCleaningDirectory scd = BuildSelfCleaningDirectory();

            string repoPath = InitNewRepository(scd.DirectoryPath);
            string repoPath2 = InitNewRepository(scd.DirectoryPath);
            Assert.AreEqual(repoPath, repoPath2);
        }
        protected string CreateConfigurationWithDummyUser(string name, string email) {
            SelfCleaningDirectory scd = BuildSelfCleaningDirectory();

            string configFilePath = Touch(scd.DirectoryPath, "fake-config");

            using (Configuration config = Configuration.BuildFrom(configFilePath)) {
                if (name != null) {
                    config.Set("user.name", name);
                }

                if (email != null) {
                    config.Set("user.email", email);
                }
            }

            return configFilePath;
        }
        protected static string Touch(string parent, string file, string content = null, Encoding encoding = null) {
            string filePath = Path.Combine(parent, file);
            string dir = Path.GetDirectoryName(filePath);
            Debug.Assert(dir != null);

            Directory.CreateDirectory(dir);

            File.WriteAllText(filePath, content ?? string.Empty, encoding ?? Encoding.ASCII);

            return filePath;
        }
        protected string InitNewRepository() {
            SelfCleaningDirectory scd = BuildSelfCleaningDirectory();
            return InitNewRepository(scd.DirectoryPath);
        }
        protected string InitNewRepository(string path) {
            var wrapper = new GitWrapper(path, testUrl, "master", testCredentials);
            return wrapper.GitDirectory;
        }
    }

    public class RandomBufferGenerator {
        readonly Random _random = new Random();
        readonly byte[] _seedBuffer;

        public RandomBufferGenerator(int maxBufferSize) {
            _seedBuffer = new byte[maxBufferSize];

            _random.NextBytes(_seedBuffer);
        }

        public byte[] GenerateBufferFromSeed(int size) {
            int randomWindow = _random.Next(0, size);

            byte[] buffer = new byte[size];

            Buffer.BlockCopy(_seedBuffer, randomWindow, buffer, 0, size - randomWindow);
            Buffer.BlockCopy(_seedBuffer, 0, buffer, size - randomWindow, randomWindow);

            return buffer;
        }
    }
}

