﻿// ***********************************************************************
// Copyright (c) Charlie Poole and TestCentric contributors.
// Licensed under the MIT License. See LICENSE file in root directory.
// ***********************************************************************

using System;
using System.IO;

namespace TestCentric
{
    /// <summary>
    /// Provides internal logging to the NUnit framework
    /// </summary>
    public class Logger
    {
        private const string TimeFmt = "HH:mm:ss.fff";
        private const string TraceFmt = "{0} {1,-5} [{2,2}] {3}: {4}";

        private readonly string _name;
        private bool _echo;

        public InternalTraceLevel TraceLevel { get; }
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
            _name = index >= 0 ? fullName.Substring(index + 1) : fullName;

            _echo = echo;
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

        private void Log(InternalTraceLevel level, string message)
        {
            if (TraceWriter != null && TraceLevel >= level)
                WriteLog(level, message);
        }

        private void Log(InternalTraceLevel level, string format, params object[] args)
        {
            string message = string.Format(format, args);
            Log(level, message);
        }

        private void WriteLog(InternalTraceLevel level, string message)
        {
#if NET20 || NET30 || NET35 || NET40
            int threadId = System.Threading.Thread.CurrentThread.ManagedThreadId;
#else
            int threadId = Environment.CurrentManagedThreadId;
#endif

            TraceWriter.WriteLine(TraceFmt,
                DateTime.Now.ToString(TimeFmt),
                level,
                threadId,
                _name,
                message);

            if (_echo)
                Console.WriteLine(TraceFmt,
                    DateTime.Now.ToString(TimeFmt),
                    level,
                    threadId,
                    _name,
                    message);
        }
    }
}
