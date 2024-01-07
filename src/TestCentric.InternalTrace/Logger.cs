// ***********************************************************************
// Copyright (c) Charlie Poole and TestCentric contributors.
// Licensed under the MIT License. See LICENSE file in root directory.
// ***********************************************************************

namespace TestCentric
{
    /// <summary>
    /// Provides internal logging to the NUnit framework
    /// </summary>
    public class Logger
    {
        public  string Name { get; }
        public bool EchoToConsole { get; }

        public InternalTraceLevel TraceLevel { get; private set; }
        public InternalTraceWriter TraceWriter { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="Logger"/> class.
        /// </summary>
        /// <param name="fullName">The name.</param>
        /// <param name="level">The log level.</param>
        /// <param name="traceWriter">The writer where logs are sent.</param>
        /// <param name="echo">If true, echo all output to System.Console.</param>
        public Logger(string fullName, InternalTraceLevel level, InternalTraceWriter traceWriter, bool echo = false)
        {
            TraceLevel = level;
            TraceWriter = traceWriter;

            var index = fullName.LastIndexOf('.');
            Name = index >= 0 ? fullName.Substring(index + 1) : fullName;

            EchoToConsole = echo;
        }

        /// <summary>
        /// Logs the message at error level.
        /// </summary>
        /// <param name="message">The message.</param>
        public void Error(string message)
        {
            Log(InternalTraceLevel.Error, message);
        }

        /// <summary>
        /// Logs the message at error level.
        /// </summary>
        /// <param name="format">The message.</param>
        /// <param name="args">The message arguments.</param>
        public void Error(string format, params object[] args)
        {
            Log(InternalTraceLevel.Error, format, args);
        }

        /// <summary>
        /// Logs the message at warm level.
        /// </summary>
        /// <param name="message">The message.</param>
        public void Warning(string message)
        {
            Log(InternalTraceLevel.Warning, message);
        }

        /// <summary>
        /// Logs the message at warning level.
        /// </summary>
        /// <param name="format">The message.</param>
        /// <param name="args">The message arguments.</param>
        public void Warning(string format, params object[] args)
        {
            Log(InternalTraceLevel.Warning, format, args);
        }

        /// <summary>
        /// Logs the message at info level.
        /// </summary>
        /// <param name="message">The message.</param>
        public void Info(string message)
        {
            Log(InternalTraceLevel.Info, message);
        }

        /// <summary>
        /// Logs the message at info level.
        /// </summary>
        /// <param name="format">The message.</param>
        /// <param name="args">The message arguments.</param>
        public void Info(string format, params object[] args)
        {
            Log(InternalTraceLevel.Info, format, args);
        }

        /// <summary>
        /// Logs the message at debug level.
        /// </summary>
        /// <param name="message">The message.</param>
        public void Debug(string message)
        {
            Log(InternalTraceLevel.Verbose, message);
        }

        /// <summary>
        /// Logs the message at debug level.
        /// </summary>
        /// <param name="format">The message.</param>
        /// <param name="args">The message arguments.</param>
        public void Debug(string format, params object[] args)
        {
            Log(InternalTraceLevel.Verbose, format, args);
        }

        private void Log(InternalTraceLevel level, string format, params object[] args)
        {
            string message = string.Format(format, args);
            Log(level, message);
        }

        private void Log(InternalTraceLevel level, string message)
        {
            // Logger without a specified TraceLevel uses the TraceWriter's
            // TraceLevel, which is set at the time of initialization. 
            // Since it's possible that the Logger was created before initialization,
            // we set it once again here.
            if (TraceLevel == InternalTraceLevel.NotSet)
                TraceLevel = TraceWriter.DefaultTraceLevel;

            TraceWriter.WriteLogEntry(this, level, message);
        }
    }
}
