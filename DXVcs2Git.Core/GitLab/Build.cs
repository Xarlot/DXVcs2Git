using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NGitLab.Models;

namespace DXVcs2Git.Core.GitLab {
    public class Build {
        public JobStatus? Status { get; set; }
        public DateTime? StartedAt { get; set; }
        public DateTime? FinishedAt { get; set; }
        public ArtifactsFile ArtifactsFile { get; set; }
        public int Id { get; set; }
    }

    public class ArtifactsFile {
        public string FileName { get; set; }
    }
}
