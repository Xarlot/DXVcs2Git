using DXVcs2Git.Core;
using DXVcs2Git.Core.Configuration;
using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.ServiceModel.Syndication;
using System.Windows.Threading;
using System.Xml;

namespace DXVcs2Git.UI.AtomFeed {
    public static class FeedWorker {
        public const string VSIXId = "DXVcs2Git.GitTools.Xarlot.e4313842-75d7-4bf3-9516-fccdb00bec7d";
        static DispatcherTimer timer;
        static Uri galleryUri;
        static WebClient downloader;
        public static Uri NewVersionUri { get; private set; }
        public static Version NewVersion { get; private set; }
        public static bool HasNewVersion { get { return NewVersionUri != null; } }
        static Dispatcher Dispatcher { get; set; }
        static int updateDelay;

        public static int UpdateDelay {
            get { return updateDelay; }
            set {
                if (updateDelay == value)
                    return;
                updateDelay = value;
                OnUpdateDelayChanged();
            }
        }

        static void OnUpdateDelayChanged() {
            if (timer == null)
                return;
            timer.Stop();
            timer.Interval = TimeSpan.FromSeconds(UpdateDelay);
            timer.Start();
        }

        public static void Initialize() {
            UpdateDelay = ConfigSerializer.GetConfig().UpdateDelay;
            if (UpdateDelay == 0)
                UpdateDelay = 30;
            galleryUri = new Uri("http://idetester-sv.corp.devexpress.com/atomfeed.html");
            downloader = new WebClient();
            downloader.OpenReadCompleted += OnOpenReadCompleted;
            Dispatcher = Dispatcher.CurrentDispatcher;
            timer = new DispatcherTimer(DispatcherPriority.ApplicationIdle, Dispatcher);
            timer.Interval = TimeSpan.FromSeconds(UpdateDelay);
            timer.Tick += (o, e) => Update();
            timer.Start();
            Update();
        }

        public static void Update() {
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
            if (vsixExtension.Version > VersionInfo.Version) {
                OnNewVersion((item.Content as UrlSyndicationContent).Url, vsixExtension.Version);
            } else if(vsixExtension.Version == VersionInfo.Version) {
                var config = ConfigSerializer.GetConfig();
                config.InstallPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                ConfigSerializer.SaveConfig(config);
            }
        }

        static void OnNewVersion(Uri url, Version version) {
            NewVersionUri = new Uri(new Uri("http://idetester-sv.corp.devexpress.com/"), url);
            NewVersion = version;
        }        
    }
}
