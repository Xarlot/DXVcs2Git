namespace DXVcs2Git.UI.ViewModels {
    public enum MessageType {
        Update,
        Refresh,
    }

    public class Message {
        public MessageType MessageType { get; }
        public Message(MessageType messageType) {
            MessageType = messageType;
        }
    }
}
