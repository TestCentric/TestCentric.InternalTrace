// ***********************************************************************
// Copyright (c) Charlie Poole and TestCentric contributors.
// Licensed under the MIT License. See LICENSE file in root directory.
// ***********************************************************************

using NUnit.Framework;
using NUnit.Framework.Internal;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace TestCentric
{
    public class InternalTraceTests
    {
        // NOTE: Since InternalTrace is a static class and is in use by the
        // test runner while executing these tests we can only do tests here,
        // which observe its state but do not change it. This is not a problem,
        // however, since the actual implentation for each function is found
        // in the InternalTestWriter class, where they are tested.

        static readonly InternalTraceLevel[] LEVELS = new[] { InternalTraceLevel.Error, InternalTraceLevel.Warning, InternalTraceLevel.Info, InternalTraceLevel.Debug };
        static readonly string DEFAULT_LOG_FILE = $"InternalTrace_{Process.GetCurrentProcess().Id}.log";

        [Test]
        public void DefaultSettings()
        {
            // NOTE: We know that InternalTrace is uninitialized because we run
            // these tests under nunitlite without calling Initialize(). So this
            // test may fail if a different runner is used.
            // TODO: Remove this limitation.
            Assert.False(InternalTrace.Initialized);
            Assert.That(InternalTrace.DefaultTraceLevel, Is.EqualTo(InternalTraceLevel.NotSet));
            Assert.NotNull(InternalTrace.TraceWriter);
            Assert.That(InternalTrace.TraceWriter.LogPath, Is.EqualTo(DEFAULT_LOG_FILE));
        }

        [Test]
        public void GetLoggerWithDefaultTraceLevel()
        {
            var logger = InternalTrace.GetLogger("MyLogger");
            Assert.That(logger.TraceLevel, Is.EqualTo(InternalTrace.DefaultTraceLevel));
            Assert.That(logger.TraceWriter, Is.SameAs(InternalTrace.TraceWriter));
        }

        [TestCaseSource(nameof(LEVELS))]
        public void GetLoggerWithSpecifiedTraceLevel(InternalTraceLevel level)
        {
            var logger = InternalTrace.GetLogger("MyLogger", level);
            Assert.That(logger.TraceLevel, Is.EqualTo(level));
            Assert.That(logger.TraceWriter, Is.SameAs(InternalTrace.TraceWriter));
        }

        [Test]
        public void GetLoggerThatEchoesToTheConsole()
        {
            var logger = InternalTrace.GetLogger("MyLogger", InternalTraceLevel.Info, echo: true);
            Assert.True(logger.EchoToConsole);
        }
    }
}
