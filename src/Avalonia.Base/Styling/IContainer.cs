using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Avalonia.Layout;
using Avalonia.Platform;

namespace Avalonia.Styling
{
    /// <summary>
    /// Defines the interface for a container
    /// </summary>
    internal interface IContainer
    {
        /// <summary>
        /// Gets or sets the <see cref="Avalonia.Layout.ContainerType"/> of the container.
        /// </summary>
        ContainerType ContainerType { get; set; }

        /// <summary>
        /// The name of the container.
        /// </summary>
        string? ContainerName { get; set; }

        /// <summary>
        /// The <see cref="VisualQueryProvider"/> of the container. This provides size information for the container query.
        /// </summary>
        VisualQueryProvider QueryProvider { get; }
    }
}
