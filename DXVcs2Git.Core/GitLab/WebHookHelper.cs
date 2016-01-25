using System;
using System.Net;

namespace DXVcs2Git.Core {
    public static class WebHookHelper {
        static readonly string SharedWebHook = "/sharedwebhook/";
        static readonly string WebhookFormat = @"http://{0}:8080" + SharedWebHook;

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
        public static bool IsSharedHook(Uri url) {
            var newUriBuilder = new UriBuilder(url);
            if (newUriBuilder.Path == SharedWebHook)
                return true;
            return false;
        }
        public static string GetSharedHookUrl(IPAddress ip) {
            return string.Format(WebhookFormat, ip);
        }
    }
}
