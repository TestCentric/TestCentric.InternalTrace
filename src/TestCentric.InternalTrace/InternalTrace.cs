// ***********************************************************************
// Copyright (c) Charlie Poole and TestCentric contributors.
// Licensed under the MIT License. See LICENSE file in root directory.
// ***********************************************************************

using System;
using System.Diagnostics;

namespace TestCentric
{
    /// <summary>
    /// InternalTrace provides facilities for tracing the execution
    /// of the NUnit framework. Tests and classes under test may make use 
    /// of Console writes, System.Diagnostics.Trace or various loggers and
    /// NUnit itself traps and processes each of them. For that reason, a
    /// separate internal trace is needed.
    /// 
    /// Note:
    /// InternalTrace uses a global lock to allow multiple threads to write
    /// trace messages. This can easily make it a bottleneck so it must be 
    /// used sparingly. Keep the trace Level as low as possible and only
    /// insert InternalTrace writes where they are needed.
    /// TODO: add some buffering and a separate writer thread as an option.
    /// </summary>
    public static class InternalTrace
    {
        /// <summary>
        /// Gets a flag indicating whether Initialize has been called
        /// </summary>
        public static bool Initialized => TraceWriter.Initialized;

        /// <summary>
        /// TraceLevel as initially set by user in call to Initialize
        /// </summary>
        public static InternalTraceLevel DefaultTraceLevel => TraceWriter.DefaultTraceLevel;

        /// <summary>
        /// The TraceWriter used for logging
        /// </summary>
        public static InternalTraceWriter TraceWriter { get; } = new InternalTraceWriter($"InternalTrace_{Process.GetCurrentProcess().Id}");

        /// <summary>
        /// Initialize the internal trace facility using the name of the log
        /// to be written to and the trace level.
        /// </summary>
        /// <param name="logName">The log name</param>
        /// <param name="level">The trace level</param>
        public static void Initialize(string logName, InternalTraceLevel level)
            => TraceWriter.Initialize(logName, level);

        /// <summary>
        /// Get a Logger specifying the logger name.
        /// </summary>
        /// <returns>A logger</returns>
        /// <param name="name">Name to use for the logger</param>
        /// <param name="level">Optional trace level for this logger</param>
        /// <param name="echo">If true, logger output is echoed to the console</param>
        public static Logger GetLogger(string name, InternalTraceLevel level = InternalTraceLevel.Default, bool echo = false)
        {
            if (level == InternalTraceLevel.Default)
                level = DefaultTraceLevel;

            return new Logger(name, level, TraceWriter, echo);
        }

        /// <summary>
        /// Get a logger named for a particular Type.
        /// </summary>
        /// <returns>A logger</returns>
        /// <param name="type">Type whose name is used for for the logger</param>
        /// <param name="level">Optional trace level for this logger</param>
        /// <param name="echo">If true, logger output is echoed to the console</param>
        public static Logger GetLogger(Type type, InternalTraceLevel level = InternalTraceLevel.Default, bool echo = false)
        {
            return GetLogger(type.FullName, level, echo);
        }
    }
}
