// ***********************************************************************
// Copyright (c) Charlie Poole and TestCentric contributors.
// Licensed under the MIT License. See LICENSE file in root directory.
// ***********************************************************************

using System.ComponentModel.Design;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using NUnit.Framework;

namespace TestCentric
{
    public class LoggingTests
    {
        static readonly InternalTraceLevel[] LEVELS = new [] { InternalTraceLevel.Error, InternalTraceLevel.Warning, InternalTraceLevel.Info, InternalTraceLevel.Debug };
        static readonly string LogFileName = "MyLogFile" + Process.GetCurrentProcess().Id;

        [OneTimeSetUp]
        public void InitializeInternalTrace()
        {
            InternalTrace.Initialize(LogFileName, InternalTraceLevel.Debug);
        }

        [OneTimeTearDown]
        public void Cleanup() 
        {
            InternalTrace.TraceWriter.Close();
            if (File.Exists(LogFileName))
                File.Delete(LogFileName);
        }

        [Test, Combinatorial]
        public void LoggerSelectsMessagesToWrite(
            [ValueSource(nameof(LEVELS))] InternalTraceLevel logLevel,
            [ValueSource(nameof(LEVELS))] InternalTraceLevel msgLevel)
        {
            var writer = new StringWriter();
            var logger = new Logger("MyLogger", logLevel, new InternalTraceWriter(writer));

            Assert.That(logger.TraceLevel, Is.EqualTo(logLevel));

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

            if (logLevel >= msgLevel)
            {
                Assert.That(output, Contains.Substring($" {msgLevel} "));
                Assert.That(output, Does.EndWith($"MyLogger: {msg}" + System.Environment.NewLine));
            }
            else
                Assert.That(output, Is.Empty);
        }

        [Test]
        public void GetLoggerWithDefaultTraceLevel()
        {
            var logger = InternalTrace.GetLogger("MyLogger");
            Assert.That(logger.TraceLevel, Is.EqualTo(InternalTrace.DefaultTraceLevel));
            Assert.NotNull(logger.TraceWriter);
        }

        [TestCaseSource(nameof(LEVELS))]
        public void GetLoggerWithSpecifiedTraceLevel(InternalTraceLevel level)
        {
            var logger = InternalTrace.GetLogger("MyLogger", level);
            Assert.That(logger.TraceLevel, Is.EqualTo(level));
            Assert.NotNull(logger.TraceWriter);
        }

        [Test]
        public void GetLoggerThatEchoesToTheConsole()
        {
            var logger = InternalTrace.GetLogger("MyLogger", InternalTraceLevel.Info, echo: true);
            Assert.NotNull(logger.TraceWriter);
            logger.Info("This should display on the console");
            logger.TraceWriter.Flush();
        }
    }
}
