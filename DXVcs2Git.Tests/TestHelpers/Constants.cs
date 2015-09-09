using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security;
using System.Text;
using System.Threading.Tasks;
using LibGit2Sharp;

namespace DXVcs2Git.Tests.TestHelpers {
    public static class Constants {
        public static readonly string TemporaryReposPath = BuildPath();
        public const string UnknownSha = "deadbeefdeadbeefdeadbeefdeadbeefdeadbeef";
        public static readonly Identity Identity = new Identity("DXVcs2GitTester", "maximatorr3@gmail.com");
        public static readonly Identity Identity2 = new Identity("nulltoken", "emeric.fermas@gmail.com");

        public static readonly Signature Signature = new Signature(Identity, new DateTimeOffset(2011, 06, 16, 10, 58, 27, TimeSpan.FromHours(2)));
        public static readonly Signature Signature2 = new Signature(Identity2, DateTimeOffset.Parse("Wed, Dec 14 2011 08:29:03 +0100"));
        public const string PrivateRepoUrl = "";
        public static Credentials PrivateRepoCredentials(string url, string usernameFromUrl,
                                                         SupportedCredentialTypes types) {
            return null;
        }

        public static string BuildPath() {
            string tempPath = null;

            const string DXVcs2GitTestPath = "DXVcs2GitTestPath";
            const string DXVcs2GitTestProject = "testProject";

            // We're running on .Net/Windows
            if (Environment.GetEnvironmentVariables().Contains(DXVcs2GitTestPath)) {
                Trace.TraceInformation("{0} environment variable detected", DXVcs2GitTestPath);
                tempPath = Environment.GetEnvironmentVariables()[DXVcs2GitTestPath] as String;
            }

            if (String.IsNullOrWhiteSpace(tempPath) || !Directory.Exists(tempPath)) {
                Trace.TraceInformation("Using default test path value");
                tempPath = Path.GetTempPath();
            }

            string testWorkingDirectory = Path.Combine(tempPath, DXVcs2GitTestProject);
            Trace.TraceInformation("Test working directory set to '{0}'", testWorkingDirectory);
            return testWorkingDirectory;
        }
        // To help with creating secure strings to test with.
        internal static SecureString StringToSecureString(string str) {
            var chars = str.ToCharArray();

            var secure = new SecureString();
            for (var i = 0; i < chars.Length; i++) {
                secure.AppendChar(chars[i]);
            }

            secure.MakeReadOnly();

            return secure;
        }
    }
}
