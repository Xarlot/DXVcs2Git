using System;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;
using CommandLine;
using DevExpress.CCNetSmart.Lib;
using Newtonsoft.Json;
using ThoughtWorks.CruiseControl.Remote;

namespace DXVcs2Git.FarmIntegrator {
    class Program {
        static void Main(string[] args) {
            var result = Parser.Default.ParseArguments<Options>(args);
            try {
                var exitCode = result.MapResult(
                    (Options options) => DoWork(options),
                    err => DoErrors(args));
                Environment.Exit(exitCode);
            }
            catch (Exception ex) {
                Environment.Exit(1);
            }
        }
        static int DoErrors(string[] args) {
            return 1;
        }
        static int DoWork(Options options) {
            string auxPath = options.AuxPath;
            string forcer = options.Forcer;
            string taskname = options.TaskName;
            
            RemoteCruiseManagerFactory f = new RemoteCruiseManagerFactory();
            ISmartCruiseManager m = (ISmartCruiseManager)f.GetCruiseManager(auxPath);
            m.ForceBuild(taskname, forcer);
            m.SendNotification(taskname, forcer, CalcMessage(MessageKind.Refresh));
            return 0;
        }
        static string CalcMessage(MessageKind kind) {
            var message = new Message {MessageKind = kind};
            string json = JsonConvert.SerializeObject(message);
            byte[] bytes = Encoding.UTF8.GetBytes(json);
            return Convert.ToBase64String(bytes);
        }
    }

    enum MessageKind {
        Refresh,
    }
    [DataContract]
    class Message {
        [DataMember]
        public MessageKind MessageKind { get; set; }
        [DataMember]
        public string Parameters { get; set; }
    }
}
