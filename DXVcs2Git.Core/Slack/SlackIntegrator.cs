using System;
using System.Windows.Threading;
using SlackConnector.Models;

namespace DXVcs2Git.Core.Slack {
    public static class SlackIntegrator {
        static readonly SlackBotWrapper Instance;
        static Dispatcher Dispatcher { get; set; }
        static Action<string> InvalidateCallback { get; set; }
        static bool started;
        static SlackIntegrator() {
            Instance = new SlackBotWrapper();
        }
        public static void Start(string token, Dispatcher dispatcher, Action<string> invalidateCallback) {
            Dispatcher = dispatcher;
            InvalidateCallback = invalidateCallback ?? (x => { });
            Instance.MessageReceived += InstanceOnMessageReceived;
            Instance.Start(token);
            started = true;
        }
        static void InstanceOnMessageReceived(object sender, SlackMessage slackMessage) {
            InvalidateCallback?.Invoke(slackMessage.Text);
        }
        public static void SendMessage(string text) {
            if (!started)
                return;
            Instance.SendMessage(text);
        }
    }
}
