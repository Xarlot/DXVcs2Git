using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DXVcs2Git.Core.Farm {
    public enum FarmNotificationType {
        synctask,
    }

    public abstract class FarmNotification {
        public FarmNotificationType NotificationType { get; set; }
        protected FarmNotification() {

        }
        protected FarmNotification(FarmNotificationType notificationType) {
            NotificationType = NotificationType;
        }
    }

    public class FarmSyncTaskNotification : FarmNotification {
        public string SyncTask { get; set; }
        public string Message { get; set; }
        public FarmSyncTaskNotification() {

        }
        public FarmSyncTaskNotification(FarmNotificationType notificationType, string syncTask, string message) : base(notificationType) {
            SyncTask = syncTask;
            Message = message;
        }
    }
}
