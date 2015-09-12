using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using log4net;

namespace DXVcs2Git.Core {
    public static class Log {
        private static readonly ILog log = LogManager.GetLogger(typeof(Log));
        public static void Message(string message, Exception ex = null) {
            log.Info(message, ex);
        }
    }
}
