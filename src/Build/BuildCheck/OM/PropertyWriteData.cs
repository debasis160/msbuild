using Microsoft.Build.Experimental.BuildCheck;
using Microsoft.Build.Shared;

namespace Microsoft.Build.BuildCheck.Analyzers;

/// <summary>
/// Information about property being written to - either during evaluation phase
///  or as part of property definition within the target.
/// </summary>
internal class PropertyWriteData(
    string projectFilePath,
    string propertyName,
    IMsBuildElementLocation? elementLocation,
    bool isEmpty)
    : AnalysisData(projectFilePath)
{
    /// <summary>
    /// Name of the property that was written to.
    /// </summary>
    public string PropertyName { get; } = propertyName;

    /// <summary>
    /// Location of the property write.
    /// If the location is null, it means that the property doesn't come from xml, but rather other sources
    ///  (environment variable, global property, toolset properties etc.).
    /// </summary>
    public IMsBuildElementLocation? ElementLocation { get; } = elementLocation;

    /// <summary>
    /// Was any value written? (E.g. if we set propA with value propB, while propB is undefined - the isEmpty will be true).
    /// </summary>
    public bool IsEmpty { get; } = isEmpty;
}