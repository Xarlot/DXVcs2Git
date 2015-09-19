using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using NGitLab;
using NGitLab.Models;

namespace DXVcs2Git.Git {
    public class GitLabWrapper {
        readonly GitLabClient client;
        public GitLabWrapper(string server, string token) {
            client = GitLabClient.Connect(server, token);
        }
        public Project FindProject(string project) {
            return client.Projects.Accessible.FirstOrDefault(x => x.PathWithNamespace == project);
        }
        public IEnumerable<MergeRequest> GetMergeRequests(Project project) {
            var mergeRequestsClient = client.GetMergeRequest(project.Id);
            return mergeRequestsClient.All;
        }
    }
}
