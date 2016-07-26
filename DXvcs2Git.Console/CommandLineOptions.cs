using CommandLine;

namespace DXVcs2Git.Console {
    public enum WorkMode {
        listener,
        synchronizer,
        patch,
    }

    public abstract class GeneralOptions {
        [Option('s', "server", Required = true, HelpText = "GitLab server url")]
        public string Server { get; }
        [Option('l', "login", Required = true, HelpText = "Login to git master account")]
        public string Login { get; }
        [Option('r', "repo", Required = true, HelpText = "Http git repo path")]
        public string Repo { get; }
        [Option('p', "password", Required = true, HelpText = "Password")]
        public string Password { get; }
        [Option('a', "auth", Required = true, HelpText = "GitLab auth token")]
        public string AuthToken { get; }

        protected GeneralOptions(string server, string login, string repo, string password, string authToken) {
            this.Server = server;
            Login = login;
            Repo = repo;
            Password = password;
            AuthToken = authToken;
        }
    }
    [Verb("applypatch", HelpText = "Apply specified patch to sources")]
    public class ApplyPatchOptions : GeneralOptions {
        [Option('p', "patch", Required = true, HelpText = "Path to patch.zip")]
        public string Patch { get; }
        [Option('b', "branch", Required = false, HelpText = "Local git branch name")]
        public string Branch { get; }
        [Option('t', "tracker", Required = false, HelpText = "Path to config with items to track")]
        public string Tracker { get; }
        [Option("sourcerepo", Required = false, HelpText = "Source repo for searching merge request")]
        public string SourceRepo { get; }
        [Option("sourcebranch", Required = false, HelpText = "Source branch for searching merge request")]
        public string SourceBranch { get; }
        [Option('d', "dir", HelpText = "Path to local git repo")]
        public string LocalFolder { get; }
        public ApplyPatchOptions(string patch, string branch, string tracker, string sourceRepo, string sourceBranch, string localFolder, string server, string login, string repo, string password, string authToken) : base(server, login, repo, password, authToken) {
            Patch = patch;
            Branch = branch;
            Tracker = tracker;
            SourceBranch = sourceBranch;
            SourceRepo = sourceRepo;
            LocalFolder = localFolder;
        }
    }
    [Verb("sync", HelpText = "Sync changes between dxvcs and git")]
    public class SyncOptions : GeneralOptions {
        [Option('b', "branch", Required = false, HelpText = "Local git branch name")]
        public string Branch { get; }
        [Option('t', "tracker", Required = false, HelpText = "Path to config with items to track")]
        public string Tracker { get; }
        [Option('c', "commits", Default = 100, HelpText = "Commits count to process")]
        public int CommitsCount { get; }
        [Option('d', "dir", HelpText = "Path to local git repo")]
        public string LocalFolder { get; }

        public SyncOptions(string branch, string tracker, int commitsCount, string localFolder, string server, string login, string repo, string password, string authToken) : base(server, login, repo, password, authToken) {
            Branch = branch;
            CommitsCount = commitsCount;
            LocalFolder = localFolder;
            Tracker = tracker;
        }
    }
    [Verb("listen", HelpText = "Web hook listener from gitlab server")]
    public class ListenerOptions : GeneralOptions {
        [Option("webhook", Default = "sharedwebhook", HelpText = "Webhook name on gitlab server")]
        public string WebHook { get; }
        [Option('i', "interval", Required = false, Default = 30, HelpText = "Duration in minutes")]
        public int Timeout { get; }


        public ListenerOptions(string webHook, int timeout, string server, string login, string repo, string password, string authToken) : base(server, login, repo, password, authToken) {
            WebHook = webHook;
            Timeout = timeout;
        }
    }
    [Verb("patch", HelpText = "Generate patch for branch")]
    public class PatchOptions : GeneralOptions {
        [Option('b', "branch", Required = false, HelpText = "Local git branch name")]
        public string Branch { get; set; }
        [Option('t', "tracker", Required = false, HelpText = "Path to config with items to track")]
        public string Tracker { get; set; }
        [Option("sourcerepo", Required = false, HelpText = "Source repo for searching merge request")]
        public string SourceRepo { get; set; }
        [Option("sourcebranch", Required = false, HelpText = "Source branch for searching merge request")]
        public string SourceBranch { get; set; }
        [Option('d', "dir", HelpText = "Path to local git repo")]
        public string LocalFolder { get; set; }
        [Option("patchdir", Required = true, HelpText = "Location of patch.info generated in patch mode")]
        public string PatchDir { get; set; }
        public PatchOptions(string branch, string repo, string login, string password, string authToken, string localFolder, string server, string tracker, string patchDir, string sourceRepo, string sourceBranch) : base(server, login, repo, password, authToken) {
            Branch = branch;
            Tracker = tracker;
            SourceBranch = sourceBranch;
            SourceRepo = sourceRepo;
            LocalFolder = localFolder;
            PatchDir = patchDir;
        }
    }

    public class CommandLineOptions {
        [Option('s', "server", Required = true, HelpText = "GitLab server url")]
        public string Server { get; set; }
        [Option('b', "branch", Required = false, HelpText = "Local git branch name")]
        public string Branch { get; set; }
        [Option('r', "repo", Required = false, HelpText = "Http git repo path")]
        public string Repo { get; set; }
        [Option('c', "commits", Default = 100, HelpText = "Commits count to process")]
        public int CommitsCount { get; set; }
        [Option('l', "login", Required = false, HelpText = "Login to git master account")]
        public string Login { get; set; }
        [Option('p', "password", Required = true, HelpText = "Password")]
        public string Password { get; set; }
        [Option('a', "auth", Required = true, HelpText = "GitLab auth token")]
        public string AuthToken { get; set; }
        [Option('d', "dir", HelpText = "Path to local git repo")]
        public string LocalFolder { get; set; }
        [Option('t', "tracker", Required = false, HelpText = "Path to config with items to track")]
        public string Tracker { get; set; }
        [Option('m', "mode", Required = false, Default = WorkMode.synchronizer, HelpText = "Work mode")]
        public WorkMode WorkMode { get; set; }
        [Option('i', "interval", Required = false, Default = 30, HelpText = "Duration in minutes")]
        public int Timeout { get; set; }
        [Option("sourcerepo", Required = false, HelpText = "Source repo for searching merge request")]
        public string SourceRepo { get; set; }
        [Option("sourcebranch", Required = false, HelpText = "Source branch for searching merge request")]
        public string SourceBranch { get; set; }
        [Option("patchdir", Required = false, HelpText = "Location of patch.info generated in patch mode")]
        public string PatchDir { get; set; }
        [Option("webhook", Default = "sharedwebhook", HelpText = "Webhook name on gitlab server")]
        public string WebHook { get; set; }
    }
}
