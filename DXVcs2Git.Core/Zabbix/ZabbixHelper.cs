using System;

namespace DXVcs2Git.Core.Zabbix {
    public static class ZabbixHelper {
        public static void Send(string branch, string message) {
            try {
                Log.Message($" Send info to zabbix - {Environment.MachineName}.{branch}.git_checkout_time - {message}");
                var r2 = new ZsRequest("ciserver", $"{Environment.MachineName}.{branch}.git_checkout_time", message.Replace(" ", "_"));
                r2.Send("zabbix");
            }
            catch(Exception ex) {
                Log.Message("Send info to zabbix failed with exception ", ex);
            }
        }
    }
}
