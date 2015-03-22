﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//-----------------------------------------------------------------------
// </copyright>
// <summary>Unit tests for the TargetBuilder with a mock task builder.</summary>
//-----------------------------------------------------------------------

using Microsoft.Build.BackEnd;
using Microsoft.Build.Collections;
using Microsoft.Build.Evaluation;
using Microsoft.Build.Execution;
using Microsoft.Build.Framework;
using Microsoft.Build.Shared;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using ILoggingService = Microsoft.Build.BackEnd.Logging.ILoggingService;
using LegacyThreadingData = Microsoft.Build.Execution.LegacyThreadingData;
using NodeLoggingContext = Microsoft.Build.BackEnd.Logging.NodeLoggingContext;
using ProjectLoggingContext = Microsoft.Build.BackEnd.Logging.ProjectLoggingContext;

namespace Microsoft.Build.UnitTests.BackEnd
{
    /// <summary>
    /// This is the unit test for the TargetBuilder.  This particular test is confined to just using the
    /// actual TargetBuilder, and uses a mock TaskBuilder on which TargetBuilder depends.
    /// </summary>
    [TestClass]
    public class TargetBuilder_Tests : IRequestBuilderCallback
    {
        /// <summary>
        /// The component host.
        /// </summary>
        private MockHost _host;

        /// <summary>
        /// A mock logger for scenario tests.
        /// </summary>
        private MockLogger _mockLogger;

        /// <summary>
        /// The node request id counter
        /// </summary>
        private int _nodeRequestId;

        /// <summary>
        /// Callback used to receive exceptions from loggers.  Unused here.
        /// </summary>
        /// <param name="e">The exception</param>
        public void LoggingException(Exception e)
        {
        }

        /// <summary>
        /// Sets up to run tests.  Creates the host object.
        /// </summary>
        [TestInitialize]
        public void SetUp()
        {
            _nodeRequestId = 1;
            _host = new MockHost();
            _mockLogger = new MockLogger();
            _host.OnLoggingThreadException += this.LoggingException;
        }

        /// <summary>
        /// Executed after all tests are run.
        /// </summary>
        [TestCleanup]
        public void TearDown()
        {
            File.Delete("testProject.proj");
            _mockLogger = null;
            _host = null;
        }

        /// <summary>
        /// Runs the constructor.
        /// </summary>
        [TestMethod]
        public void TestConstructor()
        {
            TargetBuilder builder = new TargetBuilder();
        }

        /// <summary>
        /// Runs a "simple" build with no dependencies and no outputs.
        /// </summary>
        [TestMethod]
        public void TestSimpleBuild()
        {
            ProjectInstance project = CreateTestProject();

            // The Empty target has no inputs or outputs.
            TargetBuilder builder = (TargetBuilder)_host.GetComponent(BuildComponentType.TargetBuilder);
            IConfigCache cache = (IConfigCache)_host.GetComponent(BuildComponentType.ConfigCache);
            BuildRequestEntry entry = new BuildRequestEntry(CreateNewBuildRequest(1, new string[] { "Empty" }), cache[1]);

            BuildResult result = builder.BuildTargets(GetProjectLoggingContext(entry), entry, this, entry.Request.Targets.ToArray(), CreateStandardLookup(project), CancellationToken.None).Result;
            Assert.IsTrue(result.HasResultsForTarget("Empty"));
            Assert.AreEqual(TargetResultCode.Success, result["Empty"].ResultCode);
            Assert.AreEqual(0, result["Empty"].Items.Length);
        }

        /// <summary>
        /// Runs a build with a target which depends on one other target.
        /// </summary>
        [TestMethod]
        public void TestDependencyBuild()
        {
            ProjectInstance project = CreateTestProject();

            // The Baz project depends on the Bar target.  Both should succeed.
            TargetBuilder builder = (TargetBuilder)_host.GetComponent(BuildComponentType.TargetBuilder);
            IConfigCache cache = (IConfigCache)_host.GetComponent(BuildComponentType.ConfigCache);

            BuildRequestEntry entry = new BuildRequestEntry(CreateNewBuildRequest(1, new string[] { "Baz" }), cache[1]);
            BuildResult result = builder.BuildTargets(GetProjectLoggingContext(entry), entry, this, entry.Request.Targets.ToArray(), CreateStandardLookup(project), CancellationToken.None).Result;

            // The result returned from the builder includes only those for the specified targets.
            Assert.IsTrue(result.HasResultsForTarget("Baz"));
            Assert.IsFalse(result.HasResultsForTarget("Bar"));
            Assert.AreEqual(TargetResultCode.Success, result["Baz"].ResultCode);

            // The results cache should have ALL of the results.
            IResultsCache resultsCache = (IResultsCache)_host.GetComponent(BuildComponentType.ResultsCache);
            Assert.IsTrue(resultsCache.GetResultForRequest(entry.Request).HasResultsForTarget("Bar"));
            Assert.AreEqual(TargetResultCode.Success, resultsCache.GetResultForRequest(entry.Request)["Bar"].ResultCode);
        }

        /// <summary>
        /// Tests a project with a dependency which will be skipped because its up-to-date.
        /// </summary>
        [TestMethod]
        public void TestDependencyBuildWithSkip()
        {
            ProjectInstance project = CreateTestProject();

            // DepSkip depends on Skip (which skips) but should succeed itself.
            TargetBuilder builder = (TargetBuilder)_host.GetComponent(BuildComponentType.TargetBuilder);
            IConfigCache cache = (IConfigCache)_host.GetComponent(BuildComponentType.ConfigCache);
            BuildRequestEntry entry = new BuildRequestEntry(CreateNewBuildRequest(1, new string[] { "DepSkip" }), cache[1]);
            BuildResult result = builder.BuildTargets(GetProjectLoggingContext(entry), entry, this, entry.Request.Targets.ToArray(), CreateStandardLookup(project), CancellationToken.None).Result;
            Assert.IsTrue(result.HasResultsForTarget("DepSkip"));
            Assert.IsFalse(result.HasResultsForTarget("Skip"));
            Assert.AreEqual(TargetResultCode.Success, result["DepSkip"].ResultCode);

            IResultsCache resultsCache = (IResultsCache)_host.GetComponent(BuildComponentType.ResultsCache);
            Assert.IsTrue(resultsCache.GetResultForRequest(entry.Request).HasResultsForTarget("SkipCondition"));
            Assert.AreEqual(TargetResultCode.Skipped, resultsCache.GetResultForRequest(entry.Request)["SkipCondition"].ResultCode);
        }

        /// <summary>
        /// This test is currently ignored because the error tasks aren't implemented yet (due to needing the task builder.)
        /// </summary>
        [TestMethod]
        public void TestDependencyBuildWithError()
        {
            ProjectInstance project = CreateTestProject();

            // The DepError target builds Foo (which succeeds), Skip (which skips) and Error (which fails), and Baz2
            // Baz2 should not run since it came after Error.
            // Error tries to build Foo again as an error (which is already built) and Bar, which produces outputs.
            // DepError builds Baz as an error, which produces outputs
            TargetBuilder builder = (TargetBuilder)_host.GetComponent(BuildComponentType.TargetBuilder);
            MockTaskBuilder taskBuilder = (MockTaskBuilder)_host.GetComponent(BuildComponentType.TaskBuilder);
            taskBuilder.FailTaskNumber = 3; // Succeed on Foo's one task, and Error's first task, and fail the second.

            IConfigCache cache = (IConfigCache)_host.GetComponent(BuildComponentType.ConfigCache);
            BuildRequestEntry entry = new BuildRequestEntry(CreateNewBuildRequest(1, new string[] { "DepError" }), cache[1]);
            BuildResult result = builder.BuildTargets(GetProjectLoggingContext(entry), entry, this, entry.Request.Targets.ToArray(), CreateStandardLookup(project), CancellationToken.None).Result;
            Assert.IsTrue(result.HasResultsForTarget("DepError"));
            Assert.IsFalse(result.HasResultsForTarget("Foo"));
            Assert.IsFalse(result.HasResultsForTarget("Skip"));
            Assert.IsFalse(result.HasResultsForTarget("Error"));
            Assert.IsFalse(result.HasResultsForTarget("Baz2"));
            Assert.IsFalse(result.HasResultsForTarget("Bar"));
            Assert.IsFalse(result.HasResultsForTarget("Baz"));

            IResultsCache resultsCache = (IResultsCache)_host.GetComponent(BuildComponentType.ResultsCache);

            Assert.IsTrue(resultsCache.GetResultForRequest(entry.Request).HasResultsForTarget("Foo"));
            Assert.IsTrue(resultsCache.GetResultForRequest(entry.Request).HasResultsForTarget("Skip"));
            Assert.IsTrue(resultsCache.GetResultForRequest(entry.Request).HasResultsForTarget("Error"));
            Assert.IsFalse(resultsCache.GetResultForRequest(entry.Request).HasResultsForTarget("Baz2"));
            Assert.IsTrue(resultsCache.GetResultForRequest(entry.Request).HasResultsForTarget("Bar"));
            Assert.IsTrue(resultsCache.GetResultForRequest(entry.Request).HasResultsForTarget("Baz"));
            Assert.AreEqual(TargetResultCode.Failure, resultsCache.GetResultForRequest(entry.Request)["DepError"].ResultCode);
            Assert.AreEqual(TargetResultCode.Success, resultsCache.GetResultForRequest(entry.Request)["Foo"].ResultCode);
            Assert.AreEqual(TargetResultCode.Success, resultsCache.GetResultForRequest(entry.Request)["Skip"].ResultCode);
            Assert.AreEqual(TargetResultCode.Failure, resultsCache.GetResultForRequest(entry.Request)["Error"].ResultCode);
            Assert.AreEqual(TargetResultCode.Success, resultsCache.GetResultForRequest(entry.Request)["Bar"].ResultCode);
            Assert.AreEqual(TargetResultCode.Success, resultsCache.GetResultForRequest(entry.Request)["Baz"].ResultCode);
        }

        /// <summary>
        /// Ensure that skipped targets only infer outputs once
        /// </summary>
        [TestMethod]
        public void SkippedTargetsShouldOnlyInferOutputsOnce()
        {
            MockLogger logger = new MockLogger();

            string path = FileUtilities.GetTemporaryFile();

            Thread.Sleep(100);

            string content = String.Format
                (
@"
<Project ToolsVersion='msbuilddefaulttoolsversion' xmlns='http://schemas.microsoft.com/developer/msbuild/2003'>

  <Target Name='Build' DependsOnTargets='GFA;GFT;DFTA;GAFT'>
        <Message Text='Build: [@(Outs)]' />
  </Target>


  <Target Name='GFA' Inputs='{0}' Outputs='{0}'>
        <Message Text='GFA' />
        <CreateItem Include='GFA'>
        	<Output TaskParameter='Include' ItemName='Outs' />
        </CreateItem>
  </Target>
  <Target Name='GFT'  Inputs='{0}' Outputs='{0}'>
        <CreateItem Include='GFT'>
            <Output TaskParameter='Include' ItemName='Outs' />
        </CreateItem>
        <Message Text='GFT' />
  </Target>
  <Target Name='DFTA'  Inputs='{0}' Outputs='{0}'>
        <CreateItem Include='DFTA'>
            <Output TaskParameter='Include' ItemName='Outs' />
        </CreateItem>
        <Message Text='DFTA' />
  </Target>
  <Target Name='GAFT'  Inputs='{0}' Outputs='{0}' DependsOnTargets='DFTA'>
        <CreateItem Include='GAFT'>
            <Output TaskParameter='Include' ItemName='Outs' />
        </CreateItem>
        <Message Text='GAFT' />
  </Target>
</Project>
            ",
             path
             );

            Project p = new Project(XmlReader.Create(new StringReader(content)));
            p.Build(new string[] { "Build" }, new ILogger[] { logger });

            // There should be no duplicates in the list - if there are, then skipped targets are being inferred multiple times
            logger.AssertLogContains("[GFA;GFT;DFTA;GAFT]");

            File.Delete(path);
        }

        /// <summary>
        /// Test empty before targets
        /// </summary>
        [TestMethod]
        public void TestLegacyCallTarget()
        {
            string projectBody = @"
<Target Name='Build'>
    <CallTarget Targets='Foo;Goo'/>
</Target>

<Target Name='Foo' DependsOnTargets='Foo2'>
    <FooTarget/>
</Target>

<Target Name='Goo'>
    <GooTarget/>
</Target>

<Target Name='Foo2'>
    <Foo2Target/>
</Target>
";

            ProjectInstance project = CreateTestProject(projectBody);

            TargetBuilder builder = (TargetBuilder)_host.GetComponent(BuildComponentType.TargetBuilder);
            IConfigCache cache = (IConfigCache)_host.GetComponent(BuildComponentType.ConfigCache);
            BuildRequestEntry entry = new BuildRequestEntry(CreateNewBuildRequest(1, new string[] { "Build" }), cache[1]);

            BuildResult result = builder.BuildTargets(GetProjectLoggingContext(entry), entry, this, entry.Request.Targets.ToArray(), CreateStandardLookup(project), CancellationToken.None).Result;
            AssertTaskExecutionOrder(new string[] { "CallTarget", "Foo2Target", "FooTarget", "GooTarget" });
        }

        /// <summary>
        /// BeforeTargets specifies a missing target. Should not warn or error. 
        /// </summary>
        [TestMethod]
        public void TestBeforeTargetsMissing()
        {
            string content = @"
<Project DefaultTargets='t' xmlns=""http://schemas.microsoft.com/developer/msbuild/2003"">
    
    <Target Name='t' BeforeTargets='x'>
        <Message Text='[t]' />
    </Target>
</Project>
                ";

            MockLogger log = Helpers.BuildProjectWithNewOMExpectSuccess(content);

            log.AssertLogContains("[t]");
            log.AssertLogDoesntContain("MSB4057"); // missing target
            log.AssertNoErrors();
            log.AssertNoWarnings();
        }

        /// <summary>
        /// BeforeTargets specifies a missing target. Should not warn or error. 
        /// </summary>
        [TestMethod]
        public void TestBeforeTargetsMissingRunsOthers()
        {
            string content = @"
<Project DefaultTargets='a;c' xmlns=""http://schemas.microsoft.com/developer/msbuild/2003"">
    
    <Target Name='t' BeforeTargets='a;b;c'>
        <Message Text='[t]' />
    </Target>

    <Target Name='a'>
        <Message Text='[a]' />
    </Target>

    <Target Name='c'>
        <Message Text='[c]' />
    </Target>
   
</Project>
                ";

            MockLogger log = Helpers.BuildProjectWithNewOMExpectSuccess(content);

            log.AssertLogContains("[t]", "[a]", "[c]");
            log.AssertLogDoesntContain("MSB4057"); // missing target
            log.AssertNoErrors();
            log.AssertNoWarnings();
        }

        /// <summary>
        /// AfterTargets specifies a missing target. Should not warn or error.
        /// </summary>
        [TestMethod]
        public void TestAfterTargetsMissing()
        {
            string content = @"
<Project DefaultTargets='t' xmlns=""http://schemas.microsoft.com/developer/msbuild/2003"">
    
    <Target Name='t' AfterTargets='x'>
        <Message Text='[t]' />
    </Target>
</Project>
                ";

            MockLogger log = Helpers.BuildProjectWithNewOMExpectSuccess(content);

            log.AssertLogContains("[t]");
            log.AssertLogDoesntContain("MSB4057"); // missing target
            log.AssertNoErrors();
            log.AssertNoWarnings();
        }

        /// <summary>
        /// AfterTargets specifies a missing target. Should not warn or error. 
        /// </summary>
        [TestMethod]
        public void TestAfterTargetsMissingRunsOthers()
        {
            string content = @"
<Project DefaultTargets='a;c' xmlns=""http://schemas.microsoft.com/developer/msbuild/2003"">
    
    <Target Name='t' AfterTargets='a;b'>
        <Message Text='[t]' />
    </Target>

    <Target Name='t2' AfterTargets='b;c'>
        <Message Text='[t2]' />
    </Target>

    <Target Name='a'>
        <Message Text='[a]' />
    </Target>

    <Target Name='c'>
        <Message Text='[c]' />
    </Target>
   
</Project>
                ";

            MockLogger log = Helpers.BuildProjectWithNewOMExpectSuccess(content);

            log.AssertLogContains("[a]", "[t]", "[c]", "[t2]");
            log.AssertLogDoesntContain("MSB4057"); // missing target
            log.AssertNoErrors();
            log.AssertNoWarnings();
        }

        /// <summary>
        /// Test empty before targets
        /// </summary>
        [TestMethod]
        public void TestBeforeTargetsEmpty()
        {
            string projectBody = @"
<Target Name='Build'>
    <BuildTask/>
</Target>

<Target Name='Before' BeforeTargets=''>
    <BeforeTask/>
</Target>";

            ProjectInstance project = CreateTestProject(projectBody);

            TargetBuilder builder = (TargetBuilder)_host.GetComponent(BuildComponentType.TargetBuilder);
            IConfigCache cache = (IConfigCache)_host.GetComponent(BuildComponentType.ConfigCache);
            BuildRequestEntry entry = new BuildRequestEntry(CreateNewBuildRequest(1, new string[] { "Build" }), cache[1]);

            BuildResult result = builder.BuildTargets(GetProjectLoggingContext(entry), entry, this, entry.Request.Targets.ToArray(), CreateStandardLookup(project), CancellationToken.None).Result;
            AssertTaskExecutionOrder(new string[] { "BuildTask" });
        }

        /// <summary>
        /// Test single before targets
        /// </summary>
        [TestMethod]
        public void TestBeforeTargetsSingle()
        {
            string projectBody = @"
<Target Name='Build' Outputs='$(Test)'>
    <BuildTask/>
</Target>

<Target Name='Before' BeforeTargets='Build'>
    <BeforeTask/>
</Target>";

            ProjectInstance project = CreateTestProject(projectBody);

            TargetBuilder builder = (TargetBuilder)_host.GetComponent(BuildComponentType.TargetBuilder);
            IConfigCache cache = (IConfigCache)_host.GetComponent(BuildComponentType.ConfigCache);
            BuildRequestEntry entry = new BuildRequestEntry(CreateNewBuildRequest(1, new string[] { "Build" }), cache[1]);

            BuildResult result = builder.BuildTargets(GetProjectLoggingContext(entry), entry, this, entry.Request.Targets.ToArray(), CreateStandardLookup(project), CancellationToken.None).Result;
            AssertTaskExecutionOrder(new string[] { "BeforeTask", "BuildTask" });
        }

        /// <summary>
        /// Test single before targets on an escaped target
        /// </summary>
        [TestMethod]
        public void TestBeforeTargetsEscaped()
        {
            string projectBody = @"
<Target Name='Build;Me' Outputs='$(Test)'>
    <BuildTask/>
</Target>

<Target Name='Before' BeforeTargets='Build%3bMe'>
    <BeforeTask/>
</Target>";

            ProjectInstance project = CreateTestProject(projectBody);

            TargetBuilder builder = (TargetBuilder)_host.GetComponent(BuildComponentType.TargetBuilder);
            IConfigCache cache = (IConfigCache)_host.GetComponent(BuildComponentType.ConfigCache);
            BuildRequestEntry entry = new BuildRequestEntry(CreateNewBuildRequest(1, new string[] { "Build;Me" }), cache[1]);

            BuildResult result = builder.BuildTargets(GetProjectLoggingContext(entry), entry, this, entry.Request.Targets.ToArray(), CreateStandardLookup(project), CancellationToken.None).Result;
            AssertTaskExecutionOrder(new string[] { "BeforeTask", "BuildTask" });
        }

        /// <summary>
        /// Test single before targets
        /// </summary>
        [TestMethod]
        public void TestBeforeTargetsSingleWithError()
        {
            string projectBody = @"
<Target Name='Before' BeforeTargets='Build'>
    <BeforeTask/>
</Target>

<Target Name='Build'>
    <BuildTask/>
</Target>
";

            MockTaskBuilder taskBuilder = (MockTaskBuilder)_host.GetComponent(BuildComponentType.TaskBuilder);
            taskBuilder.FailTaskNumber = 2; // Succeed on BeforeTask, fail on BuildTask

            ProjectInstance project = CreateTestProject(projectBody);

            TargetBuilder builder = (TargetBuilder)_host.GetComponent(BuildComponentType.TargetBuilder);
            IConfigCache cache = (IConfigCache)_host.GetComponent(BuildComponentType.ConfigCache);
            BuildRequestEntry entry = new BuildRequestEntry(CreateNewBuildRequest(1, new string[] { "Build" }), cache[1]);

            BuildResult result = builder.BuildTargets(GetProjectLoggingContext(entry), entry, this, entry.Request.Targets.ToArray(), CreateStandardLookup(project), CancellationToken.None).Result;
            AssertTaskExecutionOrder(new string[] { "BeforeTask", "BuildTask" });
        }

        /// <summary>
        /// Test single before targets
        /// </summary>
        [TestMethod]
        public void TestBeforeTargetsSingleWithErrorAndParent()
        {
            string projectBody = @"
<Target Name='Before' BeforeTargets='Build'>
    <BeforeTask/>
</Target>

<Target Name='Build'>
    <BuildTask/>
    <OnError ExecuteTargets='ErrorTarget'/>
</Target>

<Target Name='ErrorTarget'>
    <Error/>
</Target>
";

            MockTaskBuilder taskBuilder = (MockTaskBuilder)_host.GetComponent(BuildComponentType.TaskBuilder);
            taskBuilder.FailTaskNumber = 2; // Succeed on BeforeTask, fail on BuildTask

            ProjectInstance project = CreateTestProject(projectBody);

            TargetBuilder builder = (TargetBuilder)_host.GetComponent(BuildComponentType.TargetBuilder);
            IConfigCache cache = (IConfigCache)_host.GetComponent(BuildComponentType.ConfigCache);
            BuildRequestEntry entry = new BuildRequestEntry(CreateNewBuildRequest(1, new string[] { "Build" }), cache[1]);

            BuildResult result = builder.BuildTargets(GetProjectLoggingContext(entry), entry, this, entry.Request.Targets.ToArray(), CreateStandardLookup(project), CancellationToken.None).Result;
            AssertTaskExecutionOrder(new string[] { "BeforeTask", "BuildTask", "Error" });
        }

        /// <summary>
        /// Test multiple before targets
        /// </summary>
        [TestMethod]
        public void TestBeforeTargetsWithTwoReferringToOne()
        {
            string projectBody = @"
<Target Name='Build' Outputs='$(Test)'>
    <BuildTask/>
</Target>

<Target Name='Before' BeforeTargets='Build'>
    <BeforeTask/>
</Target>


<Target Name='Before2' BeforeTargets='Build'>
    <BeforeTask2/>
</Target>
";

            ProjectInstance project = CreateTestProject(projectBody);

            TargetBuilder builder = (TargetBuilder)_host.GetComponent(BuildComponentType.TargetBuilder);
            IConfigCache cache = (IConfigCache)_host.GetComponent(BuildComponentType.ConfigCache);
            BuildRequestEntry entry = new BuildRequestEntry(CreateNewBuildRequest(1, new string[] { "Build" }), cache[1]);

            BuildResult result = builder.BuildTargets(GetProjectLoggingContext(entry), entry, this, entry.Request.Targets.ToArray(), CreateStandardLookup(project), CancellationToken.None).Result;
            AssertTaskExecutionOrder(new string[] { "BeforeTask", "BeforeTask2", "BuildTask" });
        }

        /// <summary>
        /// Test multiple before targets
        /// </summary>
        [TestMethod]
        public void TestBeforeTargetsWithOneReferringToTwo()
        {
            string projectBody = @"
<Target Name='Build' Outputs='$(Test)'>
    <BuildTask/>
</Target>

<Target Name='Foo' Outputs='$(Test)'>
    <FooTask/>
</Target>

<Target Name='Before' BeforeTargets='Build;Foo'>
    <BeforeTask/>
</Target>
";

            ProjectInstance project = CreateTestProject(projectBody);

            TargetBuilder builder = (TargetBuilder)_host.GetComponent(BuildComponentType.TargetBuilder);
            IConfigCache cache = (IConfigCache)_host.GetComponent(BuildComponentType.ConfigCache);
            BuildRequestEntry entry = new BuildRequestEntry(CreateNewBuildRequest(1, new string[] { "Foo" }), cache[1]);

            BuildResult result = builder.BuildTargets(GetProjectLoggingContext(entry), entry, this, entry.Request.Targets.ToArray(), CreateStandardLookup(project), CancellationToken.None).Result;
            AssertTaskExecutionOrder(new string[] { "BeforeTask", "FooTask" });
        }

        /// <summary>
        /// Test before target on a skipped target
        /// </summary>
        [TestMethod]
        public void TestBeforeTargetsSkip()
        {
            string projectBody = @"
<Target Name='Build' Condition=""'0'=='1'"">
    <BuildTask/>
</Target>

<Target Name='Before' BeforeTargets='Build'>
    <BeforeTask/>
</Target>";

            ProjectInstance project = CreateTestProject(projectBody);

            TargetBuilder builder = (TargetBuilder)_host.GetComponent(BuildComponentType.TargetBuilder);
            IConfigCache cache = (IConfigCache)_host.GetComponent(BuildComponentType.ConfigCache);
            BuildRequestEntry entry = new BuildRequestEntry(CreateNewBuildRequest(1, new string[] { "Build" }), cache[1]);

            BuildResult result = builder.BuildTargets(GetProjectLoggingContext(entry), entry, this, entry.Request.Targets.ToArray(), CreateStandardLookup(project), CancellationToken.None).Result;
            AssertTaskExecutionOrder(new string[] { "BeforeTask" });
        }

        /// <summary>
        /// Test before target on a skipped target
        /// </summary>
        [TestMethod]
        public void TestBeforeTargetsDependencyOrdering()
        {
            string projectBody = @"
<Target Name='Build' DependsOnTargets='BuildDep'>
    <BuildTask/>
</Target>

<Target Name='Before' DependsOnTargets='BeforeDep' BeforeTargets='Build'>
    <BeforeTask/>
</Target>

<Target Name='BuildDep'>
    <BuildDepTask/>
</Target>

<Target Name='BeforeDep'>
    <BeforeDepTask/>
</Target>

";

            ProjectInstance project = CreateTestProject(projectBody);

            TargetBuilder builder = (TargetBuilder)_host.GetComponent(BuildComponentType.TargetBuilder);
            IConfigCache cache = (IConfigCache)_host.GetComponent(BuildComponentType.ConfigCache);
            BuildRequestEntry entry = new BuildRequestEntry(CreateNewBuildRequest(1, new string[] { "Build" }), cache[1]);

            BuildResult result = builder.BuildTargets(GetProjectLoggingContext(entry), entry, this, entry.Request.Targets.ToArray(), CreateStandardLookup(project), CancellationToken.None).Result;
            AssertTaskExecutionOrder(new string[] { "BuildDepTask", "BeforeDepTask", "BeforeTask", "BuildTask" });
        }

        /// <summary>
        /// Test after target on a skipped target
        /// </summary>
        [TestMethod]
        public void TestAfterTargetsEmpty()
        {
            string projectBody = @"
<Target Name='Build'>
    <BuildTask/>
</Target>

<Target Name='After' AfterTargets=''>
    <AfterTask/>
</Target>";

            ProjectInstance project = CreateTestProject(projectBody);

            TargetBuilder builder = (TargetBuilder)_host.GetComponent(BuildComponentType.TargetBuilder);
            IConfigCache cache = (IConfigCache)_host.GetComponent(BuildComponentType.ConfigCache);
            BuildRequestEntry entry = new BuildRequestEntry(CreateNewBuildRequest(1, new string[] { "Build" }), cache[1]);

            BuildResult result = builder.BuildTargets(GetProjectLoggingContext(entry), entry, this, entry.Request.Targets.ToArray(), CreateStandardLookup(project), CancellationToken.None).Result;
            AssertTaskExecutionOrder(new string[] { "BuildTask" });
        }

        /// <summary>
        /// Test after target on a skipped target
        /// </summary>
        [TestMethod]
        public void TestAfterTargetsSkip()
        {
            string projectBody = @"
<Target Name='Build' Condition=""'0'=='1'"">
    <BuildTask/>
</Target>

<Target Name='After' AfterTargets='Build'>
    <AfterTask/>
</Target>";

            ProjectInstance project = CreateTestProject(projectBody);

            TargetBuilder builder = (TargetBuilder)_host.GetComponent(BuildComponentType.TargetBuilder);
            IConfigCache cache = (IConfigCache)_host.GetComponent(BuildComponentType.ConfigCache);
            BuildRequestEntry entry = new BuildRequestEntry(CreateNewBuildRequest(1, new string[] { "Build" }), cache[1]);

            BuildResult result = builder.BuildTargets(GetProjectLoggingContext(entry), entry, this, entry.Request.Targets.ToArray(), CreateStandardLookup(project), CancellationToken.None).Result;
            AssertTaskExecutionOrder(new string[] { "AfterTask" });
        }

        /// <summary>
        /// Test single before targets
        /// </summary>
        [TestMethod]
        public void TestAfterTargetsSingleWithError()
        {
            string projectBody = @"
<Target Name='Build'>
    <BuildTask/>
</Target>

<Target Name='After' AfterTargets='Build'>
    <AfterTask/>
</Target>";

            MockTaskBuilder taskBuilder = (MockTaskBuilder)_host.GetComponent(BuildComponentType.TaskBuilder);
            taskBuilder.FailTaskNumber = 1; // Fail on BuildTask

            ProjectInstance project = CreateTestProject(projectBody);

            TargetBuilder builder = (TargetBuilder)_host.GetComponent(BuildComponentType.TargetBuilder);
            IConfigCache cache = (IConfigCache)_host.GetComponent(BuildComponentType.ConfigCache);
            BuildRequestEntry entry = new BuildRequestEntry(CreateNewBuildRequest(1, new string[] { "Build" }), cache[1]);

            BuildResult result = builder.BuildTargets(GetProjectLoggingContext(entry), entry, this, entry.Request.Targets.ToArray(), CreateStandardLookup(project), CancellationToken.None).Result;
            AssertTaskExecutionOrder(new string[] { "BuildTask" });
        }

        /// <summary>
        /// Test single before targets
        /// </summary>
        [TestMethod]
        public void TestAfterTargetsSingleWithErrorAndParent()
        {
            string projectBody = @"
<Target Name='After' AfterTargets='Build'>
    <AfterTask/>
</Target>

<Target Name='Build'>
    <BuildTask/>
    <OnError ExecuteTargets='ErrorTarget'/>
</Target>

<Target Name='ErrorTarget'>
    <Error/>
</Target>

<Target Name='ErrorTarget2'>
    <Error2/>
</Target>

<Target Name='PostBuild' DependsOnTargets='Build'>
    <OnError ExecuteTargets='ErrorTarget2'/>
</Target>
";

            MockTaskBuilder taskBuilder = (MockTaskBuilder)_host.GetComponent(BuildComponentType.TaskBuilder);
            taskBuilder.FailTaskNumber = 2; // Succeed on BuildTask, fail on AfterTask

            ProjectInstance project = CreateTestProject(projectBody);

            TargetBuilder builder = (TargetBuilder)_host.GetComponent(BuildComponentType.TargetBuilder);
            IConfigCache cache = (IConfigCache)_host.GetComponent(BuildComponentType.ConfigCache);
            BuildRequestEntry entry = new BuildRequestEntry(CreateNewBuildRequest(1, new string[] { "PostBuild" }), cache[1]);

            BuildResult result = builder.BuildTargets(GetProjectLoggingContext(entry), entry, this, entry.Request.Targets.ToArray(), CreateStandardLookup(project), CancellationToken.None).Result;
            AssertTaskExecutionOrder(new string[] { "BuildTask", "AfterTask", "Error2" });
        }

        /// <summary>
        /// Test after target on a normal target
        /// </summary>
        [TestMethod]
        public void TestAfterTargetsSingle()
        {
            string projectBody = @"
<Target Name='Build'>
    <BuildTask/>
</Target>

<Target Name='After' AfterTargets='Build'>
    <AfterTask/>
</Target>";

            ProjectInstance project = CreateTestProject(projectBody);

            TargetBuilder builder = (TargetBuilder)_host.GetComponent(BuildComponentType.TargetBuilder);
            IConfigCache cache = (IConfigCache)_host.GetComponent(BuildComponentType.ConfigCache);
            BuildRequestEntry entry = new BuildRequestEntry(CreateNewBuildRequest(1, new string[] { "Build" }), cache[1]);

            BuildResult result = builder.BuildTargets(GetProjectLoggingContext(entry), entry, this, entry.Request.Targets.ToArray(), CreateStandardLookup(project), CancellationToken.None).Result;
            AssertTaskExecutionOrder(new string[] { "BuildTask", "AfterTask" });
        }

        /// <summary>
        /// Test after target on a target name which needs escaping
        /// </summary>
        [TestMethod]
        public void TestAfterTargetsEscaped()
        {
            string projectBody = @"
<Target Name='Build;Me'>
    <BuildTask/>
</Target>

<Target Name='After' AfterTargets='Build%3bMe'>
    <AfterTask/>
</Target>";

            ProjectInstance project = CreateTestProject(projectBody);

            TargetBuilder builder = (TargetBuilder)_host.GetComponent(BuildComponentType.TargetBuilder);
            IConfigCache cache = (IConfigCache)_host.GetComponent(BuildComponentType.ConfigCache);
            BuildRequestEntry entry = new BuildRequestEntry(CreateNewBuildRequest(1, new string[] { "Build;Me" }), cache[1]);

            BuildResult result = builder.BuildTargets(GetProjectLoggingContext(entry), entry, this, entry.Request.Targets.ToArray(), CreateStandardLookup(project), CancellationToken.None).Result;
            AssertTaskExecutionOrder(new string[] { "BuildTask", "AfterTask" });
        }

        /// <summary>
        /// Test after target on a skipped target
        /// </summary>
        [TestMethod]
        public void TestAfterTargetsWithTwoReferringToOne()
        {
            string projectBody = @"
<Target Name='Build'>
    <BuildTask/>
</Target>

<Target Name='After' AfterTargets='Build'>
    <AfterTask/>
</Target>

<Target Name='After2' AfterTargets='Build'>
    <AfterTask2/>
</Target>
";

            ProjectInstance project = CreateTestProject(projectBody);

            TargetBuilder builder = (TargetBuilder)_host.GetComponent(BuildComponentType.TargetBuilder);
            IConfigCache cache = (IConfigCache)_host.GetComponent(BuildComponentType.ConfigCache);
            BuildRequestEntry entry = new BuildRequestEntry(CreateNewBuildRequest(1, new string[] { "Build" }), cache[1]);

            BuildResult result = builder.BuildTargets(GetProjectLoggingContext(entry), entry, this, entry.Request.Targets.ToArray(), CreateStandardLookup(project), CancellationToken.None).Result;
            AssertTaskExecutionOrder(new string[] { "BuildTask", "AfterTask", "AfterTask2" });
        }

        /// <summary>
        /// Test after target on a skipped target
        /// </summary>
        [TestMethod]
        public void TestAfterTargetsWithOneReferringToTwo()
        {
            string projectBody = @"
<Target Name='Build'>
    <BuildTask/>
</Target>

<Target Name='Foo'>
    <FooTask/>
</Target>

<Target Name='After' AfterTargets='Build;Foo'>
    <AfterTask/>
</Target>
";

            ProjectInstance project = CreateTestProject(projectBody);

            TargetBuilder builder = (TargetBuilder)_host.GetComponent(BuildComponentType.TargetBuilder);
            IConfigCache cache = (IConfigCache)_host.GetComponent(BuildComponentType.ConfigCache);
            BuildRequestEntry entry = new BuildRequestEntry(CreateNewBuildRequest(1, new string[] { "Foo" }), cache[1]);

            BuildResult result = builder.BuildTargets(GetProjectLoggingContext(entry), entry, this, entry.Request.Targets.ToArray(), CreateStandardLookup(project), CancellationToken.None).Result;
            AssertTaskExecutionOrder(new string[] { "FooTask", "AfterTask" });
        }

        /// <summary>
        /// Test after target on a skipped target
        /// </summary>
        [TestMethod]
        public void TestAfterTargetsWithDependencyOrdering()
        {
            string projectBody = @"
<Target Name='Build' DependsOnTargets='BuildDep'>
    <BuildTask/>
</Target>

<Target Name='After' DependsOnTargets='AfterDep' AfterTargets='Build'>
    <AfterTask/>
</Target>

<Target Name='BuildDep'>
    <BuildDepTask/>
</Target>

<Target Name='AfterDep'>
    <AfterDepTask/>
</Target>
";

            ProjectInstance project = CreateTestProject(projectBody);

            TargetBuilder builder = (TargetBuilder)_host.GetComponent(BuildComponentType.TargetBuilder);
            IConfigCache cache = (IConfigCache)_host.GetComponent(BuildComponentType.ConfigCache);
            BuildRequestEntry entry = new BuildRequestEntry(CreateNewBuildRequest(1, new string[] { "Build" }), cache[1]);

            BuildResult result = builder.BuildTargets(GetProjectLoggingContext(entry), entry, this, entry.Request.Targets.ToArray(), CreateStandardLookup(project), CancellationToken.None).Result;
            AssertTaskExecutionOrder(new string[] { "BuildDepTask", "BuildTask", "AfterDepTask", "AfterTask" });
        }

        /// <summary>
        /// Test a complex ordering with depends, before and after targets
        /// </summary>
        [TestMethod]
        public void TestComplexOrdering()
        {
            string projectBody = @"
<Target Name='Build' DependsOnTargets='BuildDep'>
    <BuildTask/>
</Target>

<Target Name='Before' DependsOnTargets='BeforeDep' BeforeTargets='Build'>
    <BeforeTask/>
</Target>

<Target Name='After' DependsOnTargets='AfterDep' AfterTargets='Build'>
    <AfterTask/>
</Target>

<Target Name='BuildDep'>
    <BuildDepTask/>
</Target>

<Target Name='AfterDep' DependsOnTargets='AfterDepDep'>
    <AfterDepTask/>
</Target>

<Target Name='BeforeDep' DependsOnTargets='BeforeDepDep'>
    <BeforeDepTask/>
</Target>

<Target Name='BeforeDepDep'>
    <BeforeDepDepTask/>
</Target>

<Target Name='AfterDepDep'>
    <AfterDepDepTask/>
</Target>
";

            ProjectInstance project = CreateTestProject(projectBody);

            TargetBuilder builder = (TargetBuilder)_host.GetComponent(BuildComponentType.TargetBuilder);
            IConfigCache cache = (IConfigCache)_host.GetComponent(BuildComponentType.ConfigCache);
            BuildRequestEntry entry = new BuildRequestEntry(CreateNewBuildRequest(1, new string[] { "Build" }), cache[1]);

            BuildResult result = builder.BuildTargets(GetProjectLoggingContext(entry), entry, this, entry.Request.Targets.ToArray(), CreateStandardLookup(project), CancellationToken.None).Result;
            AssertTaskExecutionOrder(new string[] { "BuildDepTask", "BeforeDepDepTask", "BeforeDepTask", "BeforeTask", "BuildTask", "AfterDepDepTask", "AfterDepTask", "AfterTask" });
        }

        /// <summary>
        /// Test a complex ordering with depends, before and after targets
        /// </summary>
        [TestMethod]
        public void TestComplexOrdering2()
        {
            string projectBody = @"
<Target Name='BuildDep'>
    <BuildDepTask/>
</Target>

<Target Name='BeforeDepDep'>
    <BeforeDepDepTask/>
</Target>

<Target Name='BeforeBeforeDep' BeforeTargets='BeforeDep'>
    <BeforeBeforeDepTask/>
</Target>

<Target Name='AfterBeforeBeforeDep' AfterTargets='BeforeBeforeDep'>
    <AfterBeforeBeforeDepTask/>
</Target>

<Target Name='BeforeDep' DependsOnTargets='BeforeDepDep'>
    <BeforeDepTask/>
</Target>

<Target Name='Before' DependsOnTargets='BeforeDep' BeforeTargets='Build'>
    <BeforeTask/>
</Target>

<Target Name='AfterBeforeDepDep'>
    <AfterBeforeDepDepTask/>
</Target>

<Target Name='AfterBeforeDep' DependsOnTargets='AfterBeforeDepDep'>
    <AfterBeforeDepTask/>
</Target>

<Target Name='AfterBefore' DependsOnTargets='AfterBeforeDep' AfterTargets='Before'>
    <AfterBeforeTask/>
</Target>

<Target Name='Build' DependsOnTargets='BuildDep'>
    <BuildTask/>
</Target>

";

            ProjectInstance project = CreateTestProject(projectBody);

            TargetBuilder builder = (TargetBuilder)_host.GetComponent(BuildComponentType.TargetBuilder);
            IConfigCache cache = (IConfigCache)_host.GetComponent(BuildComponentType.ConfigCache);
            BuildRequestEntry entry = new BuildRequestEntry(CreateNewBuildRequest(1, new string[] { "Build" }), cache[1]);

            BuildResult result = builder.BuildTargets(GetProjectLoggingContext(entry), entry, this, entry.Request.Targets.ToArray(), CreateStandardLookup(project), CancellationToken.None).Result;
            AssertTaskExecutionOrder(new string[] { "BuildDepTask", "BeforeDepDepTask", "BeforeBeforeDepTask", "AfterBeforeBeforeDepTask", "BeforeDepTask", "BeforeTask", "AfterBeforeDepDepTask", "AfterBeforeDepTask", "AfterBeforeTask", "BuildTask" });
        }

        /// <summary>
        /// Test a complex ordering with depends, before and after targets
        /// </summary>
        [TestMethod]
        public void TestBeforeAndAfterWithErrorTargets()
        {
            string projectBody = @"


<Target Name='Build' >
    <BuildTask/>
    <OnError ExecuteTargets='ErrorTarget'/>
</Target>

<Target Name='ErrorTarget'>
    <ErrorTargetTask/>
</Target>

<Target Name='BeforeErrorTarget' BeforeTargets='ErrorTarget'>
    <BeforeErrorTargetTask/>
</Target>

<Target Name='AfterErrorTarget' AfterTargets='ErrorTarget'>
    <AfterErrorTargetTask/>
</Target>

";

            MockTaskBuilder taskBuilder = (MockTaskBuilder)_host.GetComponent(BuildComponentType.TaskBuilder);
            taskBuilder.FailTaskNumber = 1; // Fail on BuildTask

            ProjectInstance project = CreateTestProject(projectBody);

            TargetBuilder builder = (TargetBuilder)_host.GetComponent(BuildComponentType.TargetBuilder);
            IConfigCache cache = (IConfigCache)_host.GetComponent(BuildComponentType.ConfigCache);
            BuildRequestEntry entry = new BuildRequestEntry(CreateNewBuildRequest(1, new string[] { "Build" }), cache[1]);

            BuildResult result = builder.BuildTargets(GetProjectLoggingContext(entry), entry, this, entry.Request.Targets.ToArray(), CreateStandardLookup(project), CancellationToken.None).Result;
            AssertTaskExecutionOrder(new string[] { "BuildTask", "BeforeErrorTargetTask", "ErrorTargetTask", "AfterErrorTargetTask" });
        }

        /// <summary>
        /// Test after target on a skipped target
        /// </summary>
        [TestMethod]
        public void TestBeforeAndAfterOverrides()
        {
            string projectBody = @"

<Target Name='BuildDep'>
    <BuildDepTask/>
</Target>

<Target Name='Build' DependsOnTargets='BuildDep'>
    <BuildTask/>
</Target>

<Target Name='After' AfterTargets='Build'>
    <AfterTask/>
</Target>

<Target Name='After' AfterTargets='BuildDep'>
    <AfterTask/>
</Target>

<Target Name='Before' BeforeTargets='Build'>
    <BeforeTask/>
</Target>

<Target Name='Before' BeforeTargets='BuildDep'>
    <BeforeTask/>
</Target>

";

            ProjectInstance project = CreateTestProject(projectBody);

            TargetBuilder builder = (TargetBuilder)_host.GetComponent(BuildComponentType.TargetBuilder);
            IConfigCache cache = (IConfigCache)_host.GetComponent(BuildComponentType.ConfigCache);
            BuildRequestEntry entry = new BuildRequestEntry(CreateNewBuildRequest(1, new string[] { "Build" }), cache[1]);

            BuildResult result = builder.BuildTargets(GetProjectLoggingContext(entry), entry, this, entry.Request.Targets.ToArray(), CreateStandardLookup(project), CancellationToken.None).Result;
            AssertTaskExecutionOrder(new string[] { "BeforeTask", "BuildDepTask", "AfterTask", "BuildTask" });
        }

        /// <summary>
        /// Test that if before and after targets skip, the main target still runs (bug 476908)
        /// </summary>
        [TestMethod]
        public void TestSkippingBeforeAndAfterTargets()
        {
            string projectBody = @"
<Target Name='Build'>
    <BuildTask/>
</Target>

<Target Name='Before' BeforeTargets='Build' Condition=""'0'=='1'"">
    <BeforeTask/>
</Target>

<Target Name='After' AfterTargets='Build' Condition=""'0'=='1'"">
    <AfterTask/>
</Target>
";

            ProjectInstance project = CreateTestProject(projectBody);

            TargetBuilder builder = (TargetBuilder)_host.GetComponent(BuildComponentType.TargetBuilder);
            IConfigCache cache = (IConfigCache)_host.GetComponent(BuildComponentType.ConfigCache);
            BuildRequestEntry entry = new BuildRequestEntry(CreateNewBuildRequest(1, new string[] { "Build" }), cache[1]);

            BuildResult result = builder.BuildTargets(GetProjectLoggingContext(entry), entry, this, entry.Request.Targets.ToArray(), CreateStandardLookup(project), CancellationToken.None).Result;
            AssertTaskExecutionOrder(new string[] { "BuildTask" });
        }

        /// <summary>
        /// Tests that a circular dependency within a CallTarget call correctly propogates the failure.  Bug 502570.
        /// </summary>
        [TestMethod]
        public void TestCircularDependencyInCallTarget()
        {
            string projectContents = @"
<Project xmlns=""http://schemas.microsoft.com/developer/msbuild/2003"">
    <Target Name=""t1"">
        <CallTarget Targets=""t3""/>
    </Target>
    <Target Name=""t2"" DependsOnTargets=""t1"">
    </Target>
    <Target Name=""t3"" DependsOnTargets=""t2"">
    </Target>
</Project>
      ";
            StringReader reader = new StringReader(projectContents);
            Project project = new Project(new XmlTextReader(reader), null, null);
            bool success = project.Build(_mockLogger);
            Assert.IsFalse(success);
        }

        /// <summary>
        /// Tests that cancel with no entries after building does not fail.
        /// </summary>
        [TestMethod]
        public void TestCancelWithNoEntriesAfterBuild()
        {
            string projectBody = @"
<Target Name='Build'>
    <BuildTask/>
</Target>
";

            ProjectInstance project = CreateTestProject(projectBody);

            TargetBuilder builder = (TargetBuilder)_host.GetComponent(BuildComponentType.TargetBuilder);
            IConfigCache cache = (IConfigCache)_host.GetComponent(BuildComponentType.ConfigCache);
            BuildRequestEntry entry = new BuildRequestEntry(CreateNewBuildRequest(1, new string[] { "Build" }), cache[1]);
            using (CancellationTokenSource source = new CancellationTokenSource())
            {
                BuildResult result = builder.BuildTargets(GetProjectLoggingContext(entry), entry, this, entry.Request.Targets.ToArray(), CreateStandardLookup(project), source.Token).Result;
                AssertTaskExecutionOrder(new string[] { "BuildTask" });

                // This simply should not fail.
                source.Cancel();
            }
        }

        #region IRequestBuilderCallback Members

        /// <summary>
        /// We have to have this interface, but it won't be used in this test because we aren't doing MSBuild callbacks.
        /// </summary>
        /// <param name="projectFiles">N/A</param>
        /// <param name="properties">N/A</param>
        /// <param name="toolsVersions">N/A</param>
        /// <param name="targets">N/A</param>
        /// <param name="waitForResults">N/A</param>
        /// <returns>N/A</returns>
        Task<BuildResult[]> IRequestBuilderCallback.BuildProjects(string[] projectFiles, PropertyDictionary<ProjectPropertyInstance>[] properties, string[] toolsVersions, string[] targets, bool waitForResults)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Not implemented
        /// </summary>
        Task IRequestBuilderCallback.BlockOnTargetInProgress(int blockingRequestId, string blockingTarget)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Empty impl
        /// </summary>
        void IRequestBuilderCallback.Yield()
        {
        }

        /// <summary>
        /// Empty impl
        /// </summary>
        void IRequestBuilderCallback.Reacquire()
        {
        }

        /// <summary>
        /// Empty impl
        /// </summary>
        void IRequestBuilderCallback.EnterMSBuildCallbackState()
        {
        }

        /// <summary>
        /// Empty impl
        /// </summary>
        void IRequestBuilderCallback.ExitMSBuildCallbackState()
        {
        }

        #endregion

        /// <summary>
        /// Verifies the order in which tasks executed.
        /// </summary>
        private void AssertTaskExecutionOrder(string[] tasks)
        {
            MockTaskBuilder mockBuilder = (MockTaskBuilder)_host.GetComponent(BuildComponentType.TaskBuilder);

            Assert.AreEqual(tasks.Length, mockBuilder.ExecutedTasks.Count);

            int currentTask = 0;
            foreach (ProjectTaskInstance task in mockBuilder.ExecutedTasks)
            {
                Assert.IsTrue(String.Equals(task.Name, tasks[currentTask]));
                currentTask++;
            }
        }

        /// <summary>
        /// Creates a new build request
        /// </summary>
        private BuildRequest CreateNewBuildRequest(int configurationId, string[] targets)
        {
            return new BuildRequest(1 /* submissionId */, _nodeRequestId++, configurationId, targets, null, BuildEventContext.Invalid, null);
        }

        /// <summary>
        /// Creates a 'Lookup' used to deal with projects.
        /// </summary>
        /// <param name="project">The project for which to create the lookup</param>
        /// <returns>The lookup</returns>
        private Lookup CreateStandardLookup(ProjectInstance project)
        {
            Lookup lookup = new Lookup(new ItemDictionary<ProjectItemInstance>(project.Items), new PropertyDictionary<ProjectPropertyInstance>(project.Properties), null);
            return lookup;
        }

        /// <summary>
        /// Creates a test project.
        /// </summary>
        /// <returns>The project.</returns>
        private ProjectInstance CreateTestProject()
        {
            string projectBodyContents = @"
                    <ItemGroup>
                        <Compile Include='b.cs' />
                        <Compile Include='c.cs' />
                    </ItemGroup>

                    <ItemGroup>
                        <Reference Include='System' />
                    </ItemGroup>                    

                    <Target Name='Empty' />

                    <Target Name='Skip' Inputs='testProject.proj' Outputs='testProject.proj' />

                    <Target Name='SkipCondition' Condition=""'true' == 'false'"" />

                    <Target Name='Error' >
                        <ErrorTask1 ContinueOnError='True'/>                    
                        <ErrorTask2 ContinueOnError='False'/>  
                        <ErrorTask3 /> 
                        <OnError ExecuteTargets='Foo'/>                  
                        <OnError ExecuteTargets='Bar'/>                  
                    </Target>

                    <Target Name='DepError' DependsOnTargets='Foo;Skip;Error;Baz2'>
                        <OnError ExecuteTargets='Baz'/>                  
                    </Target>

                    <Target Name='Foo' Inputs='foo.cpp' Outputs='foo.o'>
                        <FooTask1/>
                    </Target>

                    <Target Name='Bar'>
                        <BarTask1/>
                    </Target>

                    <Target Name='Baz' DependsOnTargets='Bar'>
                        <BazTask1/>
                        <BazTask2/>
                    </Target>

                    <Target Name='Baz2' DependsOnTargets='Bar;Foo'>
                        <Baz2Task1/>
                        <Baz2Task2/>
                        <Baz2Task3/>
                    </Target>

                    <Target Name='DepSkip' DependsOnTargets='SkipCondition'>
                        <DepSkipTask1/>
                        <DepSkipTask2/>
                        <DepSkipTask3/>
                    </Target>

                    <Target Name='DepSkip2' DependsOnTargets='Skip' Inputs='testProject.proj' Outputs='testProject.proj'>
                        <DepSkipTask1/>
                        <DepSkipTask2/>
                        <DepSkipTask3/>
                    </Target>
                ";

            return CreateTestProject(projectBodyContents);
        }

        /// <summary>
        /// Creates a test project.
        /// </summary>
        private ProjectInstance CreateTestProject(string projectBodyContents)
        {
            return CreateTestProject(projectBodyContents, String.Empty, String.Empty);
        }

        /// <summary>
        /// Creates a test project.
        /// </summary>
        private ProjectInstance CreateTestProject(string projectBodyContents, string initialTargets, string defaultTargets)
        {
            string projectFileContents = String.Format("<Project ToolsVersion='msbuilddefaulttoolsversion' xmlns='http://schemas.microsoft.com/developer/msbuild/2003' InitialTargets='{0}' DefaultTargets='{1}'>{2}</Project>", initialTargets, defaultTargets, projectBodyContents);

            // retries to deal with occasional locking issues where the file can't be written to initially
            for (int retries = 0; retries < 5; retries++)
            {
                try
                {
                    File.Create("testProject.proj").Close();
                    break;
                }
                catch (Exception ex)
                {
                    if (retries < 4)
                    {
                        Console.WriteLine(ex.ToString());
                    }
                    else
                    {
                        // All the retries have failed. We will now fail with the 
                        // actual problem now instead of with some more difficult-to-understand 
                        // issue later. 
                        throw ex;
                    }
                }
            }

            IConfigCache cache = (IConfigCache)_host.GetComponent(BuildComponentType.ConfigCache);
            BuildRequestConfiguration config = new BuildRequestConfiguration(1, new BuildRequestData("testFile", new Dictionary<string, string>(), "3.5", new string[0], null), "2.0");
            Project project = new Project(XmlReader.Create(new StringReader(projectFileContents)));

            config.Project = project.CreateProjectInstance();
            cache.AddConfiguration(config);

            return config.Project;
        }

        /// <summary>
        /// Creates a project logging context.
        /// </summary>
        /// <param name="entry">The entry on which to base the logging context.</param>
        /// <returns>The context</returns>
        private ProjectLoggingContext GetProjectLoggingContext(BuildRequestEntry entry)
        {
            return new ProjectLoggingContext(new NodeLoggingContext(_host, 1, false), entry, null);
        }

        /// <summary>
        /// The mock component host object.
        /// </summary>
        private class MockHost : MockLoggingService, IBuildComponentHost, IBuildComponent
        {
            #region IBuildComponentHost Members

            /// <summary>
            /// The config cache
            /// </summary>
            private IConfigCache _configCache;

            /// <summary>
            /// The logging service
            /// </summary>
            private ILoggingService _loggingService;

            /// <summary>
            /// The results cache
            /// </summary>
            private IResultsCache _resultsCache;

            /// <summary>
            /// The request builder
            /// </summary>
            private IRequestBuilder _requestBuilder;

            /// <summary>
            /// The mock task builder
            /// </summary>
            private ITaskBuilder _taskBuilder;

            /// <summary>
            /// The target builder
            /// </summary>
            private ITargetBuilder _targetBuilder;

            /// <summary>
            /// The build parameters
            /// </summary>
            private BuildParameters _buildParameters;

            /// <summary>
            /// Retrieves the LegacyThreadingData associated with a particular component host
            /// </summary>
            private LegacyThreadingData _legacyThreadingData;

            /// <summary>
            /// Constructor
            /// </summary>
            public MockHost()
            {
                _buildParameters = new BuildParameters();
                _legacyThreadingData = new LegacyThreadingData();

                _configCache = new ConfigCache();
                ((IBuildComponent)_configCache).InitializeComponent(this);

                _loggingService = this;

                _resultsCache = new ResultsCache();
                ((IBuildComponent)_resultsCache).InitializeComponent(this);

                _requestBuilder = new RequestBuilder();
                ((IBuildComponent)_requestBuilder).InitializeComponent(this);

                _taskBuilder = new MockTaskBuilder();
                ((IBuildComponent)_taskBuilder).InitializeComponent(this);

                _targetBuilder = new TargetBuilder();
                ((IBuildComponent)_targetBuilder).InitializeComponent(this);
            }

            /// <summary>
            /// Returns the node logging service.  We don't distinguish here.
            /// </summary>
            /// <returns>The logging service.</returns>
            public ILoggingService LoggingService
            {
                get
                {
                    return _loggingService;
                }
            }

            /// <summary>
            /// Retrieves the LegacyThreadingData associated with a particular component host
            /// </summary>
            LegacyThreadingData IBuildComponentHost.LegacyThreadingData
            {
                get
                {
                    return _legacyThreadingData;
                }
            }

            /// <summary>
            /// Retrieves the name of thoe host.
            /// </summary>
            public string Name
            {
                get
                {
                    return "TargetBuilder_Tests.MockHost";
                }
            }

            /// <summary>
            /// Returns the build parameters.
            /// </summary>
            public BuildParameters BuildParameters
            {
                get
                {
                    return _buildParameters;
                }
            }

            /// <summary>
            /// Constructs and returns a component of the specified type.
            /// </summary>
            /// <param name="type">The type of component to return</param>
            /// <returns>The component</returns>
            public IBuildComponent GetComponent(BuildComponentType type)
            {
                switch (type)
                {
                    case BuildComponentType.ConfigCache:
                        return (IBuildComponent)_configCache;

                    case BuildComponentType.LoggingService:
                        return (IBuildComponent)_loggingService;

                    case BuildComponentType.ResultsCache:
                        return (IBuildComponent)_resultsCache;

                    case BuildComponentType.RequestBuilder:
                        return (IBuildComponent)_requestBuilder;

                    case BuildComponentType.TaskBuilder:
                        return (IBuildComponent)_taskBuilder;

                    case BuildComponentType.TargetBuilder:
                        return (IBuildComponent)_targetBuilder;

                    default:
                        throw new ArgumentException("Unexpected type " + type);
                }
            }

            /// <summary>
            /// Registers a component factory
            /// </summary>
            public void RegisterFactory(BuildComponentType type, BuildComponentFactoryDelegate factory)
            {
            }

            #endregion

            #region IBuildComponent Members

            /// <summary>
            /// Sets the component host
            /// </summary>
            /// <param name="host">The component host</param>
            public void InitializeComponent(IBuildComponentHost host)
            {
                throw new NotImplementedException();
            }

            /// <summary>
            /// Shuts down the component
            /// </summary>
            public void ShutdownComponent()
            {
                throw new NotImplementedException();
            }

            #endregion
        }
    }
}
