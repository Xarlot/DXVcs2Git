using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using DXVcs2Git.Core.GitLab;
using NGitLab.Models;
using MergeRequestState = DXVcs2Git.Core.GitLab.MergeRequestState;

namespace DXVcs2Git.Console {
    public static class MSTeamsMessageFormatter {
        public static string FormatMergeRequestText(MergeRequestHookClient hook, MergeRequest mergeRequest) {
            switch (hook.Attributes.State) {
                case MergeRequestState.opened:
                    return FormatOpenedMergeRequest(hook, mergeRequest);
                case MergeRequestState.reopened:
                    return string.Empty;
                case MergeRequestState.merged:
                    return string.Empty;
                case MergeRequestState.closed:
                    return string.Empty;
                default:
                    throw new NotImplementedException("MergeRequestState");
            }
        }
        static string FormatOpenedMergeRequest(MergeRequestHookClient hook, MergeRequest mergeRequest) {
            return $"{hook.User.UserName} opened {GetMergeRequestLink(hook, mergeRequest)} in {GetRepositoryLink(hook)}: {GetMergeRequestName(mergeRequest)}";
        }
        static string GetMergeRequestName(MergeRequest mergeRequest) {
            return mergeRequest.Title;
        }
        static string GetMergeRequestLink(MergeRequestHookClient hook, MergeRequest mergeRequest) {
            return $"merge request !{mergeRequest.Id}";
        }
        static string GetRepositoryLink(MergeRequestHookClient hook) {
            return string.Empty;
        }
    }

    public static class MarkdownHelper {
        public static string ToLink(this string text) {
            return text;
        }

    }
}
