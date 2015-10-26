using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DXVcs2Git.Core;

namespace DXVcs2Git.Git {
    public class GitCommentsGenerator : CommentsGenerator {
        public static readonly GitCommentsGenerator Instance = new GitCommentsGenerator();
    }
}
