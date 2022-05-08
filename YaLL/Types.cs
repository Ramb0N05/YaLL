#region System
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
#endregion

using Newtonsoft.Json;
using SharpRambo.ExtensionsLib;

namespace SharpRambo.YaLL
{
    public partial class Logger
    {
        #region EnumTypes
        [Flags]
        public enum LogTarget
        {
            Action = 1,
            ActionAsync = 2,
            Console = 3,
#if !NETCOREAPP && !NETSTANDARD
            EventViewer = 4,
#endif
            File = 5,
            None = 0
        }

        public enum LogLevel
        {
            Trace,
            Debug,
            Information,
            Warning,
            Error,
            Critical
        }
        #endregion

        #region Interfaces
        public interface ILogGovernor
        {
            string Issuer { get; }
            DateTime CreationTime { get; }

            string ToString();
        }
        #endregion

        #region Types
        #region Collections
        public class LogActionCollection : List<Action<object, LogEventArgs>>
        {
            public LogActionCollection() : base() { }
            public LogActionCollection(int capacity) : base(capacity) { }
            public LogActionCollection(IEnumerable<Action<object, LogEventArgs>> collection) : base(collection) { }

            public void InvokeAll(object sender, LogEventArgs e)
            {
                foreach (Action<object, LogEventArgs> a in this)
                {
                    Type attType = typeof(System.Runtime.CompilerServices.AsyncStateMachineAttribute);

                    if (a.Method.GetCustomAttributes(attType, true).Any())
                        a.BeginInvoke(sender, e, a.EndInvoke, null);
                    else
                        a.Invoke(sender, e);
                }
            }

            public IAsyncResult[] InvokeAllAsync(object sender, LogEventArgs e)
            {
                List<IAsyncResult> result =
#if NET5_0_OR_GREATER
                    new();
#else
                    new List<IAsyncResult>();
#endif

                foreach (Action<object, LogEventArgs> a in this)
                    result.Add(a.BeginInvoke(sender, e, a.EndInvoke, null));

                return result.ToArray();
            }
        }

        public class TargetConfigCollection : List<TargetConfig>
        {
            public TargetConfigCollection() : base() { }
            public TargetConfigCollection(int capacity) : base(capacity) { }
            public TargetConfigCollection(IEnumerable<TargetConfig> collection) : base(collection) { }

            public static TargetConfigCollection Default
#if NET5_0_OR_GREATER
                => new() {
#else
                => new TargetConfigCollection() {
#endif
                new TargetConfig(LogLevel.Trace),
                new TargetConfig(LogLevel.Debug),
                new TargetConfig(LogLevel.Information),
                new TargetConfig(LogLevel.Warning),
                new TargetConfig(LogLevel.Error),
                new TargetConfig(LogLevel.Critical)
            };

            public new void Add(TargetConfig item)
            {
                List<TargetConfig> match = FindAll(c => c.Level == item.Level);

                if (match.IsNull())
                    base.Add(item);
                else
                    SetItem(item);
            }

            public void Set(TargetConfigCollection configCollection)
            {
                if (configCollection == null)
                    throw new ArgumentNullException(nameof(configCollection));

                configCollection.ForEach(config => SetItem(config));
            }

            public void SetItem(TargetConfig config)
            {
                if (config == null)
                    throw new ArgumentNullException(nameof(config));

                TargetConfig source = this.FirstOrDefault(c => c.Level == config.Level);

                if (source != null)
                    source.Set(config);
            }
        }
#endregion

        public class LogGovernor : ILogGovernor
        {
            public string Issuer { get; protected set; }
            public DateTime CreationTime { get; protected set; }

            public LogGovernor()
            {
                CreationTime = DateTime.Now;
            }

            public LogGovernor(string issuer) : this()
            {
                Issuer = issuer;
            }

            string ILogGovernor.ToString()
                => GetType().FullName + ": " + Issuer;

            public override string ToString()
                => GetType().FullName + ": " + Issuer;

            public static Logger GetLogger()
#if NET5_0_OR_GREATER
                => new(new LogGovernor());
#else
                => new Logger(new LogGovernor());
#endif

            public static Logger GetLogger(string issuer)
#if NET5_0_OR_GREATER
                => new(new LogGovernor(issuer));
#else
                => new Logger(new LogGovernor(issuer));
#endif
        }

#if !NETCOREAPP && !NETSTANDARD
        public class EventViewerConfig
        {
            public string Source { get; set; }
            public string DefaultLog { get; set; }
            public bool LogDetails { get; set; }

            public EventViewerConfig() { }

            public EventViewerConfig(string source) : this()
            {
                Source = source;
            }

            public EventViewerConfig(string source, string defaultLog) : this(source)
            {
                DefaultLog = defaultLog ?? "EventLog";
            }

            public EventViewerConfig(string source, string defaultLog, bool logDetails) : this(source, defaultLog)
            {
                LogDetails = logDetails;
            }
        }
#endif

        public class FileConfig
        {
            public FileInfo File { get; set; }
            public Encoding FileEncoding { get; set; }
            public bool Append { get; set; } = true;

            public FileConfig() { }

            public FileConfig(FileInfo file) : this()
            {
                File = file;
            }

            public FileConfig(FileInfo file, bool append) : this(file)
            {
                Append = append;
            }

            public FileConfig(FileInfo file, bool append, Encoding encoding) : this(file, append)
            {
                FileEncoding = encoding ?? Encoding.UTF8;
            }
        }

        public class TargetConfig
        {
            public LogLevel Level { get; set; }
            public LogActionCollection Actions { get; set; }
            public FileConfig File { get; set; }
#if !NETCOREAPP && !NETSTANDARD
            public EventViewerConfig EventViewer { get; set; }
#endif

            public TargetConfig(LogLevel level)
            {
                Level = level;
            }

            public TargetConfig(LogLevel level, LogActionCollection actions) : this(level)
            {
                Actions = actions;
            }

            public TargetConfig(LogLevel level, Action<object, LogEventArgs> action)
                : this(level, new LogActionCollection { action }) { }

            public TargetConfig(LogLevel level, LogActionCollection actions, FileConfig file) : this(level, actions)
            {
                File = file;
            }

            public TargetConfig(LogLevel level, Action<object, LogEventArgs> action, FileConfig file)
                : this(level, new LogActionCollection { action }, file) { }

#if !NETCOREAPP && !NETSTANDARD
            public TargetConfig(LogLevel level, LogActionCollection actions, FileConfig file, EventViewerConfig eventViewer) : this(level, actions, file)
            {
                EventViewer = eventViewer;
            }

            public TargetConfig(LogLevel level, Action<object, LogEventArgs> action, FileConfig file, EventViewerConfig eventViewer)
                : this(level, new LogActionCollection { action }, file, eventViewer) { }
#endif

            public void Set(TargetConfig config)
            {
                if (config == null)
                    throw new ArgumentNullException(nameof(config));

                if (Level == config.Level)
                {
                    Actions = config.Actions;
                    File = config.File;
#if !NETCOREAPP && !NETSTANDARD
                    EventViewer = config.EventViewer;
#endif
                }
            }
        }

        public class TargetMap
        {
            public static TargetMap Default
#if NET5_0_OR_GREATER
                => new();
#else
                => new TargetMap();
#endif

            public LogTarget Trace { get; private set; }
            public LogTarget Debug { get; private set; }
            public LogTarget Information { get; private set; }
            public LogTarget Warning { get; private set; }
            public LogTarget Error { get; private set; }
            public LogTarget Critical { get; private set; }

            public TargetMap(LogTarget? trace = null, LogTarget? debug = null, LogTarget? info = null, LogTarget? warn = null, LogTarget? error = null, LogTarget? critical = null)
            {
                Trace = trace ?? LogTarget.None;
                Debug = debug ?? LogTarget.Console;
                Information = info ?? LogTarget.Console;
                Warning = warn ?? LogTarget.Console;
                Error = error ?? LogTarget.Console;
                Critical = critical ?? LogTarget.Console;
            }

            public void Set(LogLevel level, LogTarget targets)
            {
                switch (level)
                {
                    case LogLevel.Trace:
                        Trace = targets;
                        break;

                    case LogLevel.Debug:
                        Debug = targets;
                        break;

                    case LogLevel.Information:
                        Information = targets;
                        break;

                    case LogLevel.Warning:
                        Warning = targets;
                        break;

                    case LogLevel.Error:
                        Error = targets;
                        break;

                    case LogLevel.Critical:
                        Critical = targets;
                        break;
                }
            }

            public void Set(TargetMap map)
            {
                if (map == null)
                    throw new ArgumentNullException(nameof(map));

                Trace = map.Trace;
                Debug = map.Debug;
                Information = map.Information;
                Warning = map.Warning;
                Error = map.Error;
                Critical = map.Critical;
            }

            public LogTarget Get(LogLevel level)
            {
#if NETCOREAPP2_2_OR_GREATER || NETSTANDARD2_1_OR_GREATER
                return level switch {
                    LogLevel.Trace => Trace,
                    LogLevel.Information => Information,
                    LogLevel.Warning => Warning,
                    LogLevel.Error => Error,
                    LogLevel.Critical => Critical,
                    _ => Debug,
                };
#else
                switch (level)
                {
                    case LogLevel.Trace:
                        return Trace;

                    case LogLevel.Information:
                        return Information;

                    case LogLevel.Warning:
                        return Warning;

                    case LogLevel.Error:
                        return Error;

                    case LogLevel.Critical:
                        return Critical;

                    default: // LogLevel.Debug
                        return Debug;
                }
#endif
            }
        }

        public class LogEntry
        {
            public string Message { get; set; }
            public string Section { get; set; }
            public string Comment { get; set; }
            public int Code { get; set; } = 0;

            public LogEntry() { }

            public LogEntry(int code)
            {
                Code = code;
            }

            public LogEntry(string message)
            {
                Message = message;
            }

            public LogEntry(string message, string section) : this(message)
            {
                Section = section;
            }

            public LogEntry(string message, int code) : this(message)
            {
                Code = code;
            }

            public LogEntry(string message, int code, string section) : this(message, code)
            {
                Section = section;
            }
        }

        public class ExceptionLogEntry : LogEntry
        {
            public const string ExceptionSectionName = "Exception";

            public Exception ExceptionObject { get; }

            public ExceptionLogEntry(Exception ex)
            {
                ExceptionObject = ex ?? throw new ArgumentNullException(nameof(ex));
                Message = ExceptionObject.Message;
                Section = ExceptionSectionName;
                Comment = nameof(ExceptionObject.Source) + ": " + ExceptionObject.Source + Environment.NewLine +
                          "Trace: " + Environment.NewLine + ExceptionObject.StackTrace;
            }

            public ExceptionLogEntry(Exception ex, int code) : this(ex)
            {
                Code = code;
            }
        }

        public class DataLogEntry : LogEntry
        {
            public const string DataSectionName = "Data";

            public enum DataLogType
            {
                Added,
                Changed,
                Error,
                Removed,
                Unknown
            }

            public object Data { get; }
            public DataLogType Type { get; } = DataLogType.Unknown;
            public string EditorName { get; }

            public DataLogEntry(object data, DataLogType type) : base(null, DataSectionName)
            {
                Data = data ?? throw new ArgumentNullException(nameof(data));
                Type = type;
                Comment = Type.ToString();
                Message = JsonConvert.SerializeObject(Data, Formatting.Indented);
            }

            public DataLogEntry(object data, DataLogType type, string editorName) : this(data, type)
            {
                string seperator = "|||";
                string seperatorReplacement = "___";

                EditorName = editorName?.Replace(seperator, seperatorReplacement);
                Comment = (!EditorName.IsNull() ? EditorName + seperator : string.Empty) + Type.ToString().Replace(seperator, seperatorReplacement);
            }

            public DataLogEntry(object data, DataLogType type, int code) : this(data, type)
            {
                Code = code;
            }

            public DataLogEntry(object data, DataLogType type, int code, string editorName) : this(data, type, editorName)
            {
                Code = code;
            }
        }
#endregion
    }
}
