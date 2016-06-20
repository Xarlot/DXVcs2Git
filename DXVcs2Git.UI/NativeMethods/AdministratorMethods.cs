using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DevExpress.Mvvm.Native;
using System.Windows;
using System.Reflection;
using System.IO;

namespace DXVcs2Git.UI.NativeMethods {
    public static class AdministratorMethods {
        static void Invoke(string name, string[] arguments) {
            var process = new Process();
            ProcessStartInfo startInfo = new ProcessStartInfo() {
                FileName = Path.Combine(Path.GetDirectoryName(Assembly.GetCallingAssembly().Location), "DXVcs2Git.AdministratorMethods.exe"),
                Arguments = new string[] { name }.Concat(arguments).ConcatStringsWithDelimeter(" "),
                Verb = "runas"
            };
            process.StartInfo = startInfo;
            process.Start();
            process.WaitForExit();
        }
        static string ConcatStringsWithDelimeter(this IEnumerable<string> source, string delimeter) {
            var builder = new StringBuilder();
            foreach (var str in source.InsertDelimeter(delimeter)) {
                builder.Append(str);
            }
            return builder.ToString();
        }
        static IEnumerable<T> InsertDelimeter<T>(this IEnumerable<T> source, T delimeter) {
            var en = source.GetEnumerator();
            if (en.MoveNext())
                yield return en.Current;
            while (en.MoveNext()) {
                yield return delimeter;
                yield return en.Current;
            }
        }
        static async Task InvokeAsync(string name, params string[] arguments) {
            await Task.Run(() => Invoke(name, arguments));
        }
        public static async Task SetWpf2SlKeyAsync(string value) {
#if DEBUG
            await Task.Run(() => DXVcs2Git.AdministratorMethods.Wpf2slAdministratorMethods.SetWpf2SlKey(value));
#else
            await InvokeAsync("SetWpf2SlKey", value);
#endif
        }
        public static async Task ResetWpf2SlKeyAsync(string value) {
#if DEBUG
            await Task.Run(() => DXVcs2Git.AdministratorMethods.Wpf2slAdministratorMethods.ResetWpf2SlKey(value));
#else
            await InvokeAsync("ResetWpf2SlKey", value);
#endif

        }
    }
}
