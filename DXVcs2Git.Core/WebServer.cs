using System;
using System.Net;
using System.Threading.Tasks;

namespace DXVcs2Git.Core {
    public class WebServer : IDisposable {
        readonly string url;
        readonly HttpListener listener = new HttpListener();
        public WebServer(string url) {
            this.url = url;
        }

        public async Task<HttpListenerContext> Start() {
            if (this.listener.IsListening)
                throw new ArgumentException("already listening");
            this.listener.Prefixes.Add(this.url);
            this.listener.Start();
            return await this.listener.GetContextAsync();
        }
        public async void Stop() {
            this.listener.Stop();
            this.listener.Prefixes.Clear();
        }

        public void Dispose() {
        }
    }
}
