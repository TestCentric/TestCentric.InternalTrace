// ***********************************************************************
// Copyright (c) Charlie Poole and TestCentric contributors.
// Licensed under the MIT License. See LICENSE file in root directory.
// ***********************************************************************

using System;
using System.Diagnostics;
using System.IO;

namespace TestCentric
{
    /// <summary>
    /// A trace listener that writes to a separate file per domain
    /// and process using it.
    /// </summary>
    public class InternalTraceWriter
    {
        private const string TIME_FORMAT = "HH:mm:ss.fff";
        private const string TRACE_FORMAT = "{0} {1,-5} [{2,2}] {3}: {4}";
        static readonly int PROCESS_ID = Process.GetCurrentProcess().Id;

        TextWriter _writer;
        object _myLock = new object();

        // Number of writes to current log file
        int _linesWritten = 0;

        /// <summary>
        /// Gets a flag indicating whether the InternalTraceWriter is initialized
        /// </summary>
        public bool Initialized { get; set; } = false;

        public string LogPath { get; private set; }

        /// <summary>
        /// TraceLevel as initially set by user in call to Initialize
        /// </summary>
        public InternalTraceLevel DefaultTraceLevel { get; private set; }

        /// <summary>
        /// Construct an InternalTraceWriter that writes to a file.
        /// </summary>
        /// <param name="logPath">Path to the file to use</param>
        public InternalTraceWriter(string logPath)
        {
            LogPath = logPath;
        }

        #region Construction and Initialization

        /// <summary>
        /// Construct an InternalTraceWriter that writes to a 
        /// TextWriter provided by the caller.
        /// </summary>
        /// <param name="writer"></param>
        public InternalTraceWriter(TextWriter writer)
        {
            _writer = writer;
        }

        /// <summary>
        /// Initialize the trace writer specifying the path to the
        /// log and the trace traceLevel.
        /// </summary>
        /// <param name="logName">Path to the log file</param>
        /// <param name="level">Optionally, the trace traceLevel, which defaults to Off</param>
        public void Initialize(string logName, InternalTraceLevel level = InternalTraceLevel.Off)
        {
            bool logFileChanging = _linesWritten > 0 && logName != LogPath;
            if (logFileChanging)
            {
                WriteLine($"Log continues in file {logName}");
                Close();
                _linesWritten = 0;
            }

            var oldPath = LogPath;
            LogPath = logName;
            DefaultTraceLevel = level;
            Initialized = true;

            if (DefaultTraceLevel > InternalTraceLevel.Off)
            {
                if (logFileChanging)
                    WriteLine($"Log continued from {oldPath}");
                WriteLine($"InternalTrace: Initializing at level {DefaultTraceLevel}");
            }
        }

        /// <summary>
        /// Initialize the trace specifying only the trace traceLevel.
        /// </summary>
        /// <param name="level">The trace traceLevel</param>
        public void Initialize(InternalTraceLevel level)
        {
            // TODO: Should we take this from the environment variable?
            var logName = $"InternalTrace_{PROCESS_ID}";
            Initialize(logName, level);
        }

        /// <summary>
        /// Initialize the trace automatically, using environment variables and defaults.
        /// </summary>
        /// <param name="level">The trace traceLevel</param>
        public void Initialize()
        {
            var logName = GetDefaultLogFilePath();
            var traceLevel = GetDefaultTraceLevel();
            if (traceLevel == InternalTraceLevel.NotSet)
                traceLevel = InternalTraceLevel.Debug;

            LogPath = logName;
            DefaultTraceLevel = traceLevel;
            Initialized = true;

            if (DefaultTraceLevel > InternalTraceLevel.Off)
                WriteLine($"InternalTrace: Initializing automatically at level {DefaultTraceLevel}");
        }

        private string GetDefaultLogFilePath()
        {
            var logSetting = Environment.GetEnvironmentVariable("TESTCENTRIC_INTERNAL_TRACE_LOG_FILE");

            if (logSetting == null)
                return $"InternalTrace_{PROCESS_ID}";

            return logSetting.Replace("{PID}", PROCESS_ID.ToString());
        }

        private InternalTraceLevel GetDefaultTraceLevel()
        {
            var traceSetting = Environment.GetEnvironmentVariable("TESTCENTRIC_INTERNAL_TRACE_LEVEL");
            InternalTraceLevel traceLevel = InternalTraceLevel.NotSet; // This is used as the default

            if (!string.IsNullOrEmpty(traceSetting))
            {
#if NET20
                try
                {
                    traceLevel = (InternalTraceLevel)Enum.Parse(typeof(InternalTraceLevel), traceSetting, true);
                }
                catch(Exception ex) 
                {
                    throw new Exception($"Environment variable TESTCENTRIC_INTERNAL_TRACE has invalid value {traceSetting}", ex);
                }
#else
                if (!Enum.TryParse<InternalTraceLevel>(traceSetting, true, out traceLevel))
                    throw new Exception($"Environment variable TESTCENTRIC_INTERNAL_TRACE has invalid value {traceSetting}");
#endif
            }

            return traceLevel;
        }

        #endregion

        /// <summary>
        /// Get a Logger specifying the logger name and optionally the  trace traceLevel and echo flag
        /// </summary>
        /// <returns>A logger</returns>
        /// <param name="name">Name to use for the logger</param>
        /// <param name="level">Optional trace traceLevel for this logger</param>
        /// <param name="echo">If true, logger output is echoed to the console</param>
        public Logger GetLogger(string name, InternalTraceLevel level = InternalTraceLevel.NotSet, bool echo = false)
        {
            if (level == InternalTraceLevel.NotSet)
                level = DefaultTraceLevel;

            return new Logger(name, level, this, echo);
        }

        public void WriteLog(string loggerName, InternalTraceLevel level, string message, bool echoToConsole=false)
        {
#if NET20 || NET30 || NET35 || NET40
            int threadId = System.Threading.Thread.CurrentThread.ManagedThreadId;
#else
            int threadId = Environment.CurrentManagedThreadId;
#endif
            string formattedMessage = string.Format(TRACE_FORMAT,
                DateTime.Now.ToString(TIME_FORMAT),
                level,
                threadId,
                loggerName,
                message);

            WriteLine(formattedMessage);

            if (echoToConsole)
                Console.WriteLine(formattedMessage);
        }

        public void WriteLogEntry(Logger logger, InternalTraceLevel level, string message)
        {
            if (logger.TraceLevel >= level || logger.TraceLevel == InternalTraceLevel.NotSet)
                WriteLog(logger.Name, level, message);
        }

        /// <summary>
        /// Writes a string followed by a line terminator to the text string or stream.
        /// </summary>
        /// <param name="value">The string to write. If <paramref name="value" /> is null, only the line terminator is written.</param>
        public void WriteLine(string value)
        {
            lock (_myLock)
            {
                // We are about to write, if needed do just-in-time self-initialization
                if (!Initialized)
                    Initialize();

                // We delay creation of the StreamWriter so that we can avoid creation of empty log files
                if (_writer == null)
                    _writer = new StreamWriter(new FileStream(LogPath, FileMode.Create, FileAccess.Write, FileShare.Write))
                    {
                        AutoFlush = true
                    };

                _writer.WriteLine(value);
                _linesWritten++;
            }
        }

        /// <summary>
        /// Releases the unmanaged resources used by the <see cref="T:System.IO.TextWriter" /> and optionally releases the managed resources.
        /// </summary>
        public void Close()
        {
            lock (_myLock)
            {
                if (_writer != null)
                {
                    _writer.Flush();
                    _writer.Dispose();
                    _writer = null;
                }
            }
        }
    }
}
