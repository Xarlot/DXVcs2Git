using ThoughtWorks.CruiseControl.Remote;

namespace DXVcs2Git.GitTools.Farm {

    public class SmartProjectActivity {
        static ProjectActivity typePending = new ProjectActivity("Pending: no ready servers with specified type");
        public static ProjectActivity TypePending {
            get {
                return typePending;
            }
        }
        static ProjectActivity typeStopped = new ProjectActivity("Stopped: no servers with specified type");
        public static ProjectActivity TypeStopped {
            get {
                return typeStopped;
            }
        }
        static ProjectActivity preparingRemoteDir = new ProjectActivity("Preparing remote directory...");
        public static ProjectActivity PreparingRemoteDir {
            get {
                return preparingRemoteDir;
            }
        }
        static ProjectActivity serverNotResponding = new ProjectActivity("Server not responding. Waiting...");
        public static ProjectActivity ServerNotResponding {
            get {
                return serverNotResponding;
            }
        }

    }
}
