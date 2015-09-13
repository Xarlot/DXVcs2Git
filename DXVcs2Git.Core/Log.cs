using System;
using log4net;

namespace DXVcs2Git.Core {
    public static class Log {
        static Log() {
            log4net.Config.XmlConfigurator.Configure();
        }
        private static readonly ILog log = LogManager.GetLogger(typeof(Log));
        public static void Message(string message, Exception ex = null) {
            log.Info(message, ex);
        }
        public static void Error(string message, Exception exception) {
            log.Error(message, exception);
        }
    }
}
