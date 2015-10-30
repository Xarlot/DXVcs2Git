using System;
using System.Collections.Generic;
using System.Linq;
using Polenter.Serialization;

namespace DXVcs2Git.Core.Serialization {
    public class SyncHistory {
        public List<SyncHistoryItem> Items { get; set; }

        public SyncHistory() {
            Items = new List<SyncHistoryItem>();
        }
        public static void Serialize(SyncHistory history, string path) {
            SharpSerializerXmlSettings settings = new SharpSerializerXmlSettings();
            settings.IncludeAssemblyVersionInTypeName = false;
            settings.IncludePublicKeyTokenInTypeName = false;
            SharpSerializer serializer = new SharpSerializer(settings);
            serializer.Serialize(history, path);
        }
        public static SyncHistory Deserialize(string path) {
            try {
                SharpSerializer serializer = new SharpSerializer();
                return (SyncHistory)serializer.Deserialize(path);
            }
            catch (Exception ex) {
                Log.Error($"Loading history from {path} failed", ex);
                return new SyncHistory();
            }
        }
        public SyncHistory Clone(int limit) {
            var items = Items.Cast<SyncHistoryItem>().Reverse().Take(limit).Reverse().ToList();
            return new SyncHistory() {Items = items};
        }
    }
}
