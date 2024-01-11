// ***********************************************************************
// Copyright (c) Charlie Poole and TestCentric contributors.
// Licensed under the MIT License. See LICENSE file in root directory.
// ***********************************************************************

using NUnit.Framework;
using NUnit.Framework.Internal;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace TestCentric
{
    public class InternalTraceWriterTests
    {
        static readonly InternalTraceLevel[] LEVELS = new[] { InternalTraceLevel.Error, InternalTraceLevel.Warning, InternalTraceLevel.Info, InternalTraceLevel.Debug };
        static readonly string DEFAULT_LOG_FILE = "InternalTrace_" + Process.GetCurrentProcess().Id;

        InternalTraceWriter _traceWriter;

        [SetUp]
        public void CreateTraceWriter()
        {
            _traceWriter = new InternalTraceWriter(DEFAULT_LOG_FILE);
        }

        [TearDown]
        public void Cleanup()
        {
            _traceWriter.Close();
            if (File.Exists(DEFAULT_LOG_FILE))
                File.Delete(DEFAULT_LOG_FILE);
        }

        [Test]
        public void DefaultSettings()
        {
            Assert.False(_traceWriter.Initialized);
            Assert.That(_traceWriter.DefaultTraceLevel, Is.EqualTo(InternalTraceLevel.NotSet));
            Assert.That(_traceWriter.LogPath, Is.EqualTo(DEFAULT_LOG_FILE));
        }

        [TestCaseSource(nameof(LEVELS))]
        [TestCase(InternalTraceLevel.Off)]
        public void InitializeWithTraceLevel(InternalTraceLevel level)
        {
            _traceWriter.Initialize(level);

            Assert.True(_traceWriter.Initialized);
            Assert.That(_traceWriter.DefaultTraceLevel, Is.EqualTo(level));
            Assert.That(_traceWriter.LogPath, Is.EqualTo(DEFAULT_LOG_FILE));
        }

        [Test]
        public void InitializeWithLogFile()
        {
            const string LOG_FILE = "LogFile.log";
            _traceWriter.Initialize(LOG_FILE);

            Assert.True(_traceWriter.Initialized);
            Assert.That(_traceWriter.DefaultTraceLevel, Is.EqualTo(InternalTraceLevel.Off));
            Assert.That(_traceWriter.LogPath, Is.EqualTo(LOG_FILE));
        }

        [TestCaseSource(nameof(LEVELS))]
        [TestCase(InternalTraceLevel.Off)]
        public void InitializeWithLogFileAndTraceLevel(InternalTraceLevel level)
        {
            const string LOG_FILE = "LogFile.log";
            _traceWriter.Initialize(LOG_FILE, level);

            Assert.True(_traceWriter.Initialized);
            Assert.That(_traceWriter.DefaultTraceLevel, Is.EqualTo(level));
            Assert.That(_traceWriter.LogPath, Is.EqualTo(LOG_FILE));
        }

        [Test]
        public void InitializeMoreThanOnce()
        {
            _traceWriter.Initialize("LogFile1.log", InternalTraceLevel.Info);

            Assert.Multiple(() =>
            {
                Assert.True(_traceWriter.Initialized);
                Assert.That(_traceWriter.DefaultTraceLevel, Is.EqualTo(InternalTraceLevel.Info));
                Assert.That(_traceWriter.LogPath, Is.EqualTo("LogFile1.log"));
            });

            _traceWriter.Initialize("LogFile2.log", InternalTraceLevel.Debug);

            Assert.Multiple(() =>
            {
                Assert.True(_traceWriter.Initialized);
                Assert.That(_traceWriter.DefaultTraceLevel, Is.EqualTo(InternalTraceLevel.Debug));
                Assert.That(_traceWriter.LogPath, Is.EqualTo("LogFile2.log"));
            });
        }

        [Test]
        public void GetLoggerWithDefaultTraceLevel()
        {
            var logger = _traceWriter.GetLogger("MyLogger");
            Assert.Multiple(() =>
            {
                Assert.That(logger.Name, Is.EqualTo("MyLogger"));
                Assert.That(logger.TraceLevel, Is.EqualTo(InternalTraceLevel.NotSet));
                Assert.That(logger.TraceWriter, Is.SameAs(_traceWriter));
                Assert.False(logger.EchoToConsole);
            });
        }

        [TestCaseSource(nameof(LEVELS))]
        public void GetLoggerWithSpecifiedTraceLevel(InternalTraceLevel level)
        {
            var logger = _traceWriter.GetLogger("MyLogger", level);

            Assert.Multiple(() =>
            {
                Assert.That(logger.Name, Is.EqualTo("MyLogger"));
                Assert.That(logger.TraceLevel, Is.EqualTo(level));
                Assert.That(logger.TraceWriter, Is.SameAs(_traceWriter));
                Assert.False(logger.EchoToConsole);
            });
        }

        [Test]
        public void GetLoggerThatEchoesToTheConsole()
        {
            var logger = _traceWriter.GetLogger("MyLogger", InternalTraceLevel.Info, echo: true);

            Assert.Multiple(() =>
            {
                Assert.That(logger.Name, Is.EqualTo("MyLogger"));
                Assert.That(logger.TraceLevel, Is.EqualTo(InternalTraceLevel.Info));
                Assert.That(logger.TraceWriter, Is.SameAs(_traceWriter));
                Assert.True(logger.EchoToConsole);
                logger.Info("This should display on the console");
            });
        }

        [TestCaseSource(nameof(LEVELS))]
        public void LogWithoutIntializingWhen_LoggerSpecifiedLevel(InternalTraceLevel expectedLevel)
        {
            var logger = _traceWriter.GetLogger("MyLogger", expectedLevel);

            switch (expectedLevel)
            {
                case InternalTraceLevel.Debug:
                    logger.Debug("My message");
                    break;
                case InternalTraceLevel.Warning:
                    logger.Warning("My message");
                    break;
                case InternalTraceLevel.Info:
                    logger.Info("My message");
                    break;
                case InternalTraceLevel.Error:
                    logger.Error("My message");
                    break;
            }

            _traceWriter.Close();

            CheckTraceOutput(DEFAULT_LOG_FILE,
                "^InternalTrace: Initializing automatically at level Debug$",
                $"{expectedLevel}.*MyLogger: My message");
        }

        [TestCaseSource(nameof(LEVELS))]
        public void LogWithoutIntializing_NoLoggerSpecifiedLevel(InternalTraceLevel expectedLevel)
        {
            var logger = _traceWriter.GetLogger("MyLogger");

            switch (expectedLevel)
            {
                case InternalTraceLevel.Debug:
                    logger.Debug("My message");
                    break;
                case InternalTraceLevel.Warning:
                    logger.Warning("My message");
                    break;
                case InternalTraceLevel.Info:
                    logger.Info("My message");
                    break;
                case InternalTraceLevel.Error:
                    logger.Error("My message");
                    break;
            }

            _traceWriter.Close();

            CheckTraceOutput(DEFAULT_LOG_FILE,
                "^InternalTrace: Initializing automatically at level Debug$",
                $"{expectedLevel}.*MyLogger: My message");
        }

        [TestCaseSource(nameof(LEVELS))]
        public void LogBeforeAndAfterInitialization_SingleFile(InternalTraceLevel loggerLevel)
        {
            var logger = _traceWriter.GetLogger("MyLogger", loggerLevel);
            var logger2 = _traceWriter.GetLogger("Logger2");  // level not set

            logger.Debug("My first DEBUG message");
            logger.Info("My first INFO message");
            logger2.Info("Displays before initialization");

            _traceWriter.Initialize(InternalTraceLevel.Info);

            logger.Debug("My second DEBUG message");
            logger.Info("My second INFO message");
            logger2.Info("Displays after initialization");

            _traceWriter.Close();

            switch (loggerLevel)
            {
                case InternalTraceLevel.Debug:
                    CheckTraceOutput(DEFAULT_LOG_FILE,
                        "^InternalTrace: Initializing automatically at level Debug$",
                        "Debug.*MyLogger: My first DEBUG message",
                        "Info.*MyLogger: My first INFO message",
                        "Info.*Logger2: Displays before initialization",
                        "^InternalTrace: Initializing at level Info$",
                        "Debug.*MyLogger: My second DEBUG message",
                        "Info.*MyLogger: My second INFO message",
                        "Info.*Logger2: Displays after initialization");
                    break;
                case InternalTraceLevel.Info:
                    CheckTraceOutput(DEFAULT_LOG_FILE,
                        "^InternalTrace: Initializing automatically at level Debug$",
                        "Info.*MyLogger: My first INFO message",
                        "Info.*Logger2: Displays before initialization",
                        "^InternalTrace: Initializing at level Info$",
                        "Info.*MyLogger: My second INFO message",
                        "Info.*Logger2: Displays after initialization");
                    break;
                default:
                    CheckTraceOutput(DEFAULT_LOG_FILE,
                        "^InternalTrace: Initializing automatically at level Debug$",
                        "Info.*Logger2: Displays before initialization",
                        "^InternalTrace: Initializing at level Info$",
                        "Info.*Logger2: Displays after initialization");
                    break;
            }
        }

        [Test]
        public void LogBeforeAndAfterInitialization_TwoFiles()
        {
            var logger = _traceWriter.GetLogger("MyLogger", InternalTraceLevel.Debug);
            var logger2 = _traceWriter.GetLogger("Logger2");  // level not set

            logger.Debug("My first DEBUG message");
            logger.Info("My first INFO message");
            logger2.Info("Displays before initialization");

            _traceWriter.Initialize("SecondLogFile.log", InternalTraceLevel.Info);

            logger.Debug("My second DEBUG message");
            logger.Info("My second INFO message");
            logger2.Info("Displays after initialization");

            _traceWriter.Close();

            CheckTraceOutput(DEFAULT_LOG_FILE,
                "^InternalTrace: Initializing automatically at level Debug$",
                "Debug.*MyLogger: My first DEBUG message",
                "Info.*MyLogger: My first INFO message",
                "Info.*Logger2: Displays before initialization",
                "^Log continues in file SecondLogFile.log$");

            CheckTraceOutput("SecondLogFile.log",
                $"^Log continued from {DEFAULT_LOG_FILE}$",
                "^InternalTrace: Initializing at level Info$",
                "Debug.*MyLogger: My second DEBUG message",
                "Info.*MyLogger: My second INFO message",
                "Info.*Logger2: Displays after initialization");
        }

        private void CheckTraceOutput(string logFile, params string[] expected)
        {
            Assert.That(File.Exists(logFile), $"Log file {logFile} was not found.");

            string[] lines = File.ReadAllLines(logFile);

            foreach (string line in lines)
                Console.WriteLine(line);

            Assert.Multiple(() => {
                for (int i = 0; i < Math.Min(lines.Length, expected.Length); i++)
                {
                    Assert.That(lines[i], Does.Match(expected[i]));
                }

                Assert.That(lines.Length, Is.EqualTo(expected.Length));
            });
        }

        private void FileMustNotExist(string logFile)
        {
            if (File.Exists(logFile))
            {
                string[] lines = File.ReadAllLines(logFile);

                Assert.Fail(
                    $"Log file {logFile} should not have been created but was...\r\n" +
                    $"  ->{string.Join("\r\n  ->", lines)}");
            }
        }
    }
}
