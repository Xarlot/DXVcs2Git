using System;
using System.Net;
using System.Text.RegularExpressions;

namespace DXVcs2Git.Core {
    public static class ReplaceIPInUrlHelper {
        public static Uri Replace(Uri url, IPAddress ip) {
            var newUriBuilder = new UriBuilder(url);
            newUriBuilder.Host = ip.ToString();
            var newUri = newUriBuilder.Uri;
            return newUri;
        }
        public static bool IsSameHost(Uri url, IPAddress ip) {
            var newUriBuilder = new UriBuilder(url);
            return newUriBuilder.Host == ip.ToString();
        }
    }
}
