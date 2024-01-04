// ***********************************************************************
// Copyright (c) Charlie Poole and TestCentric contributors.
// Licensed under the MIT License. See LICENSE file in root directory.
// ***********************************************************************

using System.ComponentModel.Design;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Runtime.CompilerServices;
using NUnit.Framework;

namespace TestCentric
{
    public class LoggingTests
    {
        static readonly InternalTraceLevel[] LEVELS = new [] { InternalTraceLevel.Error, InternalTraceLevel.Warning, InternalTraceLevel.Info, InternalTraceLevel.Debug };
        static readonly string LogFileName = "MyLogFile" + Process.GetCurrentProcess().Id;

        [OneTimeTearDown]
        public void Cleanup() 
        {
            InternalTrace.TraceWriter.Close();
            if (File.Exists(LogFileName))
                File.Delete(LogFileName);
        }

        // NOTE: Because InternalTrace is a static class, it may only be initialized once.
        // We use test ordering to first verify that it's uninitialized and that logging
        // before initialization throws an exception before we finally initialize it.
        // After initializing, the remaining tests may run in any order.

        [Test, Order(1)]
        public void InternalTraceIsNotInitializedInitially()
        {
            Assert.That(InternalTrace.Initialized, Is.False);
        }

        [Test, Order(2)]
        public void LogggingBeforeInitializationThrowsException()
        {
            var logger = InternalTrace.GetLogger("MyLogger");

            const string MSG = "This should  throw";
            Assert.That(() => { logger.Info(MSG); },
                Throws.InvalidOperationException.With.Message.Contains(MSG));
        }

        [Test, Order(3)]
        public void CanInitializeInternalTrace()
        {
            InternalTrace.Initialize(LogFileName, InternalTraceLevel.Debug);

            Assert.True(InternalTrace.Initialized);
            Assert.That(InternalTrace.DefaultTraceLevel, Is.EqualTo(InternalTraceLevel.Debug));           
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
        }
    }
}
