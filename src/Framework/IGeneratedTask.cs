﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;

#nullable disable

namespace Microsoft.Build.Framework
{
    /// <summary>
    /// An interface implemented by tasks that are generated by ITaskFactory instances.
    /// </summary>
    public interface IGeneratedTask : ITask
    {
        /// <summary>
        /// Sets a value on a property of this task instance.
        /// </summary>
        /// <param name="property">The property to set.</param>
        /// <param name="value">The value to set. The caller is responsible to type-coerce this value to match the property's <see cref="TaskPropertyInfo.PropertyType"/>.</param>
        /// <remarks>
        /// All exceptions from this method will be caught in the taskExecution host and logged as a fatal task error
        /// </remarks>
        [SuppressMessage("Microsoft.Naming", "CA1716:IdentifiersShouldNotMatchKeywords", MessageId = "Property", Justification = "Public API that has shipped")]
        void SetPropertyValue(TaskPropertyInfo property, object value);

        /// <summary>
        /// Gets the property value.
        /// </summary>
        /// <param name="property">The property to get.</param>
        /// <returns>
        /// The value of the property, the value's type will match the type given by <see cref="TaskPropertyInfo.PropertyType"/>.
        /// </returns>
        /// <remarks>
        /// MSBuild calls this method after executing the task to get output parameters.
        /// All exceptions from this method will be caught in the taskExecution host and logged as a fatal task error
        /// </remarks>
        [SuppressMessage("Microsoft.Naming", "CA1716:IdentifiersShouldNotMatchKeywords", MessageId = "Property", Justification = "Public API that has shipped")]
        object GetPropertyValue(TaskPropertyInfo property);
    }
}
