﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Build.Shared;

namespace Microsoft.Build.Utilities
{
    /// <summary>
    /// Processor architecture utilities
    /// </summary>
    static public class ProcessorArchitecture
    {
        // Known processor architectures
        internal const string X86 = "x86";
        internal const string IA64 = "IA64";

        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "AMD", Justification = "This is the correct casing for ProcessorArchitecture")]
        internal const string AMD64 = "AMD64";

        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "MSIL", Justification = "This is the correct casing for ProcessorArchitecture")]
        internal const string MSIL = "MSIL";

        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "ARM", Justification = "This is the correct casing for ProcessorArchitecture")]
        internal const string ARM = "ARM";

        /// <summary>
        /// Lazy-initted property for getting the architecture of the currently running process
        /// </summary>
        static public string CurrentProcessArchitecture
        {
            get
            {
                return ProcessorArchitecture.GetCurrentProcessArchitecture();
            }
        }

        // PInvoke delegate for IsWow64Process
        private delegate bool IsWow64ProcessDelegate([In] IntPtr hProcess, [Out] out bool Wow64Process);

        /// <summary>
        /// Gets the processor architecture of the currently running process
        /// </summary>
        /// <returns>null if unknown architecture or error, one of the known architectures otherwise</returns>
        static private string GetCurrentProcessArchitecture()
        {
            string architecture;

            switch (NativeMethodsShared.ProcessorArchitecture)
            {
                case NativeMethodsShared.ProcessorArchitectures.X86:
                    architecture = ProcessorArchitecture.X86;
                    break;

                case NativeMethodsShared.ProcessorArchitectures.X64:
                    architecture = ProcessorArchitecture.AMD64;
                    break;

                case NativeMethodsShared.ProcessorArchitectures.IA64:
                    architecture = ProcessorArchitecture.IA64;
                    break;

                case NativeMethodsShared.ProcessorArchitectures.ARM:
                    architecture = ProcessorArchitecture.ARM;
                    break;

                // unknown architecture? return null
                default:
                    architecture = null;
                    break;
            }

            return architecture;
        }
    }
}
