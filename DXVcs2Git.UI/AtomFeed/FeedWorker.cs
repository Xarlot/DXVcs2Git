using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.ServiceModel.Syndication;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace DXVcs2Git.UI.AtomFeed {
    public class FeedWorker : IDisposable {
        public FeedWorker() {

        }
        public void Initialize() {
            Uri galleryUri = new Uri("http://idetester-sv.corp.devexpress.com/atomfeed.html");
            var downloader = new WebClient();
            downloader.OpenReadCompleted += OnOpenReadCompleted;
            downloader.OpenReadAsync(galleryUri);
        }

        void OnOpenReadCompleted(object sender, OpenReadCompletedEventArgs e) {
            if (e.Error != null)
                return;
            using (var xmlReader = XmlReader.Create(e.Result)) {
                SyndicationFeed feed = SyndicationFeed.Load(xmlReader);
                foreach(SyndicationItem item in feed.Items) {
                    if (item.Id == "DXVcs2Git.GitTools.Xarlot.e4313842-75d7-4bf3-9516-fccdb00bec7d")
                        ProcessItem(item);
                }
            }
        }

        void ProcessItem(SyndicationItem item) {
            var vsixExtension = item.ElementExtensions.ReadElementExtensions<Vsix>(Vsix.ExtensionName, Vsix.ExtensionNamespace).First();
        }

        public void Dispose() {
        }
    }
}
