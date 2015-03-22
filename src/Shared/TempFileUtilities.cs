﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//-----------------------------------------------------------------------
// </copyright>
// <summary>Methods to create temp files.</summary>
//-----------------------------------------------------------------------

using System;
using System.IO;

namespace Microsoft.Build.Shared
{
    /// <summary>
    /// This class contains utility methods for file IO.
    /// It is in a separate file so that it can be selectively included into an assembly.
    /// </summary>
    static internal partial class FileUtilities
    {
        /// <summary>
        /// Generates a unique directory name in the temporary folder.  
        /// Caller must delete when finished. 
        /// </summary>
        internal static string GetTemporaryDirectory()
        {
            string temporaryDirectory = Path.Combine(Path.GetTempPath(), "Temporary" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(temporaryDirectory);

            return temporaryDirectory;
        }

        /// <summary>
        /// Generates a unique temporary file name with a given extension in the temporary folder.
        /// If no extension is provided, uses ".tmp".
        /// File is guaranteed to be unique.
        /// Caller must delete it when finished.
        /// </summary>
        internal static string GetTemporaryFile()
        {
            return GetTemporaryFile(".tmp");
        }

        /// <summary>
        /// Generates a unique temporary file name with a given extension in the temporary folder.
        /// File is guaranteed to be unique.
        /// Extension may have an initial period.
        /// Caller must delete it when finished.
        /// May throw IOException.
        /// </summary>
        internal static string GetTemporaryFile(string extension)
        {
            return GetTemporaryFile(null, extension);
        }

        /// <summary>
        /// Creates a file with unique temporary file name with a given extension in the specified folder.
        /// File is guaranteed to be unique.
        /// Extension may have an initial period.
        /// If folder is null, the temporary folder will be used.
        /// Caller must delete it when finished.
        /// May throw IOException.
        /// </summary>
        internal static string GetTemporaryFile(string directory, string extension)
        {
            ErrorUtilities.VerifyThrowArgumentLengthIfNotNull(directory, "directory");
            ErrorUtilities.VerifyThrowArgumentLength(extension, "extension");

            if (extension[0] != '.')
            {
                extension = '.' + extension;
            }

            string file = null;

            try
            {
                directory = directory ?? Path.GetTempPath();

                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                file = Path.Combine(directory, "tmp" + Guid.NewGuid().ToString("N") + extension);

                ErrorUtilities.VerifyThrow(!File.Exists(file), "Guid should be unique");

                File.WriteAllText(file, String.Empty);
            }
            catch (Exception ex)
            {
                if (ExceptionHandling.NotExpectedException(ex))
                {
                    throw;
                }

                throw new IOException(ResourceUtilities.FormatResourceString("Shared.FailedCreatingTempFile", ex.Message), ex);
            }

            return file;
        }
    }
}
