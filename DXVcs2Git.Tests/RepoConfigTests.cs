using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DXVcs2Git.Core;
using DXVcs2Git.Core.Git;
using NUnit.Framework;
using Polenter.Serialization;

namespace DXVcs2Git.Tests {
    [TestFixture]
    public class RepoConfigTests {
        [Test, Explicit]
        public void GenerateCommon152Config() {
            GenerateRepoConfig("2015.2", "TestBuild.v15.2", "XPF Common sync task v15.2", "dxvcs2git.xpf");
        }
        [Test, Explicit]
        public void GenerateCommon151Config() {
            GenerateRepoConfig("2015.1", "TestBuild.v15.1", "XPF Common sync task v15.1", "dxvcs2git.xpf");
        }
        [Test, Explicit]
        public void GenerateDiagram152Config() {
            GenerateRepoConfig("2015.2", "TestBuild.v15.2", "XPF Diagram sync task v15.2", "dxvcs2git.xpf");
        }

        void GenerateRepoConfig(string branch, string taskName, string watchTaskName, string defaultService) {
            GitRepoConfig config = new GitRepoConfig() {
                Name = branch,
                FarmTaskName = watchTaskName,
                FarmSyncTaskName = taskName,
                DefaultServiceName = defaultService,
            };
            SharpSerializerXmlSettings settings = new SharpSerializerXmlSettings();
            settings.IncludeAssemblyVersionInTypeName = false;
            settings.IncludePublicKeyTokenInTypeName = false;
            SharpSerializer serializer = new SharpSerializer(settings);
            serializer.Serialize(config, @"z:\gitconfig.config");
        }
    }
}
