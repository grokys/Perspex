using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using Sandcastle.Core.PresentationStyle.Transformation;
using Sandcastle.Core.PresentationStyle.Transformation.Elements;
using SandcastleBuilder.Utils.BuildComponent;
using SandcastleBuilder.Utils.BuildEngine;

// Search for "TODO" to find changes that you need to make to this plug-in template.

namespace SpecialAttributeRendererPlugin
{
    /// <summary>
    /// TODO: Set your plug-in's unique ID and description in the export attribute below.
    /// </summary>
    /// <remarks>The <c>HelpFileBuilderPlugInExportAttribute</c> is used to export your plug-in so that the help
    /// file builder finds it and can make use of it.  The example below shows the basic usage for a common
    /// plug-in.  Set the additional attribute values as needed:
    ///
    /// <list type="bullet">
    ///     <item>
    ///         <term>RunsInPartialBuild</term>
    ///         <description>Set this to true if your plug-in should run in partial builds used to generate
    /// reflection data for the API Filter editor dialog or namespace comments used for the Namespace Comments
    /// editor dialog.  Typically, this is left set to false.</description>
    ///     </item>
    /// </list>
    /// 
    /// Plug-ins are singletons in nature.  The composition container will create instances as needed and will
    /// dispose of them when the container is disposed of.</remarks>
    [HelpFileBuilderPlugInExport("SpecialAttributeRendererPlugin", Version = AssemblyInfo.ProductVersion,
      Copyright = AssemblyInfo.Copyright, Description = "SpecialAttributeRendererPlugin plug-in")]
    public sealed class SpecialAttributeRendererPluginPlugIn : IPlugIn
    {
        #region Private data members
        //=====================================================================

        private List<ExecutionPoint> executionPoints;

        private BuildProcess builder;

        #endregion

        #region IPlugIn implementation
        //=====================================================================

        /// <summary>
        /// This read-only property returns a collection of execution points that define when the plug-in should
        /// be invoked during the build process.
        /// </summary>
        public IEnumerable<ExecutionPoint> ExecutionPoints
        {
            get
            {
                if (executionPoints == null)
                    executionPoints = new List<ExecutionPoint>
                    {
                        // TODO: Modify this to set your execution points
                        new ExecutionPoint(BuildStep.CreateBuildAssemblerConfigs, ExecutionBehaviors.Before),
                        new ExecutionPoint(BuildStep.ApplyDocumentModel, ExecutionBehaviors.After)
                    };

                return executionPoints;
            }
        }

        /// <summary>
        /// This method is used to initialize the plug-in at the start of the build process
        /// </summary>
        /// <param name="buildProcess">A reference to the current build process</param>
        /// <param name="configuration">The configuration data that the plug-in should use to initialize itself</param>
        public void Initialize(BuildProcess buildProcess, XElement configuration)
        {
            builder = buildProcess;

            var metadata = (HelpFileBuilderPlugInExportAttribute)this.GetType().GetCustomAttributes(
                typeof(HelpFileBuilderPlugInExportAttribute), false).First();

            builder.ReportProgress("{0} Version {1}\r\n{2}", metadata.Id, metadata.Version, metadata.Copyright);

            // TODO: Add your initialization code here such as reading the configuration data
        }

        /// <summary>
        /// This method is used to execute the plug-in during the build process
        /// </summary>
        /// <param name="context">The current execution context</param>
        public void Execute(ExecutionContext context)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));

            builder.ReportWarning("A2", context.BuildStep.ToString());

            //if (context.BuildStep == BuildStep.CreateBuildAssemblerConfigs)
            {
                builder.ReportWarning("Ava.0001", "CheckPoint 1 hit");

                var transformation = builder.PresentationStyle.TopicTransformation;

                var handler = transformation.ApiTopicSectionHandlerFor(ApiTopicSectionType.Notices, null);

                transformation.RenderStarting += Transformation_RenderStarting;
                transformation.SectionRendered += Transformation_SectionRendered;

                #region Add a custom XML comments section handler
                //=====================================================================

                // Add a custom section after the summary section
                var summaryHandler = transformation.ApiTopicSectionHandlerFor(ApiTopicSectionType.Summary, null);

                // Use the CustomSection section type and give it a unique name for use in locating it
                transformation.InsertApiTopicSectionHandlerAfter(summaryHandler,
                    new ApiTopicSectionHandler(ApiTopicSectionType.CustomSection, "PresentationStyleModsCustomSection",
                        RenderCustomSection));
                #endregion


            }

        }

        private void Transformation_SectionRendered(object sender, RenderedSectionEventArgs e)
        {

        }

        private void Transformation_RenderStarting(object sender, RenderTopicEventArgs e)
        {
            builder.ReportWarning("A3", e.Key.ToString(), e.TopicContent.ToString());

            //if (sender is TopicTransformationCore transformation)
            //{
            //    transformation.RenderChildElements(e.TopicContent.Root, [new XText("Hello World")]);
            //}
        }
        #endregion

        /// <summary>
        /// This is used to handle rendering of a custom XML comments section
        /// </summary>
        /// <param name="transformation">The current transformation used to render the section</param>
        private void RenderCustomSection(TopicTransformationCore transformation)
        {
            var customSection = transformation.CommentsNode.Element("customSection");

            if (customSection != null)
            {
                // Create a section with an optional title
                var (title, content) = transformation.CreateSection("CS_" + customSection.GenerateUniqueId(), true,
                    "title_customSection", null);

                if (title != null)
                    transformation.CurrentElement.Add(title);

                if (content != null)
                    transformation.CurrentElement.Add(content);

                // Render the child elements of the custom section into the content element.  Any additional
                // rendering tasks could be performed here as well.  If the section contains custom elements,
                // use element handlers to render them.
                transformation.RenderChildElements(content ?? transformation.CurrentElement, customSection.Nodes());
            }
        }

        #region IDisposable implementation
        //=====================================================================

        // TODO: If the plug-in hasn't got any disposable resources, this finalizer can be removed
        /// <summary>
        /// This handles garbage collection to ensure proper disposal of the plug-in if not done explicitly
        /// with <see cref="Dispose()"/>.
        /// </summary>
        ~SpecialAttributeRendererPluginPlugIn()
        {
            this.Dispose();
        }

        /// <summary>
        /// This implements the Dispose() interface to properly dispose of the plug-in object
        /// </summary>
        public void Dispose()
        {
            // TODO: Dispose of any resources here if necessary
            GC.SuppressFinalize(this);
        }
        #endregion
    }
}
