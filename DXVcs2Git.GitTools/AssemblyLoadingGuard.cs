using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace DXVcs2Git.GitTools {
    public static class AssemblyLoadingGuard {
        static readonly object Locker = new object();
        static bool isInitialized;
        static readonly HashSet<string> RequestedAssemblies = new HashSet<string>();
        public static void Protect() {
            if (!isInitialized) {
                lock (Locker) {
                    if (!isInitialized) {
                        //default assembly loading engine seems broken by vs 2013
                        AppDomain.CurrentDomain.AssemblyResolve += CurrentDomainAssemblyResolve;
                        isInitialized = true;
                        LoadXpfLibraries();
                    }
                }
            }
        }
        static Assembly CurrentDomainAssemblyResolve(object sender, ResolveEventArgs args) {
            var result = AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(assembly => assembly.FullName == args.Name);
            if (result == null && args.Name.Contains("DevExpress.Xpf") && args.Name.Contains(AssemblyInfo.VSuffixWithoutSeparator)) {
                if (RequestedAssemblies.Contains(args.Name))
                    return null;
                lock (Locker) {
                    RequestedAssemblies.Add(args.Name);
                    result = Assembly.Load(args.Name);
                }
            }
            return result;
        }
        public static void LoadXpfLibraries() {
            string path = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            foreach (var file in Directory.GetFiles(path, "DevExpress.*.dll", SearchOption.AllDirectories)) {
                //#pragma warning disable CS0618 // Type or member is obsolete
                Assembly assembly = Assembly.LoadFrom(file);
                //#pragma warning restore CS0618 // Type or member is obsolete
                RequestedAssemblies.Add(assembly.FullName);
            }
        }
    }

}
