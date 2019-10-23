﻿using System.Collections.Generic;
using System.Diagnostics.Tracing;

namespace Microsoft.Build.Eventing
{

    //
    // This captures information of how various key methods of building with MSBuild ran.
    //
    // /OnlyProviders=*Microsoft-Build
    [EventSource(Name = "Microsoft-Build")]
    internal sealed class MSBuildEventSource : EventSource
    {

        // define the singleton instance of the event source
        public static MSBuildEventSource Log = new MSBuildEventSource();

        private MSBuildEventSource() { }

        #region Events

        /// <summary>
        /// Call this method to notify listeners of information relevant to collecting a set of items, mutating them in a specified way, and saving the results.
        /// </summary>
        /// <param name="itemType">The type of the item being mutated.</param>
        [Event(1)]
        public void ApplyLazyItemOperationsStart(string itemType)
        {
            WriteEvent(1, itemType);
        }

        /// <param name="itemType">The type of the item being mutated.</param>
        [Event(2)]
        public void ApplyLazyItemOperationsStop(string itemType)
        {
            WriteEvent(2, itemType);
        }

        /// <summary>
        /// Call this method to notify listeners of information relevant to the setup for a BuildManager to receive build requests.
        /// </summary>
        [Event(3)]
        public void BuildStart()
        {
            WriteEvent(3);
        }

        [Event(4)]
        public void BuildStop()
        {
            WriteEvent(4);
        }

        /// <summary>
        /// Call this method to notify listeners of information of how a project file built.
        /// <param name="projectPath">Filename of the project being built.</param>
        /// </summary>
        [Event(5)]
        public void BuildProjectStart(string projectPath)
        {
            WriteEvent(5, projectPath);
        }

        /// <param name="projectPath">Filename of the project being built.</param>
        /// <param name="targets">Names of the targets that built.</param>
        [Event(6)]
        public void BuildProjectStop(string projectPath, string[] targets)
        {
            WriteEvent(6, projectPath, targets);
        }

        [Event(7)]
        public void RarComputeClosureStart()
        {
            WriteEvent(7);
        }

        [Event(8)]
        public void RarComputeClosureStop()
        {
            WriteEvent(8);
        }

        /// <param name="condition">The condition being evaluated.</param>
        [Event(9)]
        public void EvaluateConditionStart(string condition)
        {
            WriteEvent(9, condition);
        }

        /// <param name="condition">The condition being evaluated.</param>
        /// <param name="result">The result of evaluating the condition.</param>
        [Event(10)]
        public void EvaluateConditionStop(string condition, bool result)
        {
            WriteEvent(10, condition, result);
        }

        /// <summary>
        /// Call this method to notify listeners of how the project data was evaluated.
        /// </summary>
        /// <param name="projectFile">Relevant information about where in the run of the progam it is.</param>
        [Event(11)]
        public void EvaluateStart(string projectFile)
        {
            WriteEvent(11, projectFile);
        }

        /// <param name="projectFile">Relevant information about where in the run of the progam it is.</param>
        [Event(12)]
        public void EvaluatePass0Start(string projectFile)
        {
            WriteEvent(12, projectFile);
        }

        /// <param name="projectFile">Relevant information about where in the run of the progam it is.</param>
        [Event(13)]
        public void EvaluatePass0Stop(string projectFile)
        {
            WriteEvent(13, projectFile);
        }

        /// <param name="projectFile">Relevant information about where in the run of the progam it is.</param>
        [Event(14)]
        public void EvaluatePass1Start(string projectFile)
        {
            WriteEvent(14, projectFile);
        }

        /// <param name="projectFile">Relevant information about where in the run of the progam it is.</param>
        [Event(15)]
        public void EvaluatePass1Stop(string projectFile)
        {
            WriteEvent(15, projectFile);
        }

        /// <param name="projectFile">Relevant information about where in the run of the progam it is.</param>
        [Event(16)]
        public void EvaluatePass2Start(string projectFile)
        {
            WriteEvent(16, projectFile);
        }

        /// <param name="projectFile">Relevant information about where in the run of the progam it is.</param>
        [Event(17)]
        public void EvaluatePass2Stop(string projectFile)
        {
            WriteEvent(17, projectFile);
        }

        /// <param name="projectFile">Relevant information about where in the run of the progam it is.</param>
        [Event(18)]
        public void EvaluatePass3Start(string projectFile)
        {
            WriteEvent(18, projectFile);
        }

        /// <param name="projectFile">Relevant information about where in the run of the progam it is.</param>
        [Event(19)]
        public void EvaluatePass3Stop(string projectFile)
        {
            WriteEvent(19, projectFile);
        }

        /// <param name="projectFile">Relevant information about where in the run of the progam it is.</param>
        [Event(20)]
        public void EvaluatePass4Start(string projectFile)
        {
            WriteEvent(20, projectFile);
        }

        /// <param name="projectFile">Relevant information about where in the run of the progam it is.</param>
        [Event(21)]
        public void EvaluatePass4Stop(string projectFile)
        {
            WriteEvent(21, projectFile);
        }

        /// <param name="projectFile">Relevant information about where in the run of the progam it is.</param>
        [Event(22)]
        public void EvaluatePass5Start(string projectFile)
        {
            WriteEvent(22, projectFile);
        }

        /// <param name="projectFile">Relevant information about where in the run of the progam it is.</param>
        [Event(23)]
        public void EvaluatePass5Stop(string projectFile)
        {
            WriteEvent(23, projectFile);
        }

        /// <param name="projectFile">Relevant information about where in the run of the progam it is.</param>
        [Event(24)]
        public void EvaluateStop(string projectFile)
        {
            WriteEvent(24, projectFile);
        }

        [Event(25)]
        public void GenerateResourceOverallStart()
        {
            WriteEvent(25);
        }

        [Event(26)]
        public void GenerateResourceOverallStop()
        {
            WriteEvent(26);
        }

        [Event(27)]
        public void RarOverallStart()
        {
            WriteEvent(27);
        }

        [Event(28)]
        public void RarOverallStop()
        {
            WriteEvent(28);
        }

        /// <summary>
        /// Call this method to notify listeners of information relevant to identifying a list of files that correspond to an item with a wildcard.
        /// </summary>
        /// <param name="rootDirectory">Source of files to glob.</param>
        /// <param name="glob">Pattern, possibly with wildcard(s) to be expanded.</param>
        /// <param name="excludedPatterns">Patterns not to expand.</param>
        [Event(41)]
        public void ExpandGlobStart(string rootDirectory, string glob, ISet<string> excludedPatterns)
        {
            WriteEvent(41, rootDirectory, glob, excludedPatterns);
        }

        /// <param name="rootDirectory">Source of files to glob.</param>
        /// <param name="glob">Pattern, possibly with wildcard(s) to be expanded.</param>
        /// <param name="excludedPatterns">Patterns not to expand.</param>
        [Event(42)]
        public void ExpandGlobStop(string rootDirectory, string glob, ISet<string> excludedPatterns)
        {
            WriteEvent(42, rootDirectory, glob, excludedPatterns);
        }

        /// <summary>
        /// Call this method to notify listeners of timing related to loading an XmlDocumentWithLocation from a path.
        /// <param name="fullPath">Path to the document to load.</param>
        /// </summary>
        [Event(29)]
        public void LoadDocumentStart(string fullPath)
        {
            WriteEvent(29, fullPath);
        }

        /// <param name="fullPath">Path to the document to load.</param>
        [Event(30)]
        public void LoadDocumentStop(string fullPath)
        {
            WriteEvent(30, fullPath);
        }

        [Event(31)]
        public void RarLogResultsStart()
        {
            WriteEvent(31);
        }

        [Event(32)]
        public void RarLogResultsStop()
        {
            WriteEvent(32);
        }

        /// <summary>
        /// Call this method to notify listeners of profiling for the function that parses an XML document into a ProjectRootElement.
        /// </summary>
        /// <param name="projectFileName">Relevant information about where in the run of the progam it is.</param>
        [Event(33)]
        public void ParseStart(string projectFileName)
        {

            WriteEvent(33, projectFileName);
        }

        /// <param name="projectFileName">Relevant information about where in the run of the progam it is.</param>
        [Event(34)]
        public void ParseStop(string projectFileName)
        {
            WriteEvent(34, projectFileName);
        }

        /// <summary>
        /// Call this method to notify listeners of profiling for the method that removes blacklisted references from the reference table. It puts primary and dependency references in invalid file lists.
        /// </summary>
        [Event(35)]
        public void RarRemoveReferencesMarkedForExclusionStart()
        {
            WriteEvent(35);
        }

        [Event(36)]
        public void RarRemoveReferencesMarkedForExclusionStop()
        {
            WriteEvent(36);
        }

        /// <param name="fullPath">File name of the project to build.</param>
        [Event(37)]
        public void RequestThreadProcStart(string fullPath)
        {
            WriteEvent(37, fullPath);
        }

        /// <param name="fullPath">File name of the project to build.</param>
        [Event(38)]
        public void RequestThreadProcStop(string fullPath)
        {
            WriteEvent(38, fullPath);
        }

        /// <param name="fileLocation">Project file's location.</param>
        [Event(39)]
        public void SaveStart(string fileLocation)
        {
            WriteEvent(39, fileLocation);
        }

        /// <param name="fileLocation">Project file's location.</param>
        [Event(40)]
        public void SaveStop(string fileLocation)
        {
            WriteEvent(40, fileLocation);
        }

        /// <param name="targetName"/>The name of the target being executed.</param>
        [Event(43)]
        public void TargetStart(string targetName)
        {
            WriteEvent(43, targetName);
        }

        /// <param name="targetName">The name of the target being executed.</param>
        [Event(44)]
        public void TargetStop(string targetName)
        {
            WriteEvent(44, targetName);
        }

        /// <summary>
        /// Call this method to notify listeners of the start of a build as called from the command line.
        /// </summary>
        [Event(45)]
        public void MSBuildExeStart()
        {
            WriteEvent(45);
        }

        [Event(46)]
        public void MSBuildExeStop()
        {
            WriteEvent(46);
        }

        #endregion
    }
}
