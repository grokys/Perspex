//===============================================================================================================
// System  : Sandcastle Help File Builder
// File    : AssemblyInfo.cs
// Author  : Eric Woodruff  (Eric@EWoodruff.us)
// Updated : 04/08/2021
// Note    : Copyright 2014-2021, Eric Woodruff, All rights reserved
//
// Sandcastle Tools standard presentation styles assembly attributes.
//
// This code is published under the Microsoft Public License (Ms-PL).  A copy of the license should be
// distributed with the code and can be found at the project website: https://GitHub.com/EWSoftware/SHFB.  This
// notice, the author's name, and all copyright notices must remain intact in all applications, documentation,
// and source files.
//
//    Date     Who  Comments
// ==============================================================================================================
// 01/04/2014  EFW  Created the code
//===============================================================================================================

using System;
using System.Reflection;
using System.Resources;
using System.Runtime.InteropServices;
using Avalonia.Sandcastle.PresentationStyles.Properties;

//
// General Information about an assembly is controlled through the following set of attributes.  Change these
// attribute values to modify the information associated with an assembly.
//
[assembly: AssemblyTitle("Avalonia Sandcastle Tools - Avalonia Sandcastle Presentation Styles")]
[assembly: AssemblyDescription("This assembly contains the MEF components used to define the Avalonia specific " +
    "presentation styles.")]

[assembly: CLSCompliant(true)]

// General assembly information
[assembly: AssemblyProduct("Sandcastle Help File Builder and Tools")]
[assembly: AssemblyCompany("Microsoft Corporation/EWSoftware")]
[assembly: AssemblyCopyright(AssemblyInfo.Copyright)]
[assembly: AssemblyCulture("")]

#if DEBUG
[assembly: AssemblyConfiguration("Debug")]
#else
[assembly: AssemblyConfiguration("Release")]
#endif

// Not visible to COM
[assembly: ComVisible(false)]

// Resources contained within the assembly are English
[assembly: NeutralResourcesLanguage("en")]

// Version numbers.  See comments below.

// Certain assemblies may contain a specific version to maintain binary compatibility with a prior release
#if !ASSEMBLYSPECIFICVERSION
[assembly: AssemblyVersion(AssemblyInfo.StrongNameVersion)]
#endif

[assembly: AssemblyFileVersion(AssemblyInfo.FileVersion)]
[assembly: AssemblyInformationalVersion(AssemblyInfo.ProductVersion)]

// This defines constants that can be used by plug-ins and components in their metadata.
//
// All version numbers for an assembly consists of the following four values:
//
//      Year of release
//      Month of release
//      Day of release
//      Revision (typically zero unless multiple releases are made on the same day)
//
// This versioning scheme allows build component and plug-in developers to use the same major, minor, and build
// numbers as the Sandcastle tools to indicate with which version their components are compatible.
//
namespace Avalonia.Sandcastle.PresentationStyles.Properties;

internal static partial class AssemblyInfo
{
    // Common assembly strong name version - DO NOT CHANGE UNLESS NECESSARY.
    //
    // This is used to set the assembly version in the strong name.  This should remain unchanged to maintain
    // binary compatibility with prior releases.  It should only be changed if a breaking change is made that
    // requires assemblies that reference older versions to be recompiled against the newer version.
    // Should match current Avalonia major version
    public const string StrongNameVersion = "11.0.0.0";

    // Common assembly file version
    //
    // This is used to set the assembly file version.  This will change with each new release.  MSIs only
    // support a Major value between 0 and 255 so we drop the century from the year on this one.
    public const string FileVersion = "11.0.0.1";

    // Common product version
    //
    // This may contain additional text to indicate Alpha or Beta states.  The version number will always match
    // the file version above
    public const string ProductVersion = "11.0.0.1";

    // Assembly copyright information
    public const string Copyright = "Copyright \xA9 2006-2024, Microsoft Corporation, All Rights Reserved.\r\n" +
                                    "Portions Copyright \xA9 2006-2024, Eric Woodruff, All Rights Reserved.\r\n" +
                                    "And \xA9 2024, Avalonia UI, All Rights Reserved.";
}
