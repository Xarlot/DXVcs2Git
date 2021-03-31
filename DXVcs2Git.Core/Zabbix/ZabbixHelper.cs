using System;

namespace DXVcs2Git.Core.Zabbix {
    public static class ZabbixHelper {
        public static void Send(string branch, string message) {
            try {
                var r2 = new ZsRequest("ciserver", $"{Environment.MachineName}.{branch}.git_checkout_time", message.Replace(" ", "_"));
                r2.Send("zabbix");
                Log.Message($" Send info to zabbix - {Environment.MachineName}.{branch}.git_checkout_time - {message}");
            }
            catch(Exception ex) {
                Log.Message("Send info to zabbix failed with exception ", ex);
            }
        }
    }
}
