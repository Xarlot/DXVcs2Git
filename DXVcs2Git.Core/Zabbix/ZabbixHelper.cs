using System;
using Microsoft.Win32;
using Zabbix_Sender;

namespace DXVcs2Git.Core.Zabbix {
    public static class ZabbixHelper {
        public static void Send(string branch, string message) {
            try {
                string hostName = GetHostName();
                Log.Message($" Send info to zabbix - {hostName}.{branch}.git_checkout_time - {message}");
                var r2 = new ZS_Request("ciserver", $"{hostName}.{branch}.git_checkout_time", message.Replace(" ", "_"));
                r2.Send("zabbix");
            }
            catch (Exception ex) {
                Log.Message("Send info to zabbix failed with exception ", ex);
            }
        }
        static string GetHostName() {
            try {
                using RegistryKey registryKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64).OpenSubKey(@"SOFTWARE\Microsoft\Virtual Machine\Guest\Parameters");
                return ((string)registryKey.GetValue("HostName")).Split(new char[] {'.'})[0];
            }
            catch (Exception ex) {
                return Environment.MachineName;
            }
        }
    }
}
