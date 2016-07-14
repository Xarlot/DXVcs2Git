using DXVcs2Git.Core.Git;
using NUnit.Framework;
using Polenter.Serialization;

namespace DXVcs2Git.Tests {
    [TestFixture]
    public class RepoConfigTests {
        [Test, Explicit]
        public void GenerateCommon152Config() {
            GenerateRepoConfig("2015.2", "TestBuild.v15.2", "XPF Common sync task v15.2", "dxvcs2git.xpf", true, new []{ "XPF_2015.2_All", "XPF_2015.2_Core"});
        }
        [Test, Explicit]
        public void GenerateCommon151Config() {
            GenerateRepoConfig("2015.1", "TestBuild.v15.1", "XPF Common sync task v15.1", "dxvcs2git.xpf");
        }
        [Test, Explicit]
        public void GenerateDiagram152Config() {
            GenerateRepoConfig("2015.2", "TestBuild.v15.2", "XPF Diagram sync task v15.2", "dxvcs2git.xpf");
        }

        void GenerateRepoConfig(string branch, string taskName, string watchTaskName, string defaultService, bool allowTesting = false,  string[] testConfigs = null) {
            RepoConfig config = new RepoConfig() {
                Name = branch,
                FarmTaskName = watchTaskName,
                FarmSyncTaskName = taskName,
                DefaultServiceName = defaultService,
                SupportsTesting = allowTesting,
                TestConfigs = testConfigs,
            };
            SharpSerializerXmlSettings settings = new SharpSerializerXmlSettings();
            settings.IncludeAssemblyVersionInTypeName = false;
            settings.IncludePublicKeyTokenInTypeName = false;
            SharpSerializer serializer = new SharpSerializer(settings);
            serializer.Serialize(config, @"z:\gitconfig.config");
        }
    }
}
