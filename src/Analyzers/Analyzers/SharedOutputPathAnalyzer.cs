﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using Microsoft.Build.Construction;
using Microsoft.Build.Experimental;

namespace Microsoft.Build.Analyzers.Analyzers;

// Some background on ids:
//  * https://github.com/dotnet/roslyn-analyzers/blob/main/src/Utilities/Compiler/DiagnosticCategoryAndIdRanges.txt
//  * https://github.com/dotnet/roslyn/issues/40351
//
// quick suggestion now - let's force external ids to start with 'X', for ours - avoid 'MSB'
//  maybe - BS - build styling; BA - build authoring; BE - build execution/environment; BC - build configuration

internal sealed class SharedOutputPathAnalyzer : BuildAnalyzer
{
    public static BuildAnalysisRule SupportedRule = new BuildAnalysisRule("BC0101", "ConflictingOutputPath",
        "Two projects should not share their OutputPath nor IntermediateOutputPath locations", "Configuration",
        "Projects {0} and {1} have conflicting output paths: {2}.",
        new BuildAnalyzerConfiguration() { Severity = BuildAnalysisResultSeverity.Warning, IsEnabled = true });

    public override string FriendlyName => "MSBuild.SharedOutputPathAnalyzer";

    public override ImmutableArray<BuildAnalysisRule> SupportedRules { get; } =[SupportedRule];

    public override void Initialize(ConfigurationContext configurationContext)
    {
        /* This is it - no custom configuration */
    }

    public override void RegisterActions(IBuildAnalyzerContext context)
    {
        context.RegisterEvaluatedPropertiesAction(EvaluatedPropertiesAction);
    }

    private readonly Dictionary<string, string> _projectsPerOutputPath = new(StringComparer.CurrentCultureIgnoreCase);
    private readonly HashSet<string> _projects = new(StringComparer.CurrentCultureIgnoreCase);

    private void EvaluatedPropertiesAction(EvaluatedPropertiesContext context)
    {
        if (!_projects.Add(context.ProjectFilePath))
        {
            return;
        }

        string? binPath, objPath;

        context.EvaluatedProperties.TryGetValue("OutputPath", out binPath);
        context.EvaluatedProperties.TryGetValue("IntermediateOutputPath", out objPath);

        string? absoluteBinPath = CheckAndAddFullOutputPath(binPath, context);
        if (
            !string.IsNullOrEmpty(objPath) && !string.IsNullOrEmpty(absoluteBinPath) &&
            !objPath.Equals(binPath, StringComparison.CurrentCultureIgnoreCase)
            && !objPath.Equals(absoluteBinPath, StringComparison.CurrentCultureIgnoreCase)
        )
        {
            CheckAndAddFullOutputPath(objPath, context);
        }
    }

    private string? CheckAndAddFullOutputPath(string? path, EvaluatedPropertiesContext context)
    {
        if (string.IsNullOrEmpty(path))
        {
            return path;
        }

        string projectPath = context.ProjectFilePath;

        if (!Path.IsPathRooted(path))
        {
            path = Path.Combine(Path.GetDirectoryName(projectPath)!, path);
        }

        if (_projectsPerOutputPath.TryGetValue(path!, out string? conflictingProject))
        {
            context.ReportResult(BuildAnalysisResult.Create(
                SupportedRule,
                // TODO: let's support transmitting locations of specific properties
                ElementLocation.EmptyLocation,
                Path.GetFileName(projectPath),
                Path.GetFileName(conflictingProject),
                path!));
        }
        else
        {
            _projectsPerOutputPath[path!] = projectPath;
        }

        return path;
    }
}
