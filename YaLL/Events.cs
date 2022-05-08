using System;
using SharpRambo.ExtensionsLib;

namespace SharpRambo.YaLL
{
    public partial class Logger
    {
        #region Events
        public class LogEventArgs
        {
            public string ClassName { get; private set; }
            public DateTime Timestamp { get; private set; }
            public LogLevel Level { get; private set; }
            public LogTarget Target { get; private set; }
            public LogEntry Entry { get; private set; } = new LogEntry();

            public LogEventArgs(string className)
            {
                ClassName = !className.IsNull() ? className : throw new ArgumentNullException(nameof(className));
                Timestamp = DateTime.Now;
            }

            public LogEventArgs(string className, LogEntry entry) : this(className)
            {
                Entry = entry;
            }

            public LogEventArgs(string className, string logText) : this(className)
            {
                Entry = new LogEntry(logText);
                Level = LogLevel.Information;
                Target = LogTarget.None;
            }

            public LogEventArgs(string className, int code) : this(className)
            {
                Entry = new LogEntry(code);
            }

            public LogEventArgs(string className, LogLevel level) : this(className)
            {
                Level = level;
            }

            public LogEventArgs(string className, LogLevel level, LogTarget target) : this(className, level)
            {
                Target = target;
            }

            public LogEventArgs(string className, LogLevel level, LogTarget target, LogEntry entry) : this(className, level, target)
            {
                Entry = entry;
            }

            public LogEventArgs(string className, string logText, LogLevel level) : this(className, logText)
            {
                Level = level;
            }

            public LogEventArgs(string className, LogLevel level, LogEntry entry) : this(className, level)
            {
                Entry = entry;
            }

            public LogEventArgs(string className, string logText, LogLevel level, LogTarget target) : this(className, logText, level)
            {
                Target = target;
            }

            public LogEventArgs(string className, string logText, LogLevel level, LogTarget target, LogEntry entry) : this(className, level, target)
            {
                Entry = entry;
            }

            public LogEventArgs(string className, string logText, LogLevel level, LogTarget target, string section) : this(className, logText, level, target)
            {
                Entry = new LogEntry(logText, section);
            }

            public LogEventArgs(string className, string logText, string section) : this(className, logText)
            {
                Entry = new LogEntry(logText, section);
            }

            public LogEventArgs(string className, string logText, int code) : this(className, logText)
            {
                Entry = new LogEntry(logText, code);
            }

            public LogEventArgs(string className, string logText, string section, int code) : this(className, logText, section)
            {
                Entry = new LogEntry(logText, code, section);
            }

            public LogEventArgs(string className, string logText, LogLevel level, LogTarget target, string section, int code) : this(className, logText, level, target, section)
            {
                Entry = new LogEntry(logText, code, section);
            }

            public override string ToString()
                => ToString(true, false, false, false);

            public string ToString(bool logDetails = true, bool section = false, bool code = false, bool inline = false)
                => logDetails
                    ? nameof(Timestamp) + ": " + Timestamp.ToString("G") + (inline ? " - " : Environment.NewLine) +
                      nameof(ClassName) + ": " + ClassName + (inline ? " - " : Environment.NewLine) +
                      nameof(Level) + ": " + string.Format("{0} ({1})", Level.ToString(), (int)Level) + (inline ? " - " : Environment.NewLine) +
                      nameof(Target) + ": " + string.Format("{0} ({1})", Target.ToString(), (int)Target) + (inline ? " - " : Environment.NewLine) +
                      (section ? nameof(Entry.Section) + ": " + Entry.Section + (inline ? " - " : Environment.NewLine) : string.Empty) +
                      (code ? nameof(Entry.Code) + ": " + Entry.Code.ToString() + (inline ? " - " : Environment.NewLine) : string.Empty) +
                      (inline ? string.Empty : Environment.NewLine) + "Message: " + (inline ? string.Empty : Environment.NewLine) +
                      (inline ? Entry.Message.Replace(Environment.NewLine, " - ") : Entry.Message)
                    : (inline ? Entry.Message.Replace(Environment.NewLine, " - ") : Entry.Message);

            public void WriteToConsole()
                => Console.WriteLine(ToString(true, true, true, true));
        }

        public delegate void LogEventHandler(object sender, LogEventArgs e);
        public event LogEventHandler LogEvent;
        #endregion
    }
}
