using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DXVcs2Git.AdministratorMethods {
#if AdministratorMethods
    class AdministratorMethodsImpl {
        static void Main(string[] args) {
            switch (args[0]) {
                case "ResetWpf2SlKey":
                    Wpf2slAdministratorMethods.ResetWpf2SlKey(args[1]);
                    return;
                case "SetWpf2SlKey":
                    Wpf2slAdministratorMethods.SetWpf2SlKey(args[1]);
                    return;
                default:
                    break;
            }
        }        
    }
#endif
#if AdministratorMethods || UIDebug
    public static class Wpf2slAdministratorMethods {
        public static void ResetWpf2SlKey(string value) {
            var splitted = GetWpf2slKeys().Where(x => x != value);
            PushWpf2slKeys(splitted);
        }
        public static void SetWpf2SlKey(string value) {
            ResetWpf2SlKey(value);
            PushWpf2slKeys(GetWpf2slKeys().Concat(new string[] { value }));
        }
        static void PushWpf2slKeys(IEnumerable<string> keys) {
            string result = "";
            foreach (var key in keys) {
                result += key + ",";
            }
            if (result.Length > 0)
                result = result.Remove(result.Length - 1);
            Environment.SetEnvironmentVariable("wpf2slkey", result, EnvironmentVariableTarget.Machine);
        }
        static IEnumerable<string> GetWpf2slKeys() {
            var ev = Environment.GetEnvironmentVariable("wpf2slkey", EnvironmentVariableTarget.Machine);
            if (ev == null)
                return Enumerable.Empty<string>();
            return ev.Split(',');
        }
    }
#endif
}
