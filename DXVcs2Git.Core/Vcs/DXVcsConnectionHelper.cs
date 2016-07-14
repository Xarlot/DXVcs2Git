using System;

namespace DXVcs2Git.DXVcs {
    public static class DXVcsConnectionHelper {
        public static IDXVcsRepository Connect(string vcsService, string user = null, string password = null) {
            if (string.IsNullOrEmpty(vcsService))
                throw new ArgumentException("vcsService");

            return DXVcsRepositoryFactory.Create(vcsService, user, password);
        }
    }
}
