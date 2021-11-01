﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.IO;
using Microsoft.Build.Shared;
using Shouldly;
using Xunit;

namespace Microsoft.Build.Framework.UnitTests
{
    public class FileClassifierTests
    {
        [Fact]
        public void Shared_ReturnsInstance()
        {
            FileClassifier.Shared.ShouldNotBeNull();
        }

        [Fact]
        public void IsNonModifiable_EvaluatesModifiability()
        {
            FileClassifier classifier = new();

            var volume = NativeMethodsShared.IsWindows ? @"X:\" : "/home/usr";
            classifier.RegisterImmutableDirectories($"{Path.Combine(volume,"Test1")};{Path.Combine(volume, "Test2")}");

            classifier.IsNonModifiable(Path.Combine(volume, "Test1", "File.ext")).ShouldBeTrue();
            classifier.IsNonModifiable(Path.Combine(volume, "Test2", "File.ext")).ShouldBeTrue();
            classifier.IsNonModifiable(Path.Combine(volume, "Test3", "File.ext")).ShouldBeFalse();
        }

        [Fact]
        public void IsNonModifiable_DuplicateNugetRegistry_EvaluatesModifiability()
        {
            FileClassifier classifier = new();

            var volume = NativeMethodsShared.IsWindows ? @"X:\" : "/home/usr";

            for (int i = 0; i < 3; ++i)
            {
                classifier.RegisterImmutableDirectories($"{Path.Combine(volume, "Test1")};{Path.Combine(volume, "Test2")}");
            }

            classifier.IsNonModifiable(Path.Combine(volume, "Test1", "File.ext")).ShouldBeTrue();
            classifier.IsNonModifiable(Path.Combine(volume, "Test2", "File.ext")).ShouldBeTrue();
            classifier.IsNonModifiable(Path.Combine(volume, "Test3", "File.ext")).ShouldBeFalse();
        }

        [Fact]
        public void IsNonModifiable_RespectsOSCaseSensitivity()
        {
            FileClassifier classifier = new();

            var volume = NativeMethodsShared.IsWindows ? @"X:\" : "/home/usr";
            classifier.RegisterImmutableDirectories($"{Path.Combine(volume, "Test1")}");

            if (NativeMethodsShared.IsLinux)
            {
                classifier.IsNonModifiable(Path.Combine(volume, "Test1", "File.ext")).ShouldBeTrue();
                classifier.IsNonModifiable(Path.Combine(volume, "test1", "File.ext")).ShouldBeFalse();
            }
            else
            {
                classifier.IsNonModifiable(Path.Combine(volume, "Test1", "File.ext")).ShouldBeTrue();
                classifier.IsNonModifiable(Path.Combine(volume, "test1", "File.ext")).ShouldBeTrue();
            }
        }

        [Fact]
        public void IsNonModifiable_DoesntThrowWhenPackageFoldersAreNotRegistered()
        {
            FileClassifier classifier = new();

            classifier.IsNonModifiable("X:\\Test3\\File.ext").ShouldBeFalse();
        }
    }
}
