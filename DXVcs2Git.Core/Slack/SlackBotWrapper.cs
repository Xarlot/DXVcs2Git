using System.Threading.Tasks;
using SlackConnector;
using SlackConnector.Models;

namespace DXVcs2Git.Core.Slack {
    public class SlackBotWrapper {
        readonly string botToken;
        readonly ISlackConnector connector;
        ISlackConnection connection;
        bool isConnected;
        public SlackBotWrapper(string token) {
            botToken = token;
            connector = new SlackConnector.SlackConnector();
            Connect();
        }
        void Connect() {
            if (isConnected)
                return;
            Task.Run(() => {
                connection = connector.Connect(botToken).Result;
                connection.OnDisconnect += Connection_OnDisconnect;
                connection.OnMessageReceived += MessageReceived;
                isConnected = true;
            });
        }

        void Connection_OnDisconnect() {
            connection.OnDisconnect -= Connection_OnDisconnect;
            connection.OnMessageReceived -= MessageReceived;
            isConnected = false;
            Connect();
        }

        async Task MessageReceived(SlackMessage message) {
            var text = message.Text;
        }
    }
}
