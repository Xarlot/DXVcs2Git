namespace DXVcs2Git.UI.ViewModels {
    public enum MessageType {
        BeforeUpdate,
        Update,
        BeforeRefresh,
        Refresh,
        SelectedBranchChanged,
    }

    public class Message {
        public MessageType MessageType { get; }
        public Message(MessageType messageType) {
            MessageType = messageType;
        }
    }
}
