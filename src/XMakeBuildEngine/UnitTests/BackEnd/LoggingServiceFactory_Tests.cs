﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//-----------------------------------------------------------------------
// </copyright>
// <summary>Verify the LoggingService Factory</summary>
//-----------------------------------------------------------------------

using Microsoft.Build.BackEnd;
using Microsoft.Build.BackEnd.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Build.UnitTests.Logging
{
    /// <summary>
    ///Test the Factory to create components of the type LoggingService
    /// </summary>
    [TestClass]
    public class LoggingServiceFactory_Tests
    {
        /// <summary>
        /// Verify we can create a synchronous LoggingService
        /// </summary>
        [TestMethod]
        public void TestCreateSynchronousLogger()
        {
            LoggingServiceFactory factory = new LoggingServiceFactory(LoggerMode.Synchronous, 1);
            LoggingService loggingService = (LoggingService)factory.CreateInstance(BuildComponentType.LoggingService);
            Assert.IsTrue(loggingService.LoggingMode == LoggerMode.Synchronous, "Expected to create a Synchronous LoggingService");
        }

        /// <summary>
        /// Verify we can create a Asynchronous LoggingService
        /// </summary>
        [TestMethod]
        public void TestCreateAsynchronousLogger()
        {
            LoggingServiceFactory factory = new LoggingServiceFactory(LoggerMode.Asynchronous, 1);
            LoggingService loggingService = (LoggingService)factory.CreateInstance(BuildComponentType.LoggingService);
            Assert.IsTrue(loggingService.LoggingMode == LoggerMode.Asynchronous, "Expected to create an Asynchronous LoggingService");
        }
    }
}