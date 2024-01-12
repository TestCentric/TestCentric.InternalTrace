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
        private const string LOG_FILE_PATTERN_ENV_VAR = "TESTCENTRIC_INTERNAL_TRACE_LOG_FILE";
        private const string LOG_FILE_PATTERN_DEFAULT = "InternalTrace_{PID}.log";
        private const string TRACE_LEVEL_ENV_VAR = "TESTCENTRIC_INTERNAL_TRACE_LEVEL";

        private static readonly int PROCESS_ID = Process.GetCurrentProcess().Id;
        private const string PROCESS_TOKEN = "{PID}";

        private static readonly string DEFAULT_LOG_FILE_PATTERN =
            Environment.GetEnvironmentVariable(LOG_FILE_PATTERN_ENV_VAR) ?? LOG_FILE_PATTERN_DEFAULT;
        private static readonly string DEFAULT_LOG_FILE_PATH =
            DEFAULT_LOG_FILE_PATTERN.Replace(PROCESS_TOKEN, PROCESS_ID.ToString());
        private static readonly InternalTraceLevel DEFAULT_TRACE_LEVEL =
            TraceLevelFromString(Environment.GetEnvironmentVariable(TRACE_LEVEL_ENV_VAR));

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
        /// Gets the InternalTraceLevel used by this writer. Before Initialize
        /// has been called, this will be InternalTraceLevel.NotSet.
        /// </summary>
        public InternalTraceLevel DefaultTraceLevel { get; private set; }

        #region Construction and Initialization

        /// <summary>
        /// Construct an InternalTraceWriter that writes to a file.
        /// </summary>
        /// <param name="logPath">Path to the file to use</param>
        public InternalTraceWriter()
        {
            LogPath = DEFAULT_LOG_FILE_PATH;
        }

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
            Initialize(DEFAULT_LOG_FILE_PATH, level);
        }

        /// <summary>
        /// Initialize the trace automatically, using environment variables and defaults.
        /// </summary>
        /// <param name="level">The trace traceLevel</param>
        public void Initialize()
        {
            LogPath = DEFAULT_LOG_FILE_PATH;
            DefaultTraceLevel = DEFAULT_TRACE_LEVEL == InternalTraceLevel.NotSet
                ? InternalTraceLevel.Debug
                : DEFAULT_TRACE_LEVEL;
            Initialized = true;

            if (DefaultTraceLevel > InternalTraceLevel.Off)
                WriteLine($"InternalTrace: Initializing automatically at level {DefaultTraceLevel}");
        }

        private static InternalTraceLevel TraceLevelFromString(string setting)
        {
            InternalTraceLevel traceLevel = InternalTraceLevel.NotSet; // This is used as the default

            if (!string.IsNullOrEmpty(setting))
            {
#if NET20
                try
                {
                    traceLevel = (InternalTraceLevel)Enum.Parse(typeof(InternalTraceLevel), setting, true);
                }
                catch(Exception ex) 
                {
                    throw new Exception($"Environment variable TESTCENTRIC_INTERNAL_TRACE has invalid value {setting}", ex);
                }
#else
                if (!Enum.TryParse<InternalTraceLevel>(setting, true, out traceLevel))
                    throw new Exception($"Environment variable TESTCENTRIC_INTERNAL_TRACE has invalid value {setting}");
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
