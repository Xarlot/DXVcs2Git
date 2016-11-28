using System.Net;
using System.Text;

namespace DXVcs2Git.Console {
    public static class MSTeamMessageSender {
        const string url = @"https://outlook.office365.com/webhook/75d1d336-927c-4098-9bcd-e808b369cee5@e4d60396-9352-4ae8-b84c-e69244584fa4/IncomingWebhook/865557ff0a3c45bba0798398f15e7c4a/a41bd164-150b-48db-96e1-02d59e253b2b";
        public static void SendMessage(string message) {
            using (WebClient webClient = new WebClient()) {
                webClient.Headers[HttpRequestHeader.ContentType] = "application/json";
                webClient.UploadData(url, Encoding.UTF8.GetBytes(message));
            }
        }
    }
}
