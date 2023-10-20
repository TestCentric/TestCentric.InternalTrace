// ***********************************************************************
// Copyright (c) Charlie Poole and TestCentric contributors.
// Licensed under the MIT License. See LICENSE file in root directory.
// ***********************************************************************

namespace TestCentric
{
    /// <summary>
    /// InternalTraceLevel is an enumeration controlling the
    /// level of detailed presented in the InternalTrace logs.
    /// </summary>
    public enum InternalTraceLevel
    {
        /// <summary>
        /// Use the default trace level as specified by the user.
        /// If not specified, defaults to 'Off'.
        /// </summary>
        Default = 0,

        /// <summary>
        /// Do not display any trace messages
        /// </summary>
        Off = 1,

        /// <summary>
        /// Display Error messages only
        /// </summary>
        Error = 2,

        /// <summary>
        /// Display Warning level and higher messages
        /// </summary>
        Warning = 3,

        /// <summary>
        /// Display informational and higher messages
        /// </summary>
        Info = 4,

        /// <summary>
        /// Display debug messages and higher - i.e. all messages
        /// </summary>
        Debug = 5,

        /// <summary>
        /// Display debug messages and higher - i.e. all messages
        /// </summary>
        Verbose = 5
    }
}
