﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace Microsoft.Build.UnitTests
{
    [TestClass]
    sealed public class CommandLineBuilderTest
    {
        /*
        * Method:   AppendSwitchSimple
        *
        * Just append a simple switch.
        */
        [TestMethod]
        public void AppendSwitchSimple()
        {
            CommandLineBuilder c = new CommandLineBuilder();
            c.AppendSwitch("/a");
            c.AppendSwitch("-b");
            Assert.AreEqual("/a -b", c.ToString());
        }

        /*
        * Method:   AppendSwitchWithStringParameter
        *
        * Append a switch that has a string parameter.
        */
        [TestMethod]
        public void AppendSwitchWithStringParameter()
        {
            CommandLineBuilder c = new CommandLineBuilder();
            c.AppendSwitchIfNotNull("/animal:", "dog");
            Assert.AreEqual("/animal:dog", c.ToString());
        }

        /*
        * Method:   AppendSwitchWithSpacesInParameter
        *
        * This should trigger implicit quoting.
        */
        [TestMethod]
        public void AppendSwitchWithSpacesInParameter()
        {
            CommandLineBuilder c = new CommandLineBuilder();
            c.AppendSwitchIfNotNull("/animal:", "dog and pony");
            Assert.AreEqual("/animal:\"dog and pony\"", c.ToString());
        }

        /// <summary>
        /// Test for AppendSwitchIfNotNull for the ITaskItem version
        /// </summary>
        [TestMethod]
        public void AppendSwitchWithSpacesInParameterTaskItem()
        {
            CommandLineBuilder c = new CommandLineBuilder();
            c.AppendSwitchIfNotNull("/animal:", (ITaskItem)new TaskItem("dog and pony"));
            Assert.AreEqual("/animal:\"dog and pony\"", c.ToString());
        }

        /*
        * Method:   AppendLiteralSwitchWithSpacesInParameter
        *
        * Implicit quoting should not happen.
        */
        [TestMethod]
        public void AppendLiteralSwitchWithSpacesInParameter()
        {
            CommandLineBuilder c = new CommandLineBuilder();
            c.AppendSwitchUnquotedIfNotNull("/animal:", "dog and pony");
            Assert.AreEqual("/animal:dog and pony", c.ToString());
        }

        /*
        * Method:   AppendTwoStringsEnsureNoSpace
        *
        * When appending two comma-delimted strings, there should be no space before the comma.
        */
        [TestMethod]
        public void AppendTwoStringsEnsureNoSpace()
        {
            CommandLineBuilder c = new CommandLineBuilder();
            c.AppendFileNamesIfNotNull(new string[] { "Form1.resx", @"built\Form1.resources" }, ",");

            // There shouldn't be a space before or after the comma
            // Tools like resgen require comma-delimited lists to be bumped up next to each other.
            Assert.AreEqual(@"Form1.resx,built\Form1.resources", c.ToString());
        }

        /*
        * Method:   AppendSourcesArray
        *
        * Append several sources files using JoinAppend
        */
        [TestMethod]
        public void AppendSourcesArray()
        {
            CommandLineBuilder c = new CommandLineBuilder();
            c.AppendFileNamesIfNotNull(new string[] { "Mercury.cs", "Venus.cs", "Earth.cs" }, " ");

            // Managed compilers use this function to append sources files.
            Assert.AreEqual(@"Mercury.cs Venus.cs Earth.cs", c.ToString());
        }

        /*
        * Method:   AppendSourcesArrayWithDashes
        *
        * Append several sources files starting with dashes using JoinAppend
        */
        [TestMethod]
        public void AppendSourcesArrayWithDashes()
        {
            CommandLineBuilder c = new CommandLineBuilder();
            c.AppendFileNamesIfNotNull(new string[] { "-Mercury.cs", "-Venus.cs", "-Earth.cs" }, " ");

            // Managed compilers use this function to append sources files.
            Assert.AreEqual(@".\-Mercury.cs .\-Venus.cs .\-Earth.cs", c.ToString());
        }

        /// <summary>
        /// Test AppendFileNamesIfNotNull, the ITaskItem version
        /// </summary>
        [TestMethod]
        public void AppendSourcesArrayWithDashesTaskItem()
        {
            CommandLineBuilder c = new CommandLineBuilder();
            c.AppendFileNamesIfNotNull(new TaskItem[] { new TaskItem("-Mercury.cs"), null, new TaskItem("Venus.cs"), new TaskItem("-Earth.cs") }, " ");

            // Managed compilers use this function to append sources files.
            Assert.AreEqual(@".\-Mercury.cs  Venus.cs .\-Earth.cs", c.ToString());
        }

        /*
        * Method:   JoinAppendEmpty
        *
        * Append append and empty array. Result should be NOP.
        */
        [TestMethod]
        public void JoinAppendEmpty()
        {
            CommandLineBuilder c = new CommandLineBuilder();
            c.AppendFileNamesIfNotNull(new string[] { "" }, " ");

            // Managed compilers use this function to append sources files.
            Assert.AreEqual(@"", c.ToString());
        }

        /*
        * Method:   JoinAppendNull
        *
        * Append append and empty array. Result should be NOP.
        */
        [TestMethod]
        public void JoinAppendNull()
        {
            CommandLineBuilder c = new CommandLineBuilder();
            c.AppendFileNamesIfNotNull((string[])null, " ");

            // Managed compilers use this function to append sources files.
            Assert.AreEqual(@"", c.ToString());
        }

        /// <summary>
        /// Append a switch with parameter array, quoting
        /// </summary>
        [TestMethod]
        public void AppendSwitchWithParameterArrayQuoting()
        {
            CommandLineBuilder c = new CommandLineBuilder();
            c.AppendSwitch("/something");
            c.AppendSwitchIfNotNull("/switch:", new string[] { "Mer cury.cs", "Ve nus.cs", "Ear th.cs" }, ",");

            // Managed compilers use this function to append sources files.
            Assert.AreEqual("/something /switch:\"Mer cury.cs\",\"Ve nus.cs\",\"Ear th.cs\"", c.ToString());
        }

        /// <summary>
        /// Append a switch with parameter array, quoting, ITaskItem version
        /// </summary>
        [TestMethod]
        public void AppendSwitchWithParameterArrayQuotingTaskItem()
        {
            CommandLineBuilder c = new CommandLineBuilder();
            c.AppendSwitch("/something");
            c.AppendSwitchIfNotNull("/switch:", new TaskItem[] { new TaskItem("Mer cury.cs"), null, new TaskItem("Ve nus.cs"), new TaskItem("Ear th.cs") }, ",");

            // Managed compilers use this function to append sources files.
            Assert.AreEqual("/something /switch:\"Mer cury.cs\",,\"Ve nus.cs\",\"Ear th.cs\"", c.ToString());
        }

        /// <summary>
        /// Append a switch with parameter array, no quoting
        /// </summary>
        [TestMethod]
        public void AppendSwitchWithParameterArrayNoQuoting()
        {
            CommandLineBuilder c = new CommandLineBuilder();
            c.AppendSwitch("/something");
            c.AppendSwitchUnquotedIfNotNull("/switch:", new string[] { "Mer cury.cs", "Ve nus.cs", "Ear th.cs" }, ",");

            // Managed compilers use this function to append sources files.
            Assert.AreEqual("/something /switch:Mer cury.cs,Ve nus.cs,Ear th.cs", c.ToString());
        }

        /// <summary>
        /// Append a switch with parameter array, no quoting, ITaskItem version
        /// </summary>
        [TestMethod]
        public void AppendSwitchWithParameterArrayNoQuotingTaskItem()
        {
            CommandLineBuilder c = new CommandLineBuilder();
            c.AppendSwitch("/something");
            c.AppendSwitchUnquotedIfNotNull("/switch:", new TaskItem[] { new TaskItem("Mer cury.cs"), null, new TaskItem("Ve nus.cs"), new TaskItem("Ear th.cs") }, ",");

            // Managed compilers use this function to append sources files.
            Assert.AreEqual("/something /switch:Mer cury.cs,,Ve nus.cs,Ear th.cs", c.ToString());
        }

        /// <summary>
        /// Appends a single file name
        /// </summary>
        [TestMethod]
        public void AppendSingleFileName()
        {
            CommandLineBuilder c = new CommandLineBuilder();
            c.AppendSwitch("/something");
            c.AppendFileNameIfNotNull("-Mercury.cs");
            c.AppendFileNameIfNotNull("Mercury.cs");
            c.AppendFileNameIfNotNull("Mer cury.cs");

            // Managed compilers use this function to append sources files.
            Assert.AreEqual("/something .\\-Mercury.cs Mercury.cs \"Mer cury.cs\"", c.ToString());
        }

        /// <summary>
        /// Appends a single file name, ITaskItem version
        /// </summary>
        [TestMethod]
        public void AppendSingleFileNameTaskItem()
        {
            CommandLineBuilder c = new CommandLineBuilder();
            c.AppendSwitch("/something");
            c.AppendFileNameIfNotNull((ITaskItem)new TaskItem("-Mercury.cs"));
            c.AppendFileNameIfNotNull((ITaskItem)new TaskItem("Mercury.cs"));
            c.AppendFileNameIfNotNull((ITaskItem)new TaskItem("Mer cury.cs"));

            // Managed compilers use this function to append sources files.
            Assert.AreEqual("/something .\\-Mercury.cs Mercury.cs \"Mer cury.cs\"", c.ToString());
        }

        /// <summary>
        /// Verify that we throw an exception correctly for the case where we don't have a switch name
        /// </summary>
        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void AppendSingleFileNameWithQuotes()
        {
            // Cannot have escaped quotes in a file name
            CommandLineBuilder c = new CommandLineBuilder();
            c.AppendFileNameIfNotNull("string with \"quotes\"");

            Assert.AreEqual("\"string with \\\"quotes\\\"\"", c.ToString());
        }

        /// <summary>
        /// Trigger escaping of literal quotes.
        /// </summary>
        [TestMethod]
        public void AppendSwitchWithLiteralQuotesInParameter()
        {
            CommandLineBuilder c = new CommandLineBuilder();
            c.AppendSwitchIfNotNull("/D", "LSYSTEM_COMPATIBLE_ASSEMBLY_NAME=L\"Microsoft.Windows.SystemCompatible\"");
            Assert.AreEqual("/D\"LSYSTEM_COMPATIBLE_ASSEMBLY_NAME=L\\\"Microsoft.Windows.SystemCompatible\\\"\"", c.ToString());
        }

        /// <summary>
        /// Trigger escaping of literal quotes.
        /// </summary>
        [TestMethod]
        public void AppendSwitchWithLiteralQuotesInParameter2()
        {
            CommandLineBuilder c = new CommandLineBuilder();
            c.AppendSwitchIfNotNull("/D", @"ASSEMBLY_KEY_FILE=""c:\\foo\\FinalKeyFile.snk""");
            Assert.AreEqual(@"/D""ASSEMBLY_KEY_FILE=\""c:\\foo\\FinalKeyFile.snk\""""", c.ToString());
        }

        /// <summary>
        /// Trigger escaping of literal quotes. This time, a double set of literal quotes.
        /// </summary>
        [TestMethod]
        public void AppendSwitchWithLiteralQuotesInParameter3()
        {
            CommandLineBuilder c = new CommandLineBuilder();
            c.AppendSwitchIfNotNull("/D", @"""A B"" and ""C""");
            Assert.AreEqual(@"/D""\""A B\"" and \""C\""""", c.ToString());
        }

        /// <summary>
        /// When a value contains a backslash, it doesn't normally need escaping.
        /// </summary>
        [TestMethod]
        public void AppendQuotableSwitchContainingBackslash()
        {
            CommandLineBuilder c = new CommandLineBuilder();
            c.AppendSwitchIfNotNull("/D", @"A \B");
            Assert.AreEqual(@"/D""A \B""", c.ToString());
        }

        /// <summary>
        /// Backslashes before quotes need escaping themselves.
        /// </summary>
        [TestMethod]
        public void AppendQuotableSwitchContainingBackslashBeforeLiteralQuote()
        {
            CommandLineBuilder c = new CommandLineBuilder();
            c.AppendSwitchIfNotNull("/D", @"A"" \""B");
            Assert.AreEqual(@"/D""A\"" \\\""B""", c.ToString());
        }

        /// <summary>
        /// Don't quote if not asked to
        /// </summary>
        [TestMethod]
        public void AppendSwitchUnquotedIfNotNull()
        {
            CommandLineBuilder c = new CommandLineBuilder();
            c.AppendSwitchUnquotedIfNotNull("/D", @"A"" \""B");
            Assert.AreEqual(@"/DA"" \""B", c.ToString());
        }

        /// <summary>
        /// When a value ends with a backslash, that certainly should be escaped if it's
        /// going to be quoted.
        /// </summary>
        [TestMethod]
        public void AppendQuotableSwitchEndingInBackslash()
        {
            CommandLineBuilder c = new CommandLineBuilder();
            c.AppendSwitchIfNotNull("/D", @"A B\");
            Assert.AreEqual(@"/D""A B\\""", c.ToString());
        }

        /// <summary>
        /// Backslashes don't need to be escaped if the string isn't going to get quoted.
        /// </summary>
        [TestMethod]
        public void AppendNonQuotableSwitchEndingInBackslash()
        {
            CommandLineBuilder c = new CommandLineBuilder();
            c.AppendSwitchIfNotNull("/D", @"AB\");
            Assert.AreEqual(@"/DAB\", c.ToString());
        }

        /// <summary>
        /// Quoting of hyphens
        /// </summary>
        [TestMethod]
        public void AppendQuotableSwitchWithHyphen()
        {
            CommandLineBuilder c = new CommandLineBuilder(/* do not quote hyphens*/);
            c.AppendSwitchIfNotNull("/D", @"foo-bar");
            Assert.AreEqual(@"/Dfoo-bar", c.ToString());
        }

        /// <summary>
        /// Quoting of hyphens 2
        /// </summary>
        [TestMethod]
        public void AppendQuotableSwitchWithHyphenQuoting()
        {
            CommandLineBuilder c = new CommandLineBuilder(true /* quote hyphens*/);
            c.AppendSwitchIfNotNull("/D", @"foo-bar");
            Assert.AreEqual(@"/D""foo-bar""", c.ToString());
        }

        /// <summary>
        /// Appends an ITaskItem item spec as a parameter
        /// </summary>
        [TestMethod]
        public void AppendSwitchTaskItem()
        {
            CommandLineBuilder c = new CommandLineBuilder(true);
            c.AppendSwitchIfNotNull("/D", new TaskItem(@"foo-bar"));
            Assert.AreEqual(@"/D""foo-bar""", c.ToString());
        }

        /// <summary>
        /// Appends an ITaskItem item spec as a parameter
        /// </summary>
        [TestMethod]
        public void AppendSwitchUnQuotedTaskItem()
        {
            CommandLineBuilder c = new CommandLineBuilder(true);
            c.AppendSwitchUnquotedIfNotNull("/D", new TaskItem(@"foo-bar"));
            Assert.AreEqual(@"/Dfoo-bar", c.ToString());
        }

        /// <summary>
        /// Odd number of literal quotes. This should trigger an exception, because command line parsers
        /// generally can't handle this case.
        /// </summary>
        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void AppendSwitchWithOddNumberOfLiteralQuotesInParameter()
        {
            CommandLineBuilder c = new CommandLineBuilder();
            c.AppendSwitchIfNotNull("/D", @"ASSEMBLY_KEY_FILE=""c:\\foo\\FinalKeyFile.snk");
        }

        internal class TestCommandLineBuilder : CommandLineBuilder
        {
            internal void TestVerifyThrow(string switchName, string parameter)
            {
                VerifyThrowNoEmbeddedDoubleQuotes(switchName, parameter);
            }

            protected override void VerifyThrowNoEmbeddedDoubleQuotes(string switchName, string parameter)
            {
                base.VerifyThrowNoEmbeddedDoubleQuotes(switchName, parameter);
            }
        }

        /// <summary>
        /// Test the else of VerifyThrowNOEmbeddedDouble quotes where the switch name is not empty or null
        /// </summary>
        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void TestVerifyThrowElse()
        {
            TestCommandLineBuilder c = new TestCommandLineBuilder();
            c.TestVerifyThrow("SuperSwitch", @"Parameter");
            c.TestVerifyThrow("SuperSwitch", @"Para""meter");
        }
    }
}
