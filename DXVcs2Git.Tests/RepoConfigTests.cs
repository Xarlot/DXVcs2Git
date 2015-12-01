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
        public void GenerateRepo152Config() {
            GenerateRepoConfig("2015.2", "TestBuild.v15.2", "XPF DXVcs2Git sync task v15.2");
        }
        void GenerateRepoConfig(string branch, string taskName, string watchTaskName) {
            GitRepoConfig config = new GitRepoConfig() {
                Name = branch,
                FarmTaskName = watchTaskName,
                FarmSyncTaskName = taskName,
            };
            SharpSerializerXmlSettings settings = new SharpSerializerXmlSettings();
            settings.IncludeAssemblyVersionInTypeName = false;
            settings.IncludePublicKeyTokenInTypeName = false;
            SharpSerializer serializer = new SharpSerializer(settings);
            serializer.Serialize(config, @"z:\gitconfig.config");
        }
    }
}
