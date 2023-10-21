// ***********************************************************************
// Copyright (c) Charlie Poole and TestCentric contributors.
// Licensed under the MIT License. See LICENSE file in root directory.
// ***********************************************************************

using System.IO;

namespace TestCentric
{
    /// <summary>
    /// A trace listener that writes to a separate file per domain
    /// and process using it.
    /// </summary>
    public class InternalTraceWriter : TextWriter
    {
        TextWriter _writer;
        string _logPath;
        object _myLock = new object();

        private TextWriter Writer
        {
            get
            {
                // We delay creation of the writer so that we can avoid creation of empty log files
                if (_writer == null)
                {
                    var streamWriter = new StreamWriter(new FileStream(_logPath, FileMode.Append, FileAccess.Write, FileShare.Write));
                    streamWriter.AutoFlush = true;
                    _writer = streamWriter;
                }
                return _writer;
            }
        }

        /// <summary>
        /// Construct an InternalTraceWriter that writes to a file.
        /// </summary>
        /// <param name="logPath">Path to the file to use</param>
        public InternalTraceWriter(string logPath)
        {
            _logPath = logPath;
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
        /// Returns the character encoding in which the output is written.
        /// </summary>
        /// <returns>The character encoding in which the output is written.</returns>
        public override System.Text.Encoding Encoding
        {
            get { return Writer.Encoding; }
        }

        /// <summary>
        /// Writes a character to the text string or stream.
        /// </summary>
        /// <param name="value">The character to write to the text stream.</param>
        public override void Write(char value)
        {
            lock (_myLock)
            {
                Writer.Write(value);
            }
        }

        /// <summary>
        /// Writes a string to the text string or stream.
        /// </summary>
        /// <param name="value">The string to write.</param>
        public override void Write(string value)
        {
            lock (_myLock)
            {
                Writer.Write(value);
            }
        }

        /// <summary>
        /// Writes a string followed by a line terminator to the text string or stream.
        /// </summary>
        /// <param name="value">The string to write. If <paramref name="value" /> is null, only the line terminator is written.</param>
        public override void WriteLine(string value)
        {
            lock (_myLock)
            {
                Writer.WriteLine(value);
            }
        }

        /// <summary>
        /// Releases the unmanaged resources used by the <see cref="T:System.IO.TextWriter" /> and optionally releases the managed resources.
        /// </summary>
        /// <param name="disposing">true to release both managed and unmanaged resources; false to release only unmanaged resources.</param>
        protected override void Dispose(bool disposing)
        {
            lock (_myLock)
            {
                if (disposing && _writer != null)
                {
                    Writer.Flush();
                    Writer.Dispose();
                    _writer = null;
                }
            }

            base.Dispose(disposing);
        }

        /// <summary>
        /// Clears all buffers for the current writer and causes any buffered data to be written to the underlying device.
        /// </summary>
        public override void Flush()
        {
            if ( _writer != null )
                _writer.Flush();
        }
    }
}
