using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.ServiceModel.Syndication;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace DXVcs2Git.UI.AtomFeed {
    public static class FeedWorker{
        public const string VSIXId = "DXVcs2Git.GitTools.Xarlot.e4313842-75d7-4bf3-9516-fccdb00bec7d";
        public static Uri NewVersionUri { get; private set; }
        public static Version NewVersion { get; private set; }
        public static bool HasNewVersion { get { return NewVersionUri != null; } }
        public static void Initialize() {
            Uri galleryUri = new Uri("http://idetester-sv.corp.devexpress.com/atomfeed.html");
            var downloader = new WebClient();
            downloader.OpenReadCompleted += OnOpenReadCompleted;
            downloader.OpenReadAsync(galleryUri);
        }

        static void OnOpenReadCompleted(object sender, OpenReadCompletedEventArgs e) {
            if (e.Error != null)
                return;
            using (var xmlReader = XmlReader.Create(e.Result)) {
                SyndicationFeed feed = SyndicationFeed.Load(xmlReader);
                foreach(SyndicationItem item in feed.Items) {
                    if (item.Id == VSIXId)
                        ProcessItem(item);
                }
            }
        }

        static void ProcessItem(SyndicationItem item) {
            var vsixExtension = item.ElementExtensions.ReadElementExtensions<Vsix>(Vsix.ExtensionName, Vsix.ExtensionNamespace).First();
            if (vsixExtension.Version > DXVcs2Git.Core.VersionInfo.Version) {
                OnNewVersion((item.Content as UrlSyndicationContent).Url, vsixExtension.Version);
            }
        }

        static void OnNewVersion(Uri url, Version version) {
            NewVersionUri = new Uri(new Uri("http://idetester-sv.corp.devexpress.com/"), url);
            NewVersion = version;
        }        
    }
}
