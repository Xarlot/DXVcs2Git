using System;
using System.Net;
using NGitLab.Models;

namespace DXVcs2Git.Core {
    public static class WebHookHelper {
        static readonly string SharedWebHook = "/{0}/";
        static readonly string WebhookFormat = @"http://{0}:8080";

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
        public static bool IsSharedHook(string sharedwebhookPath, Uri url) {
            var newUriBuilder = new UriBuilder(url);
            if (newUriBuilder.Path == string.Format(SharedWebHook, sharedwebhookPath))
                return true;
            return false;
        }
        public static string GetSharedHookUrl(IPAddress ip, string sharedWebHookPath) {
            return string.Format(WebhookFormat, ip) + string.Format(SharedWebHook, sharedWebHookPath);
        }
        public static bool EnsureWebHook(ProjectHook webHook) {
            return webHook.JobEvents && webHook.PipelineEvents && webHook.PushEvents && webHook.MergeRequestsEvents && !webHook.EnableSslVerification;
        }
    }
}
