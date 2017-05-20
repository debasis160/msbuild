﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//-----------------------------------------------------------------------
// </copyright>
// <summary>A base class providing type-safe methods of logging in various contexts.</summary>
//-----------------------------------------------------------------------

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Xml;
using Microsoft.Build.Framework;
using Microsoft.Build.Shared;

using InvalidProjectFileException = Microsoft.Build.Exceptions.InvalidProjectFileException;

namespace Microsoft.Build.BackEnd.Logging
{
    /// <summary>
    /// Intended to be used in the nodes, during a build, where a base LoggingContext is created when the node
    /// initializes for a build (this is the public constructor.)  When a new project, target batch
    /// or task batch is started, the appropriate method on the current LoggingContext is invoked
    /// and a new LoggingContext is returned.  This new LoggingContext should be used for all
    /// subsequent logging within the subcontext.
    /// </summary>
    internal class BaseBuildLoggingContext : BaseLoggingContext
    {
        /// <summary>
        /// True if this context comes from the in-proc node.
        /// </summary>
        private bool _isInProcNode;

        /// <summary>
        /// Constructs the logging context from a logging service and an event context.
        /// </summary>
        /// <param name="loggingService">The logging service to use</param>
        /// <param name="buildEventContext">The event context</param>
        /// <param name="inProc">Flag indicating if this context belongs to an in-proc node.</param>
        protected BaseBuildLoggingContext(ILoggingService loggingService, BuildEventContext buildEventContext, bool inProc) : base(loggingService, buildEventContext)
        {
            _isInProcNode = inProc;
        }

        /// <inheritdoc cref="BaseLoggingContext"/>
        protected BaseBuildLoggingContext(BaseBuildLoggingContext baseContext) : base(baseContext)
        {
            _isInProcNode = baseContext._isInProcNode;
        }

        /// <summary>
        /// Flag indicating if the context is being used for the in-proc node.
        /// </summary>
        public bool IsInProcNode
        {
            [DebuggerStepThrough]
            get
            { return _isInProcNode; }
        }

        /// <summary>
        /// Log an error based on an exception during the execution of a task
        /// </summary>
        /// <param name="file">The file in which the error occurred</param>
        /// <param name="exception">The exception wich is to be logged</param>
        /// <param name="taskName">The task in which the error occurred</param>
        internal void LogFatalTaskError(BuildEventFileInfo file, Exception exception, string taskName)
        {
            ErrorUtilities.VerifyThrow(IsValid, "must be valid");
            LoggingService.LogFatalTaskError(BuildEventContext, exception, file, taskName);
        }
    }
}
