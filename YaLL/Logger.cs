#region System
using System;
using System.IO;
using System.Linq;
#endregion

#if !NETCOREAPP && !NETSTANDARD
using System.Diagnostics;
using SharpRambo.ExtensionsLib;
#endif

namespace SharpRambo.YaLL
{
    public partial class Logger
    {
        #region GlobalConfig
        public struct GlobalConfiguration
        {
            private static readonly TargetMap _targetMap =
#if NET5_0_OR_GREATER
                new();
#else
                new TargetMap();
#endif
            public static TargetMap TargetMapping
            {
                get => _targetMap;
                set => _targetMap.Set(value);
            }

            private static readonly TargetConfigCollection _targetConfig = TargetConfigCollection.Default;
            public static TargetConfigCollection TargetConfiguration
            {
                get => _targetConfig;
                set => _targetConfig.Set(value);
            }
        }
        #endregion

        #region PublicProperties
        public object Sender { get; }
        public TargetMap TargetMappings { get; private set; } = GlobalConfiguration.TargetMapping;
        public TargetConfigCollection TargetConfiguration { get; set; } = GlobalConfiguration.TargetConfiguration;
        #endregion

        #region PrivateProperties
#if !NETCOREAPP && !NETSTANDARD
        private EventLog _EV { get; set; } = new EventLog();
#endif
        #endregion

        #region Constructor
        public Logger(object sender)
        {
            Sender = sender ?? throw new ArgumentNullException(nameof(sender));
            LogEvent += onLog;
            TargetMappings = GlobalConfiguration.TargetMapping;
            TargetConfiguration = GlobalConfiguration.TargetConfiguration;
        }

        public Logger(object sender, TargetMap targetMap) : this(sender)
        {
            TargetMappings.Set(targetMap);
        }

        public Logger(object sender, TargetMap targetMap, TargetConfigCollection targetConfig) : this(sender, targetMap)
        {
            TargetConfiguration = targetConfig;
        }

        public Logger(object sender, LogEventHandler logEventHandler)
        {
            Sender = sender ?? throw new ArgumentNullException(nameof(sender));

            if (logEventHandler != null)
                LogEvent += logEventHandler;

            TargetMappings = GlobalConfiguration.TargetMapping;
            TargetConfiguration = GlobalConfiguration.TargetConfiguration;
        }

        public Logger(object sender, TargetConfigCollection targetConfig, LogEventHandler logEventHandler) : this(sender, logEventHandler)
        {
            TargetConfiguration = targetConfig;
        }

        public Logger(object sender, TargetMap targetMap, LogEventHandler logEventHandler) : this(sender, logEventHandler)
        {
            TargetMappings.Set(targetMap);
        }

        public Logger(object sender, TargetMap targetMap, TargetConfigCollection targetConfig, LogEventHandler logEventHandler) : this(sender, targetMap, logEventHandler)
        {
            TargetConfiguration = targetConfig;
        }
        #endregion

        #region LogMethods
        public void Log(LogLevel level, LogTarget target, LogEntry entry)
            => LogEvent.Invoke(Sender, new LogEventArgs(Sender.GetType().FullName, level, target, entry ?? throw new ArgumentNullException(nameof(entry))));
        public void Log(LogLevel level, LogTarget target, string message, string section = null, int code = 0)
            => LogEvent.Invoke(Sender, new LogEventArgs(Sender.GetType().FullName, level, target, new LogEntry(message, code, section)));

        public void LogTrace(LogEntry entry)
            => Log(LogLevel.Trace, TargetMappings.Trace, entry);
        public void LogTrace(string message, string section = null)
            => LogTrace(new LogEntry(message, section));
        public void LogTrace(string message, int code, string section = null)
            => LogTrace(new LogEntry(message, getCode(int.MinValue, 1000, code), section));

        public void LogDebug(LogEntry entry)
            => Log(LogLevel.Debug, TargetMappings.Debug, entry ?? throw new ArgumentNullException(nameof(entry)));
        public void LogDebug(string message, string section = null)
            => LogDebug(new LogEntry(message, 1000, section));
        public void LogDebug(string message, int code, string section = null)
            => LogDebug(new LogEntry(message, getCode(1000, 2000, code), section));

        public void LogInfo(LogEntry entry)
            => Log(LogLevel.Information, TargetMappings.Information, entry ?? throw new ArgumentNullException(nameof(entry)));
        public void LogInfo(string message, string section = null)
            => LogInfo(new LogEntry(message, 2000, section));
        public void LogInfo(string message, int code, string section = null)
            => LogInfo(new LogEntry(message, getCode(2000, 3000, code), section));

        public void LogWarning(LogEntry entry)
            => Log(LogLevel.Warning, TargetMappings.Warning, entry ?? throw new ArgumentNullException(nameof(entry)));
        public void LogWarning(string message, string section = null)
            => LogWarning(new LogEntry(message, 3000, section));
        public void LogWarning(string message, int code, string section = null)
            => LogWarning(new LogEntry(message, getCode(3000, 4000, code), section));

        public void LogError(LogEntry entry)
            => Log(LogLevel.Error, TargetMappings.Error, entry ?? throw new ArgumentNullException(nameof(entry)));
        public void LogError(string message, string section = null)
            => LogError(new LogEntry(message, 4000, section));
        public void LogError(string message, int code, string section = null)
            => LogError(new LogEntry(message, getCode(4000, 5000, code), section));

        public void LogCritical(LogEntry entry)
            => Log(LogLevel.Critical, TargetMappings.Critical, entry ?? throw new ArgumentNullException(nameof(entry)));
        public void LogCritical(string message, string section = null)
            => LogCritical(new LogEntry(message, 5000, section));
        public void LogCritical(string message, int code, string section = null)
            => LogCritical(new LogEntry(message, getCode(5000, int.MaxValue, code), section));

        public void LogException(ExceptionLogEntry entry)
            => LogError(entry ?? throw new ArgumentNullException(nameof(entry)));
        public void LogException(Exception ex)
            => LogException(new ExceptionLogEntry(ex));
        public void LogException(Exception ex, int code)
            => LogException(new ExceptionLogEntry(ex, code));
        public void LogException(LogLevel level, LogTarget target, ExceptionLogEntry entry)
            => Log(level, target, entry ?? throw new ArgumentNullException(nameof(entry)));
        public void LogException(LogLevel level, LogTarget target, Exception ex)
            => LogException(level, target, new ExceptionLogEntry(ex));
        public void LogException(LogLevel level, LogTarget target, Exception ex, int code)
            => LogException(level, target, new ExceptionLogEntry(ex, code));

        public void LogData(DataLogEntry entry)
            => LogInfo(entry ?? throw new ArgumentNullException(nameof(entry)));
        public void LogData(LogLevel level, LogTarget target, DataLogEntry entry)
            => Log(level, target, entry ?? throw new ArgumentNullException(nameof(entry)));
        #endregion

        #region EventHandlers
        private void onLog(object sender, LogEventArgs e)
        {
            TargetConfig config = TargetConfiguration.FirstOrDefault(t => t.Level == e.Level);

            if (e.Target.HasFlag(LogTarget.Action) && config != null && config.Actions != null)
                config.Actions.InvokeAll(Sender, e);

            if (e.Target.HasFlag(LogTarget.Console))
                e.WriteToConsole();

#if !NETCOREAPP && !NETSTANDARD
            if (e.Target.HasFlag(LogTarget.EventViewer) && config != null && config.EventViewer != null)
            {
                string log = !e.Entry.Section.IsNull() ? e.Entry.Section : config.EventViewer.DefaultLog;
                EventLogEntryType logType = mapLogLevel(e.Level);

                if (!EventLog.SourceExists(config.EventViewer.Source))
                    EventLog.CreateEventSource(config.EventViewer.Source, log);

                _EV.Source = config.EventViewer.Source;
                _EV.Log = log;

                _EV.WriteEntry(e.ToString(config.EventViewer.LogDetails), logType, e.Entry.Code);
            }
#endif

            if (e.Target.HasFlag(LogTarget.File) && config != null && config.File != null && config.File.File != null)
            {
                FileMode fm = config.File.Append ? FileMode.Append : FileMode.OpenOrCreate;

#if NET5_0_OR_GREATER
                using FileStream fs = new(config.File.File.FullName, fm, FileAccess.Write);
#elif NETCOREAPP3_1_OR_GREATER || NETSTANDARD2_1_OR_GREATER
                using FileStream fs = new FileStream(config.File.File.FullName, fm, FileAccess.Write);
#else
                using (FileStream fs = new FileStream(config.File.File.FullName, fm, FileAccess.Write)) {
#endif
                string data = e.ToString(true, true, true, true) + Environment.NewLine;
                byte[] dataB = config.File.FileEncoding.GetBytes(data);
                int dataC = config.File.FileEncoding.GetByteCount(data);

                fs.Write(dataB, 0, dataC);
                fs.Flush();
#if !NET5_0_OR_GREATER && !NETCOREAPP3_1_OR_GREATER && !NETSTANDARD2_1_OR_GREATER
                }
#endif
            }
        }
        #endregion

        #region HelperMethods
#if !NETCOREAPP && !NETSTANDARD
        private EventLogEntryType mapLogLevel(LogLevel level)
        {
            switch (level)
            {
                case LogLevel.Warning:
                    return EventLogEntryType.Warning;

                case LogLevel.Error:
                    return EventLogEntryType.FailureAudit;

                case LogLevel.Critical:
                    return EventLogEntryType.Error;

                default:
                    return EventLogEntryType.Information;
            }
        }
#endif

        private static int getCode(int min, int max, int input)
            => input >= min && input < max ? input : (input < 1000 ? min + input : min);
        #endregion
    }
}
