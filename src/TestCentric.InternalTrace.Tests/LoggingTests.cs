// ***********************************************************************
// Copyright (c) Charlie Poole and TestCentric contributors.
// Licensed under the MIT License. See LICENSE file in root directory.
// ***********************************************************************

using System.Diagnostics;
using System.IO;
using NUnit.Framework;

namespace TestCentric
{
    [TestFixtureSource(nameof(LEVELS))]
    public class LoggingTests
    {
        static readonly InternalTraceLevel[] LEVELS = new [] { InternalTraceLevel.Error, InternalTraceLevel.Warning, InternalTraceLevel.Info, InternalTraceLevel.Debug };
        static readonly string LogFileName = "MyLogFile" + Process.GetCurrentProcess().Id;

        private InternalTraceLevel _logLevel;

        public LoggingTests(InternalTraceLevel logLevel)
        {
            _logLevel = logLevel;
        }

        [OneTimeTearDown]
        public void Cleanup() 
        {
            InternalTrace.TraceWriter.Close();
            if (File.Exists(LogFileName))
                File.Delete(LogFileName);
        }

        [TestCaseSource(nameof(LEVELS))]
        public void LoggerSelectsMessagesToWrite(InternalTraceLevel msgLevel)
        {
            // Simulate getting logger from InternalTrace after initializationi
            var writer = new StringWriter();
            var traceWriter = new InternalTraceWriter(writer) { Initialized = true };
            var logger = new Logger("MyLogger", _logLevel, traceWriter);

            Assert.That(logger.TraceLevel, Is.EqualTo(_logLevel));

            var msg = "This is my message";

            switch (msgLevel)
            {
                case InternalTraceLevel.Error:
                    logger.Error(msg);
                    break;
                case InternalTraceLevel.Warning:
                    logger.Warning(msg);
                    break;
                case InternalTraceLevel.Info:
                    logger.Info(msg);
                    break;
                case InternalTraceLevel.Debug:
                    logger.Debug(msg);
                    break;
            }

            var output = writer.ToString();

            if (_logLevel >= msgLevel)
            {
                Assert.That(output, Contains.Substring($" {msgLevel} "));
                Assert.That(output, Does.EndWith($"MyLogger: {msg}" + System.Environment.NewLine));
            }
            else
                Assert.That(output, Is.Empty);
        }
    }
}
