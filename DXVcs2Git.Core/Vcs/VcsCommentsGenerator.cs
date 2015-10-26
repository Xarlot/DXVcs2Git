using System;
using DXVcs2Git.Core;

namespace DXVcs2Git.DXVcs {
    public class VcsCommentsGenerator : CommentsGenerator {
        public static readonly VcsCommentsGenerator Instance = new VcsCommentsGenerator();
    }
}
