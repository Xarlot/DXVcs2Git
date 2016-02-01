using System.IO;
using System.Net;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace DXVcs2Git.Core.GitLab {
    public static class HttpRequestParser {
        public static string Extract(HttpListenerRequest message) {
            using (var reader = new StreamReader(message.InputStream)) {
                string jsonString = reader.ReadToEnd();
                return jsonString;
            }
        }
        public static T Parse<T>(string json) where T : IParseApiSupported {
            var result = JsonConvert.DeserializeObject<T>(json, new IsoDateTimeConverter());
            result.Json = json;
            return result;
        }
    }
}
