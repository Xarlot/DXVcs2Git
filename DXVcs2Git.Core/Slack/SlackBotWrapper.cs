using System;
using System.Threading.Tasks;
using SlackConnector;
using SlackConnector.Models;
using System.Linq;

namespace DXVcs2Git.Core.Slack {
    public class SlackBotWrapper {
        const string chat = "#dxvcs2git";
        string botToken;
        ISlackConnector connector;
        ISlackConnection connection;
        bool isConnected;

        public event EventHandler<SlackMessage> MessageReceived;

        public SlackBotWrapper() {
        }
        public void Start(string token) {
            Log.Message($"Started slack bot with token {token}.");

            botToken = token;
            connector = new SlackConnector.SlackConnector();
            Connect();
        }
        public void Stop() {
            if (!isConnected)
                return;
            Log.Message($"Stopped slack bot.");
            BeforeDisconnect();
            connection?.Disconnect();
            connection = null;
        }
        void Connect() {
            if (isConnected)
                return;
            Task.Run(() => {
                Log.Message($"Connecting to slack.");
                connection = connector.Connect(botToken).Result;
                Log.Message($"Connected to slack.");
                connection.OnDisconnect += Connection_OnDisconnect;
                connection.OnMessageReceived += Connection_OnMessageReceived;
                isConnected = true;
            });
        }

        void Connection_OnDisconnect() {
            Log.Message($"Disconnected from slack.");
            BeforeDisconnect();
            Connect();
        }
        void BeforeDisconnect() {
            if (connection == null)
                return;
            connection.OnDisconnect -= Connection_OnDisconnect;
            connection.OnMessageReceived -= Connection_OnMessageReceived;
            isConnected = false;
        }

        async Task Connection_OnMessageReceived(SlackMessage message) {
            Log.Message($"Slack message received.");
            MessageReceived?.Invoke(this, message);
        }

        public void SendMessage(string text) {
            Log.Message("Sending slack message.");
            var conn = connection;
            if (!isConnected || !conn.IsConnected) {
                Log.Error("Slack bot is not connected.");
                return;
            }
            try {
                var chatChannel = conn.ConnectedHubs.Values.FirstOrDefault(x => x.Name == chat);
                conn.Say(new BotMessage() {ChatHub = chatChannel, Text = text});
            }
            catch (Exception ex) {
                Log.Error("Exception while sending slack message", ex);
            }
        }
    }
}
