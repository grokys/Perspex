//===============================================================================================================
// System  : Sandcastle Tools Standard Presentation Styles
// File    : AvaloniaHtmlPresentationStyle.cs
// Author  : Eric Woodruff  (Eric@EWoodruff.us)
// Updated : 08/06/2022
// Note    : Copyright 2014-2022, Eric Woodruff, All rights reserved
//
// This file contains the presentation style definition for the Avalonia Html presentation style.
//
// This code is published under the Microsoft Public License (Ms-PL).  A copy of the license should be
// distributed with the code and can be found at the project website: https://GitHub.com/EWSoftware/SHFB.  This
// notice, the author's name, and all copyright notices must remain intact in all applications, documentation,
// and source files.
//
//    Date     Who  Comments
// ==============================================================================================================
// 03/16/2022  EFW  Created the code
//===============================================================================================================

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Avalonia.Sandcastle.PresentationStyles.Properties;
using Sandcastle.Core;
using Sandcastle.Core.PresentationStyle;

namespace Avalonia.Sandcastle.PresentationStyles.AvaloniaHtml
{
    /// <summary>
    /// This contains the definition for the Avalonia Html presentation style
    /// </summary>
    [PresentationStyleExport("AvaloniaHtml", "Avalonia Html", Version = AssemblyInfo.ProductVersion,
      Copyright = AssemblyInfo.Copyright, Description = "This is the default presentation style.  It generates " +
        "website output with a responsive layout.")]
    public sealed class AvaloniaHtmlPresentationStyle : PresentationStyleSettings
    {
        /// <inheritdoc />
        public override string Location => ComponentUtilities.AssemblyFolder(Assembly.GetExecutingAssembly());

        /// <summary>
        /// Constructor
        /// </summary>
        public AvaloniaHtmlPresentationStyle()
        {
            // The base path of the presentation style files relative to the assembly's location
            this.BasePath = "AvaloniaHtml";

            this.SupportedFormats = HelpFileFormats.Website;

            this.SupportsNamespaceGrouping = this.SupportsCodeSnippetGrouping = true;

            this.DocumentModelApplicator = new StandardDocumentModel();
            this.ApiTableOfContentsGenerator = new StandardApiTocGenerator();
            this.TopicTransformation = new AvaloniaHtmlTransformation(this);

            // If relative, these paths are relative to the base path
            this.BuildAssemblerConfiguration = @"Configuration\BuildAssembler.config";

            // Note that UNIX based web servers may be case-sensitive with regard to folder and filenames so
            // match the case of the folder and filenames in the literals to their actual casing on the file
            // system.
            this.ContentFiles.Add(new ContentFiles(this.SupportedFormats, @"css\*.*"));
            this.ContentFiles.Add(new ContentFiles(this.SupportedFormats, @"icons\*.*"));
            this.ContentFiles.Add(new ContentFiles(this.SupportedFormats, @"scripts\*.*"));
            this.ContentFiles.Add(new ContentFiles(this.SupportedFormats, @"webfonts\*.*"));
            this.ContentFiles.Add(new ContentFiles(HelpFileFormats.Website, null, @"RootWebsiteContent\*.*",
                String.Empty, new[] { ".aspx", ".htm", ".html", ".php" }));

            // Add the plug-in dependencies
            this.PlugInDependencies.Add(new PlugInDependency("Website Table of Contents Generator", null));
        }

        /// <inheritdoc />
        /// <remarks>This presentation style only uses the standard shared content</remarks>
        public override IEnumerable<string> ResourceItemFiles(string languageName)
        {
            string filePath = this.ResolvePath(@"..\Shared\Content"),
                fileSpec = "SharedContent_" + languageName + ".xml";

            if(!File.Exists(Path.Combine(filePath, fileSpec)))
                fileSpec = "SharedContent_en-US.xml";

            yield return Path.Combine(filePath, fileSpec);

            foreach(string f in this.AdditionalResourceItemsFiles)
                yield return f;
        }
    }
}
