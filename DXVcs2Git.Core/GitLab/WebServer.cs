using System;
using System.Collections.Concurrent;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using DXVcs2Git.Core.GitLab;

namespace DXVcs2Git.Core {
    public class WebServer : IDisposable {
        readonly ConcurrentQueue<WebHookRequest> requests = new ConcurrentQueue<WebHookRequest>();
        readonly string url;
        readonly HttpListener listener = new HttpListener();
        Task listenerTask;
        public WebServer(string url) {
            this.url = url;
            this.listener.Prefixes.Add(this.url);
        }
        public void Start() {
            if (this.listener.IsListening)
                throw new ArgumentException("already listening");
            this.listener.Start();
            Log.Message($"Web server started at {url}");

            CancellationTokenSource source = new CancellationTokenSource();
            CancellationToken token = source.Token;

            TaskFactory taskFactory = new TaskFactory(token, TaskCreationOptions.LongRunning, TaskContinuationOptions.LongRunning, TaskScheduler.Default);
            this.listenerTask = taskFactory.StartNew(() => {
                while (!token.IsCancellationRequested) {
                    var context = this.listener.GetContextAsync();
                    var httpRequest = context.Result.Request;
                    var request = new WebHookRequest() {Request = HttpRequestParser.Extract(httpRequest)};
                    this.requests.Enqueue(request);
                    var response = context.Result.Response;
                    response.StatusCode = 200;

                    var message = System.Text.Encoding.UTF8.GetBytes("OK");
                    response.ContentLength64 = message.Length;
                    response.ContentType = "text";
                    var outputstream = response.OutputStream;
                    outputstream.Write(message, 0, message.Length);
                    outputstream.Close();
                }
            }, token);
        }
        public WebHookRequest GetRequest() {
            WebHookRequest request;
            if (this.requests.TryDequeue(out request))
                return request;
            return null;
        }
        public void Stop() {
            this.listener.Stop();
        }

        public void Dispose() {
            Stop();
        }
    }

    public class WebHookRequest {
        public string Request { get; set; }
    }
}
