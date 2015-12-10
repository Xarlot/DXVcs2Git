namespace DXVcs2Git.Core.Git {
    public class GitRepoConfig {
        public const string ConfigFileName = "gitconfig.config";
        public string Name { get; set; }
        public string FarmTaskName { get; set; }
        public string FarmSyncTaskName { get; set; }
        public string DefaultServiceName { get; set; }
    }
}
