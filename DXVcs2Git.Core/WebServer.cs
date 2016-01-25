using System;
using System.Collections.Concurrent;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace DXVcs2Git.Core {
    public class WebServer : IDisposable {
        readonly ConcurrentQueue<HttpListenerRequest> requests = new ConcurrentQueue<HttpListenerRequest>();
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

            CancellationTokenSource source = new CancellationTokenSource();
            CancellationToken token = source.Token;

            TaskFactory taskFactory = new TaskFactory(token, TaskCreationOptions.LongRunning, TaskContinuationOptions.LongRunning, TaskScheduler.Current);
            this.listenerTask = taskFactory.StartNew(() => {
                while (token.IsCancellationRequested) {
                    var context = this.listener.GetContextAsync();
                    var request = context.Result.Request;
                    this.requests.Enqueue(request);
                }

            }, token);
            this.listenerTask.Wait(token);
        }
        public HttpListenerRequest GetRequest() {
            HttpListenerRequest request;
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
