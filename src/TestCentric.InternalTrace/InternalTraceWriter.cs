// ***********************************************************************
// Copyright (c) Charlie Poole and TestCentric contributors.
// Licensed under the MIT License. See LICENSE file in root directory.
// ***********************************************************************

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
        TextWriter _writer;
        object _myLock = new object();

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
        /// Initialize the trace specifying only the trace level.
        /// </summary>
        /// <param name="level">The trace level</param>
        public void Initialize(InternalTraceLevel level)
        {
            var pid = Process.GetCurrentProcess().Id;
            var logName = $"InternalTrace_{pid}";
            Initialize(logName, level);
        }

        /// <summary>
        /// Initialize the trace writer specifying the path to the
        /// log and the trace level.
        /// </summary>
        /// <param name="logName">Path to the log file</param>
        /// <param name="level">The trace level</param>
        public void Initialize(string logName, InternalTraceLevel level)
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

            if (DefaultTraceLevel > InternalTraceLevel.Off)
            {
                if (logFileChanging)
                    WriteLine($"Log continued from {oldPath}");
                WriteLine($"InternalTrace: Initializing at level {DefaultTraceLevel}");
            }
            
            Initialized = true;
        }

        /// <summary>
        /// Get a Logger specifying the logger name and optionally the  trace level and echo flag
        /// </summary>
        /// <returns>A logger</returns>
        /// <param name="name">Name to use for the logger</param>
        /// <param name="level">Optional trace level for this logger</param>
        /// <param name="echo">If true, logger output is echoed to the console</param>
        public Logger GetLogger(string name, InternalTraceLevel level = InternalTraceLevel.Default, bool echo = false)
        {
            if (level == InternalTraceLevel.Default)
                level = DefaultTraceLevel;

            return new Logger(name, level, this, echo);
        }

        private int _linesWritten = 0;

        /// <summary>
        /// Writes a string followed by a line terminator to the text string or stream.
        /// </summary>
        /// <param name="value">The string to write. If <paramref name="value" /> is null, only the line terminator is written.</param>
        public void WriteLine(string value)
        {
            lock (_myLock)
            {
                // We delay creation of the writer so that we can avoid creation of empty log files
                if (_writer == null)
                {
                    var streamWriter = new StreamWriter(new FileStream(LogPath, FileMode.Create, FileAccess.Write, FileShare.Write));
                    streamWriter.AutoFlush = true;
                    _writer = streamWriter;
                }

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
