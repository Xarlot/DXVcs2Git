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
        static readonly Credentials testCredentials = new UsernamePasswordCredentials() {Password = "q1w2e3r4t5y6", Username = Constants.Identity.Name};
        [Test]
        public void InitNewRepo() {
            string repoPath = InitNewRepository();
            string configPath = CreateConfigurationWithDummyUser(Constants.Identity);
            var options = new RepositoryOptions {GlobalConfigurationLocation = configPath};

            using (var repo = new Repository(repoPath, options)) {
                string dir = repo.Info.Path;
                Assert.True(Path.IsPathRooted(dir));
                Assert.True(Directory.Exists(dir));
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
        [Test]
        public void CloneRepo() {
            var scd = BuildSelfCleaningDirectory();
            CloneOptions options = new CloneOptions();
            var credentials = new UsernamePasswordCredentials();
            credentials.Username = Constants.Identity.Name;
            credentials.Password = "q1w2e3r4t5y6";
            options.CredentialsProvider += (url, fromUrl, types) => credentials;

            string clonedRepoPath = Repository.Clone(testUrl, scd.DirectoryPath, options);
            string file = Path.Combine(scd.DirectoryPath, "testpush.txt");
            var rbg = new RandomBufferGenerator(30000);
            using (var repo = new Repository(clonedRepoPath)) {
                for (int i = 0; i < 1; i++) {
                    var network = repo.Network.Remotes.First();
                    FetchOptions fetchOptions = new FetchOptions();
                    fetchOptions.CredentialsProvider += (url, fromUrl, types) => credentials;
                    repo.Fetch(network.Name, fetchOptions);

                    File.WriteAllBytes(file, rbg.GenerateBufferFromSeed(30000));
                    repo.Stage(file);
                    Signature author = Constants.Signature;

                    Commit commit = repo.Commit($"Here's a commit {i + 1} i made!", author, author);
                    PushOptions pushOptions = new PushOptions();
                    pushOptions.CredentialsProvider += (url, fromUrl, types) => credentials;
                    repo.Network.Push(repo.Branches["master"], pushOptions);
                }
            }
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
            var wrapper = new GitWrapper(path, testUrl, testCredentials);
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

