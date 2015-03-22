﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//-----------------------------------------------------------------------
// </copyright>
// <summary>Tests the CopyOnReadEnumerable utility class</summary>
//-----------------------------------------------------------------------

using Microsoft.Build.Collections;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;

namespace Microsoft.Build.UnitTests.OM.Collections
{
    /// <summary>
    /// Tests for CopyOnReadEnumerable
    /// </summary>
    [TestClass]
    public class CopyOnReadEnumerable_Tests
    {
        /// <summary>
        /// Verify basic case
        /// </summary>
        [TestMethod]
        public void NonCloneableBackingCollection()
        {
            List<int> values = new List<int>(new int[] { 1, 2, 3 });

            CopyOnReadEnumerable<int> enumerable = new CopyOnReadEnumerable<int>(values, values);

            using (IEnumerator<int> enumerator = values.GetEnumerator())
            {
                foreach (int i in enumerable)
                {
                    enumerator.MoveNext();
                    Assert.AreEqual(i, enumerator.Current);
                }
            }
        }

        /// <summary>
        /// Verify cloning case
        /// </summary>
        [TestMethod]
        public void CloneableBackingCollection()
        {
            List<Cloneable> values = new List<Cloneable>(new Cloneable[] { new Cloneable(), new Cloneable(), new Cloneable() });

            CopyOnReadEnumerable<Cloneable> enumerable = new CopyOnReadEnumerable<Cloneable>(values, values);

            using (IEnumerator<Cloneable> enumerator = values.GetEnumerator())
            {
                foreach (Cloneable i in enumerable)
                {
                    enumerator.MoveNext();
                    Assert.IsFalse(Object.ReferenceEquals(i, enumerator.Current), "Enumerator copied references.");
                }
            }
        }

        /// <summary>
        /// A class used for testing cloneable backing collections.
        /// </summary>
        private class Cloneable : IDeepCloneable<Cloneable>
        {
            #region IDeepCloneable<Cloneable> Members

            /// <summary>
            /// Clones the object.
            /// </summary>
            /// <returns>The new instance.</returns>
            public Cloneable DeepClone()
            {
                return new Cloneable();
            }

            #endregion
        }
    }
}
