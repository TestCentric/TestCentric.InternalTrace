// ***********************************************************************
// Copyright (c) Charlie Poole and TestCentric contributors.
// Licensed under the MIT License. See LICENSE file in root directory.
// ***********************************************************************

using NUnit.Framework;
using NUnit.Framework.Internal;
using System;
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
            Assert.That(_traceWriter.DefaultTraceLevel, Is.EqualTo(InternalTraceLevel.Default));
            Assert.That(_traceWriter.LogPath, Is.EqualTo(DEFAULT_LOG_FILE));
        }

        [Test]
        public void Initialization()
        {
            const string LOG_FILE = "LogFile.log";
            const InternalTraceLevel LEVEL = InternalTraceLevel.Info;

            _traceWriter.Initialize(LOG_FILE, LEVEL);

            Assert.True(_traceWriter.Initialized);
            Assert.That(_traceWriter.DefaultTraceLevel, Is.EqualTo(LEVEL));
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
                Assert.That(logger.TraceLevel, Is.EqualTo(InternalTraceLevel.Default));
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
        [TestCase(InternalTraceLevel.Default)]
        public void LogWithoutIntializing(InternalTraceLevel expectedLevel)
        {
            var logger = expectedLevel != InternalTraceLevel.Default
                ? _traceWriter.GetLogger("MyLogger", expectedLevel)
                : _traceWriter.GetLogger("MyLogger");

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
                case InternalTraceLevel.Default:
                    logger.Info("My message");
                    expectedLevel = InternalTraceLevel.Info;
                    break;
            }

            _traceWriter.Close();

            CheckTraceOutput(DEFAULT_LOG_FILE, new[] {
                $"{expectedLevel}.*MyLogger: My message"
            });
        }

        [Test]
        public void LogBeforeAndAfterInitialization_SingleFile()
        {
            var logger = _traceWriter.GetLogger("Mylogger");

            logger.Debug("My debug message");
            logger.Info("My first message");

            _traceWriter.Initialize(InternalTraceLevel.Info);

            logger.Debug("This should not appear");
            logger.Info("My second message");

            _traceWriter.Close();

            CheckTraceOutput(DEFAULT_LOG_FILE, new[]
            {
                "My debug message",
                "My first message",
                "InternalTrace: Initializing at level Info",
                "My second message"
            });
        }

        [Test]
        public void LogBeforeAndAfterInitialization_TwoFiles()
        {
            var logger = _traceWriter.GetLogger("Mylogger");

            logger.Debug("My debug message");
            logger.Info("My first message");

            _traceWriter.Initialize("SecondLogFile.log", InternalTraceLevel.Info);

            logger.Debug("This should not appear");
            logger.Info("My second message");

            _traceWriter.Close();

            CheckTraceOutput(DEFAULT_LOG_FILE, new[]
            {
                "My debug message",
                "My first message",
                "Log continues in file SecondLogFile.log"
            });

            CheckTraceOutput("SecondLogFile.log", new[]
            {
                $"Log continued from {DEFAULT_LOG_FILE}",
                "InternalTrace: Initializing at level Info",
                "My second message"
            });
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
    }
}
