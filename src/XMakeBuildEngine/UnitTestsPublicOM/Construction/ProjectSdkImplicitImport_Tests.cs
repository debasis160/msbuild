// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//-----------------------------------------------------------------------

using System;
using System.IO;
using System.Xml;
using Microsoft.Build.Construction;
using Microsoft.Build.Evaluation;
using Microsoft.Build.Exceptions;
using Microsoft.Build.Shared;
using Xunit;

namespace Microsoft.Build.UnitTests.OM.Construction
{
    /// <summary>
    /// Tests for the ProjectImportElement class when imports are implicit through an Sdk specification.
    /// </summary>
    public class ProjectSdkImplicitImport_Tests : IDisposable
    {
        private const string SdkName = "MSBuildUnitTestSdk";
        private readonly string _testSdkRoot;
        private readonly string _testSdkDirectory;
        private readonly string _sdkPropsPath;
        private readonly string _sdkTargetsPath;

        public ProjectSdkImplicitImport_Tests()
        {
            _testSdkRoot = Path.Combine(ObjectModelHelpers.TempProjectDir, Guid.NewGuid().ToString("N"));
            _testSdkDirectory = Path.Combine(_testSdkRoot, SdkName, "Sdk");
            _sdkPropsPath = Path.Combine(_testSdkDirectory, "Sdk.props");
            _sdkTargetsPath = Path.Combine(_testSdkDirectory, "Sdk.targets");

            Directory.CreateDirectory(_testSdkDirectory);
        }

        [Fact]
        public void SdkImportsAreInImportList()
        {
            File.WriteAllText(_sdkPropsPath, "<Project><PropertyGroup><InitialImportProperty>Hello</InitialImportProperty></PropertyGroup></Project>");
            File.WriteAllText(_sdkTargetsPath, "<Project><PropertyGroup><FinalImportProperty>World</FinalImportProperty></PropertyGroup></Project>");

            using (new Helpers.TemporaryEnvironment("MSBuildSDKsPath", _testSdkRoot))
            {
                string content = $@"
                    <Project Sdk=""{SdkName}"">
                        <PropertyGroup>
                            <UsedToTestIfImplicitImportsAreInTheCorrectLocation>null</UsedToTestIfImplicitImportsAreInTheCorrectLocation>
                        </PropertyGroup>
                    </Project>";

                ProjectRootElement projectRootElement = ProjectRootElement.Create(XmlReader.Create(new StringReader(content)));

                Project project = new Project(projectRootElement);

                // The XML representation of the project should indicate there are no imports
                Assert.Equal(0, projectRootElement.Imports.Count);

                // The project representation should have imports
                Assert.Equal(2, project.Imports.Count);

                ResolvedImport initialResolvedImport = project.Imports[0];
                Assert.Equal(_sdkPropsPath, initialResolvedImport.ImportedProject.FullPath);


                ResolvedImport finalResolvedImport = project.Imports[1];
                Assert.Equal(_sdkTargetsPath, finalResolvedImport.ImportedProject.FullPath);

                ProjectProperty initialImportProperty = project.GetProperty("InitialImportProperty");
                Assert.Equal(_sdkPropsPath, initialImportProperty.Xml.ContainingProject.FullPath);
                Assert.True(initialImportProperty.IsImported);

                ProjectProperty finalImportProperty = project.GetProperty("FinalImportProperty");
                Assert.Equal(_sdkTargetsPath, finalImportProperty.Xml.ContainingProject.FullPath);
                Assert.True(finalImportProperty.IsImported);

                // TODO: Check the location of the import, maybe it should point to the location of the SDK attribute?
            }
        }

        [Fact]
        public void ProjectWithSdkImportsIsCloneable()
        {
            File.WriteAllText(_sdkPropsPath, "<Project />");
            File.WriteAllText(_sdkTargetsPath, "<Project />");

            using (new Helpers.TemporaryEnvironment("MSBuildSDKsPath", _testSdkRoot))
            {
                // Based on the new-console-project CLI template (but not matching exactly
                // should not be a deal-breaker).
                string content = @"<Project Sdk=""Microsoft.NET.Sdk"" ToolsVersion=""15.0"">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>netcoreapp1.0</TargetFramework>
  </PropertyGroup>

  <ItemGroup>
    <Compile Include=""**\*.cs"" />
    <EmbeddedResource Include=""**\*.resx"" />
  </ItemGroup>

  <ItemGroup>
    <PackageReference Include=""Microsoft.NETCore.App"" Version=""1.0.1"" />
  </ItemGroup>

</Project>";

                ProjectRootElement project = ProjectRootElement.Create(XmlReader.Create(new StringReader(content)));

                project.DeepClone();
            }
        }

        [Fact]
        public void ProjectWithSdkImportsIsRemoveable()
        {
            File.WriteAllText(_sdkPropsPath, "<Project />");
            File.WriteAllText(_sdkTargetsPath, "<Project />");

            using (new Helpers.TemporaryEnvironment("MSBuildSDKsPath", _testSdkRoot))
            {
                // Based on the new-console-project CLI template (but not matching exactly
                // should not be a deal-breaker).
                string content = @"<Project Sdk=""Microsoft.NET.Sdk"" ToolsVersion=""15.0"">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>netcoreapp1.0</TargetFramework>
  </PropertyGroup>

  <ItemGroup>
    <Compile Include=""**\*.cs"" />
    <EmbeddedResource Include=""**\*.resx"" />
  </ItemGroup>

  <ItemGroup>
    <PackageReference Include=""Microsoft.NETCore.App"" Version=""1.0.1"" />
  </ItemGroup>

</Project>";

                ProjectRootElement project = ProjectRootElement.Create(XmlReader.Create(new StringReader(content)));
                ProjectRootElement clone = ProjectRootElement.Create(XmlReader.Create(new StringReader(content)));

                clone.DeepCopyFrom(project);

                clone.RemoveAllChildren();
            }
        }

        /// <summary>
        /// Verifies that an error occurs when an SDK name is not in the correct format.
        /// </summary>
        [Fact]
        public void ProjectWithInvalidSdkName()
        {
            const string invalidSdkName = "SdkWithExtra/Slash/1.0.0";

            InvalidProjectFileException exception = Assert.Throws<InvalidProjectFileException>(() =>
            {
                using (new Helpers.TemporaryEnvironment("MSBuildSDKsPath", _testSdkRoot))
                {
                    string content = $@"
                    <Project Sdk=""{invalidSdkName}"">
                        <PropertyGroup>
                            <UsedToTestIfImplicitImportsAreInTheCorrectLocation>null</UsedToTestIfImplicitImportsAreInTheCorrectLocation>
                        </PropertyGroup>
                    </Project>";

                    Project project = new Project(ProjectRootElement.Create(XmlReader.Create(new StringReader(content))));
                }
            });
            
            Assert.Equal("MSB4229", exception.ErrorCode);
        }

        /// <summary>
        /// Verifies that an empty SDK attribute works and nothing is imported.
        /// </summary>
        [Fact]
        public void ProjectWithEmptySdkName()
        {
            using (new Helpers.TemporaryEnvironment("MSBuildSDKsPath", _testSdkRoot))
            {
                string content = @"
                    <Project Sdk="""">
                        <PropertyGroup>
                            <UsedToTestIfImplicitImportsAreInTheCorrectLocation>null</UsedToTestIfImplicitImportsAreInTheCorrectLocation>
                        </PropertyGroup>
                    </Project>";

                Project project = new Project(ProjectRootElement.Create(XmlReader.Create(new StringReader(content))));

                Assert.Equal(0, project.Imports.Count);
            }
        }

        /// <summary>
        /// Verifies that an error occurs when one or more SDK names are empty.
        /// </summary>
        [Fact]
        public void ProjectWithEmptySdkNameInValidList()
        {
            const string invalidSdkName = "foo;  ;bar";

            InvalidProjectFileException exception = Assert.Throws<InvalidProjectFileException>(() =>
            {
                using (new Helpers.TemporaryEnvironment("MSBuildSDKsPath", _testSdkRoot))
                {
                    string content = $@"
                    <Project Sdk=""{invalidSdkName}"">
                        <PropertyGroup>
                            <UsedToTestIfImplicitImportsAreInTheCorrectLocation>null</UsedToTestIfImplicitImportsAreInTheCorrectLocation>
                        </PropertyGroup>
                    </Project>";

                    Project project = new Project(ProjectRootElement.Create(XmlReader.Create(new StringReader(content))));
                }
            });

            Assert.Equal("MSB4229", exception.ErrorCode);
        }

        public void Dispose()
        {
            if (Directory.Exists(_testSdkDirectory))
            {
                FileUtilities.DeleteWithoutTrailingBackslash(_testSdkDirectory, true);
            }
        }
    }
}
