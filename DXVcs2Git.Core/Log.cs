using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Windows.Threading;
using log4net;
using log4net.Appender;
using log4net.Core;
using log4net.Repository.Hierarchy;

namespace DXVcs2Git.Core {
    public static class Log {
        static Log() {
            log4net.Config.XmlConfigurator.Configure();
        }
        private static readonly ILog log = LogManager.GetLogger(typeof(Log));
        private static readonly StringBuilder errorsAccumulator = new StringBuilder();
        public static void Message(string message, Exception ex = null) {
            log.Info(message, ex);
        }
        public static void ResetErrorsAccumulator() {
            errorsAccumulator.Clear();
        }
        public static void Error(string message, Exception exception = null) {
            log.Error(message, exception);
            errorsAccumulator.AppendLine(message);
        }
        public static string GetLog() {
            ILog log = log4net.LogManager.GetLogger("PoC");

            var hierarchy = (Hierarchy)LogManager.GetRepository();
            var mappender = (LimitedMemoryAppender)hierarchy.Root.GetAppender("LimitedMemoryAppender");
            StringBuilder sb = new StringBuilder();
            using (TextWriter writer = new StringWriter(sb)) {
                foreach (var ev in mappender.GetEvents()) {
                    mappender.Layout.Format(writer, ev);
                }
            }
            return sb.ToString();
        }
        public static string GetErrorsAccumulatorContent() {
            return errorsAccumulator.ToString();
        }
    }

    public class LimitedMemoryAppender : AppenderSkeleton {
        readonly Queue<LoggingEvent> eventsList = new Queue<LoggingEvent>();
        public virtual FixFlags Fix { get; set; } = FixFlags.All;
        public int MaxLength { get; set; } = 100;

        public virtual LoggingEvent[] GetEvents() {
            return this.eventsList.ToArray();
        }
        protected override void Append(LoggingEvent loggingEvent) {
            loggingEvent.Fix = this.Fix;
            this.eventsList.Enqueue(loggingEvent);
            if (this.eventsList.Count >= MaxLength)
                this.eventsList.Dequeue();
        }
        public virtual void Clear() {
            this.eventsList.Clear();
        }
    }

    public static class LogIntegrator {
        static DispatcherTimer Timer;

        static LogIntegrator() {
        }
        public static void Start(Dispatcher currentDispatcher, Action refreshed) {
            Timer = new DispatcherTimer(DispatcherPriority.ContextIdle, currentDispatcher);
            Timer.Interval = TimeSpan.FromSeconds(2);
            Timer.Tick += (s, e) => refreshed();
            Timer.Start();
        }
        public static void Stop() {
            Timer.Stop();
        }
    }
}
