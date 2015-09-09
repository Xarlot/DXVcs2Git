using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DXVcs2Git.DXVcs;

namespace DXVcs2Git.DXVcs {
    public static class DXVcsConectionHelper {
        public static IDXVcsRepository Connect(string vcsService) {
            if (string.IsNullOrEmpty(vcsService))
                throw new ArgumentException("vcsService");

            return DXVcsRepositoryFactory.Create(vcsService);
        }
    }
}
