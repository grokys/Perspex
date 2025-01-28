//===============================================================================================================
// System  : Sandcastle Tools Standard Presentation Styles
// File    : AvaloniaHtmlTransformation.cs
// Author  : Eric Woodruff  (Eric@EWoodruff.us)
// Updated : 08/30/2024
// Note    : Copyright 2022-2024, Eric Woodruff, All rights reserved
//
// This file contains the class used to generate a MAML or API HTML topic from the raw topic XML data for the
// Avalonia Html presentation style.
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

// Ignore Spelling: fa resizer

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using Sandcastle.Core.PresentationStyle.Transformation;
using Sandcastle.Core.PresentationStyle.Transformation.Elements;
using Sandcastle.Core.PresentationStyle.Transformation.Elements.Html;
using Sandcastle.Core.Reflection;

namespace Avalonia.Sandcastle.PresentationStyles.AvaloniaHtml
{
    /// <summary>
    /// This class is used to generate a MAML or API HTML topic from the raw topic XML data for the Avalonia Html
    /// presentation style.
    /// </summary>
    public class AvaloniaHtmlTransformation : TopicTransformationCore
    {
        #region Private data members
        //=====================================================================

        private XDocument pageTemplate;
        private readonly AvaloniaHtmlPresentationStyle presentationStyle;

        #endregion

        #region Constructor
        //=====================================================================

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="presentationStyle">The presentation style that is making use of this transformation</param>
        public AvaloniaHtmlTransformation(AvaloniaHtmlPresentationStyle presentationStyle) :
          base(presentationStyle.SupportedFormats, presentationStyle.ResolvePath)
        {
            this.presentationStyle = presentationStyle;
            this.TopicTemplatePath = this.ResolvePath(@"Templates\TopicTemplate.html");
        }
        #endregion

        #region Topic transformation argument shortcut properties
        //=====================================================================

        /// <summary>
        /// Robots metadata
        /// </summary>
        private string RobotsMetadata => this.TransformationArguments[nameof(RobotsMetadata)].Value;

        /// <summary>
        /// Root breadcrumb title text
        /// </summary>
        private string RootBreadcrumbTitleText => this.TransformationArguments[nameof(RootBreadcrumbTitleText)].Value;

        /// <summary>
        /// Enable the resizable table of contents column
        /// </summary>
        private bool ResizableTocColumn => Boolean.TryParse(this.TransformationArguments[nameof(ResizableTocColumn)].Value,
            out bool resizableTocColumn) && resizableTocColumn;

        /// <summary>
        /// Render collapsible sections
        /// </summary>
        private bool CollapsibleSections => Boolean.TryParse(this.TransformationArguments[nameof(CollapsibleSections)].Value,
            out bool collapsibleSections) && collapsibleSections;

        /// <summary>
        /// Logo file
        /// </summary>
        private string LogoFile => this.TransformationArguments[nameof(LogoFile)].Value;

        /// <summary>
        /// Logo height
        /// </summary>
        private int LogoHeight => Int32.TryParse(this.TransformationArguments[nameof(LogoHeight)].Value,
            out int height) ? height : 0;

        /// <summary>
        /// Logo width
        /// </summary>
        private int LogoWidth => Int32.TryParse(this.TransformationArguments[nameof(LogoWidth)].Value,
            out int width) ? width : 0;

        /// <summary>
        /// Logo alternate text
        /// </summary>
        private string LogoAltText => this.TransformationArguments[nameof(LogoAltText)].Value;

        /// <summary>
        /// Logo placement
        /// </summary>
        private LogoPlacement LogoPlacement => Enum.TryParse(this.TransformationArguments[nameof(LogoPlacement)].Value,
            true, out LogoPlacement placement) ? placement : LogoPlacement.Left ;

        /// <summary>
        /// Logo alignment
        /// </summary>
        private LogoAlignment LogoAlignment => Enum.TryParse(this.TransformationArguments[nameof(LogoAlignment)].Value,
            true, out LogoAlignment alignment) ? alignment : LogoAlignment.Left;

        /// <summary>
        /// Logo URL
        /// </summary>
        private string LogoUrl => this.TransformationArguments[nameof(LogoUrl)].Value;

        /// <summary>
        /// Maximum version parts
        /// </summary>
        private int MaxVersionParts => Int32.TryParse(this.TransformationArguments[nameof(MaxVersionParts)].Value,
            out int maxVersionParts) ? maxVersionParts : 5;

        /// <summary>
        /// Default language
        /// </summary>
        private string DefaultLanguage => !String.IsNullOrWhiteSpace(this.TransformationArguments[nameof(DefaultLanguage)].Value) ?
            this.TransformationArguments[nameof(DefaultLanguage)].Value : this.TransformationArguments[nameof(DefaultLanguage)].DefaultValue;

        /// <summary>
        /// Include enumerated type values
        /// </summary>
        private bool IncludeEnumValues => Boolean.TryParse(this.TransformationArguments[nameof(IncludeEnumValues)].Value,
            out bool includeEnumValues) && includeEnumValues;

        /// <summary>
        /// Enumeration member sort order
        /// </summary>
        private EnumMemberSortOrder EnumMemberSortOrder => Enum.TryParse(this.TransformationArguments[nameof(EnumMemberSortOrder)].Value,
            true, out EnumMemberSortOrder sortOrder) ? sortOrder : EnumMemberSortOrder.Value;

        /// <summary>
        /// Flags enumeration value format
        /// </summary>
        private EnumValueFormat FlagsEnumValueFormat => Enum.TryParse(this.TransformationArguments[nameof(FlagsEnumValueFormat)].Value,
            true, out EnumValueFormat format) ? format : EnumValueFormat.IntegerValue;

        /// <summary>
        /// Flags enumeration value separator group size
        /// </summary>
        private int FlagsEnumSeparatorSize => Int32.TryParse(this.TransformationArguments[nameof(FlagsEnumSeparatorSize)].Value,
            out int groupSize) ? groupSize : 0;

        /// <summary>
        /// Include separators for integer enumeration values
        /// </summary>
        private bool IncludeIntegerEnumSeparators => Boolean.TryParse(this.TransformationArguments[nameof(IncludeIntegerEnumSeparators)].Value,
            out bool includeSeparators) && includeSeparators;

        /// <summary>
        /// Base source code URL
        /// </summary>
        private string BaseSourceCodeUrl => this.TransformationArguments[nameof(BaseSourceCodeUrl)].Value;

        /// <summary>
        /// Request example URL
        /// </summary>
        private string RequestExampleUrl => this.TransformationArguments[nameof(RequestExampleUrl)].Value;

        /// <summary>
        /// Show parameters on all methods on the member list page, not just on overloads
        /// </summary>
        private bool ShowParametersOnAllMethods => Boolean.TryParse(this.TransformationArguments[nameof(ShowParametersOnAllMethods)].Value,
            out bool showParameters) && showParameters;

        /// <summary>
        /// Always show top level auto-outline
        /// </summary>
        private bool AlwaysShowTopLevelAutoOutline => Boolean.TryParse(this.TransformationArguments[nameof(AlwaysShowTopLevelAutoOutline)].Value,
            out bool alwaysShowOutOutline) && alwaysShowOutOutline;

        #endregion

        #region TopicTransformationCore implementation
        //=====================================================================

        /// <inheritdoc />
        public override string IconPath { get; set; } = "../icons/";

        /// <inheritdoc />
        public override string StyleSheetPath { get; set; } = "../css/";

        /// <inheritdoc />
        public override string ScriptPath { get; set; } = "../scripts/";

        /// <inheritdoc />
        protected override void CreateTransformationArguments()
        {
            this.AddTransformationArgumentRange(new[]
            {
                new TransformationArgument(nameof(RobotsMetadata), true, true, null,
                    "An optional robots metadata value (e.g. noindex, nofollow).  If left blank, the robots " +
                    "metadata element will be omitted from the topics."),
                new TransformationArgument(nameof(BibliographyDataFile), true, true, null,
                    "An optional bibliography data XML file.  Specify the filename with a fully qualified or " +
                    "relative path.  If the path is relative or omitted, it is assumed to be relative to the " +
                    "project folder.\r\n\r\n" +
                    "If blank, no bibliography section will be included in the topics.\r\n\r\n" +
                    "For information on the data file's format, see the bibliography element topic in the " +
                    "Sandcastle MAML Guide or XML Comments Guide."),
                new TransformationArgument(nameof(RootBreadcrumbTitleText), true, true, "Docs", "Specify the " +
                    "text to use for the root breadcrumb's title.  The default if blank is \"Docs\"."),
                new TransformationArgument(nameof(ResizableTocColumn), true, true, "False", "Indicate whether " +
                    "or not to enable the resizable table of contents column.  True to enable it, false if not."),
                new TransformationArgument(nameof(CollapsibleSections), true, true, "True", "Indicate whether " +
                    "or not collapsible sections are rendered.  True to include them, false if not."),
                new TransformationArgument(nameof(LogoFile), true, true, null,
                    "An optional logo file to insert into the topic headers.  Specify the filename only, omit " +
                    "the path.\r\n\r\n" +
                    "Important: Add a folder called \"icons\\\" to the root of your help file builder project and " +
                    "place the logo file in the icons\\ folder.  Set the Build Action property to Content on the " +
                    "logo file's properties.\r\n\r\n" +
                    "If blank, no logo will appear in the topic headers.  If building website output and your web " +
                    "server is case-sensitive, be sure to match the case of the folder name in your project with " +
                    "that of the presentation style.  The same applies to the logo filename itself."),
                new TransformationArgument(nameof(LogoHeight), true, true, null,
                    "An optional logo height in pixels.  If left blank, the actual logo image height is used."),
                new TransformationArgument(nameof(LogoWidth), true, true, null,
                    "An optional logo width in pixels.  If left blank, the actual logo image width is used."),
                new TransformationArgument(nameof(LogoAltText), true, true, null,
                    "Optional logo alternate text.  If left blank, no alternate text is added."),
                new TransformationArgument(nameof(LogoPlacement), true, true, "Left",
                    "An optional logo placement.  Specify Left, Right, or Above.  If not specified, the " +
                    "default is Left."),
                new TransformationArgument(nameof(LogoAlignment), true, true, "Left",
                    "An optional logo alignment when using the 'Above' placement option.  Specify Left, " +
                    "Right, or Center.  If not specified, the default is Left."),
                new TransformationArgument(nameof(LogoUrl), true, true, null,
                    "An optional logo URL to navigate to when the logo is clicked."),
                new TransformationArgument(nameof(MaxVersionParts), false, true, null,
                    "The maximum number of assembly version parts to show in API member topics.  Set to 2, " +
                    "3, or 4 to limit it to 2, 3, or 4 parts or leave it blank for all parts including the " +
                    "assembly file version value if specified."),
                new TransformationArgument(nameof(DefaultLanguage), true, true, "cs",
                    "The default language to use for syntax sections, code snippets, and a language-specific " +
                    "text.  This should be set to cs, vb, cpp, fs, or the keyword style parameter value of a " +
                    "third-party syntax generator if you want to use a non-standard language as the default."),
                new TransformationArgument(nameof(IncludeEnumValues), false, true, "True",
                    "Set this to True to include the column for the numeric value of each field in " +
                    "enumerated type topics.  Set it to False to omit the numeric values column."),
                new TransformationArgument(nameof(EnumMemberSortOrder), false, true, "Value",
                    "The sort order for enumeration members.  Set it to Value to sort by value or Name to sort " +
                    "by name."),
                new TransformationArgument(nameof(FlagsEnumValueFormat), false, true, "IntegerValue",
                    "The format of flags enumeration values: IntegerValue, HexValue, or BitFlags"),
                new TransformationArgument(nameof(FlagsEnumSeparatorSize), false, true, "0",
                    "The separator group size for flags enumeration values (0, 4, or 8).  This determines where " +
                    "separators are placed in the formatted value (e.g. 0b0000_0000, 0x1234_ABCD).  If set to " +
                    "zero, no separators will be inserted."),
                new TransformationArgument(nameof(IncludeIntegerEnumSeparators), false, true, "true",
                    "Set this to true to include separators in integer enumeration values (1,000 vs 1000)."),
                new TransformationArgument(nameof(BaseSourceCodeUrl), false, true, null,
                    "If you set the Source Code Base Path property in the Paths category, specify the URL to " +
                    "the base source code folder on your project's website here.  Some examples for GitHub are " +
                    "shown below.\r\n\r\n" +
                    "Important: Be sure to set the Source Code Base Path property and terminate the URL below with " +
                    "a slash if necessary.\r\n\r\n" +
                    "Format: https://github.com/YourUserID/YourProject/blob/BranchNameOrCommitHash/BaseSourcePath/ \r\n\r\n" +
                    "Master branch: https://github.com/JohnDoe/WidgestProject/blob/master/src/ \r\n" +
                    "A different branch: https://github.com/JohnDoe/WidgestProject/blob/dev-branch/src/ \r\n" +
                    "A specific commit: https://github.com/JohnDoe/WidgestProject/blob/c6e41c4fc2a4a335352d2ae8e7e85a1859751662/src/"),
                new TransformationArgument(nameof(RequestExampleUrl), false, true, null,
                    "To include a link that allows users to request an example for an API topic, set the URL " +
                    "to which the request will be sent.  This can be a web page URL or an e-mail URL.  Only include " +
                    "the URL as the parameters will be added automatically by the topic.  For example:\r\n\r\n" +
                    "Create a new issue on GitHub: https://github.com/YourUserID/YourProject/issues/new \r\n" +
                    "Send via e-mail: mailto:YourEmailAddress@Domain.com"),
                new TransformationArgument(nameof(ShowParametersOnAllMethods), false, true, "False",
                    "If false, the default, parameters are hidden on all but overloaded methods on the member " +
                    "list pages.  If set to true, parameters are shown on all methods."),
                new TransformationArgument(nameof(AlwaysShowTopLevelAutoOutline), true, false, "False",
                    "If false, the default, top-level auto-outlines are hidden unless the display is small " +
                    "enough (typically mobile displays).  If set to true, top-level auto-outlines are always " +
                    "shown regardless of the display size and the In This Article section is hidden.")
            });
        }

        /// <inheritdoc />
        protected override void CreateLanguageSpecificText()
        {
            this.AddLanguageSpecificTextRange(new[]
            {
                new LanguageSpecificText(true, new[]
                {
                    (LanguageSpecificText.CPlusPlus, "nullptr"),
                    (LanguageSpecificText.VisualBasic, "Nothing"),
                    (LanguageSpecificText.Neutral, "null"),
                }),
                new LanguageSpecificText(true, new[]
                {
                    (LanguageSpecificText.VisualBasic, "Shared"),
                    (LanguageSpecificText.Neutral, "static"),
                }),
                new LanguageSpecificText(true, new[]
                {
                    (LanguageSpecificText.VisualBasic, "Overridable"),
                    (LanguageSpecificText.Neutral, "virtual"),
                }),
                new LanguageSpecificText(true, new[]
                {
                    (LanguageSpecificText.VisualBasic, "True"),
                    (LanguageSpecificText.Neutral, "true"),
                }),
                new LanguageSpecificText(true, new[]
                {
                    (LanguageSpecificText.VisualBasic, "False"),
                    (LanguageSpecificText.Neutral, "false"),
                }),
                new LanguageSpecificText(true, new[]
                {
                    (LanguageSpecificText.VisualBasic, "MustInherit"),
                    (LanguageSpecificText.Neutral, "abstract"),
                }),
                new LanguageSpecificText(true, new[]
                {
                    (LanguageSpecificText.VisualBasic, "NotInheritable"),
                    (LanguageSpecificText.Neutral, "sealed"),
                }),
                new LanguageSpecificText(true, new[]
                {
                    (LanguageSpecificText.VisualBasic, "In"),
                    (LanguageSpecificText.FSharp, String.Empty),
                    (LanguageSpecificText.Neutral, "in"),
                }),
                new LanguageSpecificText(true, new[]
                {
                    (LanguageSpecificText.VisualBasic, "Out"),
                    (LanguageSpecificText.FSharp, String.Empty),
                    (LanguageSpecificText.Neutral, "out"),
                }),
                new LanguageSpecificText(true, new[]
                {
                    (LanguageSpecificText.VisualBasic, "Async"),
                    (LanguageSpecificText.Neutral, "async"),
                }),
                new LanguageSpecificText(true, new[]
                {
                    (LanguageSpecificText.VisualBasic, "Await"),
                    (LanguageSpecificText.FSharp, "let!"),
                    (LanguageSpecificText.Neutral, "await"),
                })
            });
        }

        /// <inheritdoc />
        protected override void CreateElementHandlers()
        {
            this.AddElements(new Element[]
            {
                // MAML document root element types
                new NonRenderedParentElement("topic"),
                new NonRenderedParentElement("codeEntityDocument"),
                new NonRenderedParentElement("developerConceptualDocument"),
                new NonRenderedParentElement("developerErrorMessageDocument"),
                new NonRenderedParentElement("developerGlossaryDocument"),
                new NonRenderedParentElement("developerHowToDocument"),
                new NonRenderedParentElement("developerOrientationDocument"),
                new NonRenderedParentElement("developerReferenceWithSyntaxDocument"),
                new NonRenderedParentElement("developerReferenceWithoutSyntaxDocument"),
                new NonRenderedParentElement("developerSDKTechnologyOverviewArchitectureDocument"),
                new NonRenderedParentElement("developerSDKTechnologyOverviewCodeDirectoryDocument"),
                new NonRenderedParentElement("developerSDKTechnologyOverviewOrientationDocument"),
                new NonRenderedParentElement("developerSDKTechnologyOverviewScenariosDocument"),
                new NonRenderedParentElement("developerSDKTechnologyOverviewTechnologySummaryDocument"),
                new NonRenderedParentElement("developerSampleDocument"),
                new NonRenderedParentElement("developerTroubleshootingDocument"),
                new NonRenderedParentElement("developerUIReferenceDocument"),
                new NonRenderedParentElement("developerWalkthroughDocument"),
                new NonRenderedParentElement("developerWhitePaperDocument"),
                new NonRenderedParentElement("developerXmlReference"),

                // HTML elements (may occur in XML comments)
                new PassthroughElement("a"),
                new PassthroughElement("abbr"),
                new PassthroughElement("acronym"),
                new PassthroughElement("area"),
                new PassthroughElement("article"),
                new PassthroughElement("aside"),
                new PassthroughElement("audio"),
                new PassthroughElement("b"),
                new PassthroughElement("bdi"),
                new PassthroughElement("blockquote"),
                new PassthroughElement("br"),
                new PassthroughElement("canvas"),
                new PassthroughElement("datalist"),
                new PassthroughElement("dd"),
                new PassthroughElement("del"),
                new PassthroughElement("details"),
                new PassthroughElement("dialog"),
                new PassthroughElement("div"),
                new PassthroughElement("dl"),
                new PassthroughElement("dt"),
                new PassthroughElement("em"),
                new PassthroughElement("embed"),
                new PassthroughElement("figcaption"),
                new PassthroughElement("figure"),
                new PassthroughElement("font"),
                new PassthroughElement("footer"),
                new PassthroughElement("h1"),
                new PassthroughElement("h2"),
                new PassthroughElement("h3"),
                new PassthroughElement("h4"),
                new PassthroughElement("h5"),
                new PassthroughElement("h6"),
                new PassthroughElement("header"),
                new PassthroughElement("hr"),
                new PassthroughElement("i"),
                new PassthroughElement("img"),
                new PassthroughElement("ins"),
                new PassthroughElement("keygen"),
                new PassthroughElement("li"),
                new PassthroughElement("main"),
                new PassthroughElement("map"),
                new PassthroughElement("mark"),
                new PassthroughElement("meter"),
                new PassthroughElement("nav"),
                new PassthroughElement("ol"),
                new PassthroughElement("output"),
                new PassthroughElement("p"),
                new PassthroughElement("pre"),
                new PassthroughElement("progress"),
                new PassthroughElement("q"),
                new PassthroughElement("rp"),
                new PassthroughElement("rt"),
                new PassthroughElement("ruby"),
                new PassthroughElement("source"),
                new PassthroughElement("strong"),
                new PassthroughElement("sub"),
                new PassthroughElement("sup"),
                new PassthroughElement("svg"),
                new PassthroughElement("tbody"),
                new PassthroughElement("td"),
                new PassthroughElement("tfoot"),
                new PassthroughElement("th"),
                new PassthroughElement("thead"),
                new PassthroughElement("time"),
                new PassthroughElement("tr"),
                new PassthroughElement("track"),
                new PassthroughElement("u"),
                new PassthroughElement("ul"),
                new PassthroughElement("video"),
                new PassthroughElement("wbr"),

                // Elements common to HTML, MAML, and/or XML comments.  Processing may differ based on the topic
                // type (API or MAML).
                new BibliographyElement(),
                new CiteElement(),
                new CodeSnippetGroupElementLanguageFilter(),
                new PassthroughElement("include"),
                new PassthroughElement("includeAttribute"),
                new MarkupElement(),
                new ConvertibleElement("para", "p"),
                new ListElement(),
                new ParametersElement(),
                new PassthroughElement("referenceLink"),
                new PassthroughElement("span"),
                new SummaryElement(),
                new TableElement(),

                // MAML elements
                new NoteElement("alert"),
                new ConvertibleElement("application", "strong"),
                new NamedSectionElement("appliesTo"),
                // Only display top-level auto-outlines on mobile devices.  For tablet and above, the In This
                // Article quick links serve the same purpose.  This can be overridden by the transformation argument.
                new AutoOutlineElement { TopLevelStyleName = "is-hidden-tablet" },
                new NamedSectionElement("background"),
                new NamedSectionElement("buildInstructions"),
                new CodeEntityReferenceElement(),
                new CodeExampleElement(),
                new ConvertibleElement("codeFeaturedElement", "strong"),
                new ConvertibleElement("codeInline", "span", "code"),
                new NonRenderedParentElement("codeReference"),
                new ConvertibleElement("command", "span", "command"),
                new ConvertibleElement("computerOutputInline", "span", "code"),
                new NonRenderedParentElement("conclusion"),
                new NonRenderedParentElement("content"),
                new CopyrightElement(),
                new NonRenderedParentElement("corporation"),
                new NonRenderedParentElement("country"),
                new ConvertibleElement("database", "strong"),
                new NonRenderedParentElement("date"),
                new ConvertibleElement("definedTerm", "dt", true),
                new ConvertibleElement("definition", "dd"),
                new ConvertibleElement("definitionTable", "dl"),
                new NamedSectionElement("demonstrates"),
                new NonRenderedParentElement("description"),
                new NamedSectionElement("dotNetFrameworkEquivalent"),
                new ConvertibleElement("embeddedLabel", "strong"),
                new EntryElement(),
                new ConvertibleElement("environmentVariable", "span", "code"),
                new ConvertibleElement("errorInline", "em"),
                new NamedSectionElement("exceptions"),
                new ExternalLinkElement(),
                new NamedSectionElement("externalResources"),
                new ConvertibleElement("fictitiousUri", "em"),
                new ConvertibleElement("foreignPhrase", "em"),
                new GlossaryElement(),
                new ConvertibleElement("hardware", "strong"),
                new NamedSectionElement("inThisSection"),
                new IntroductionElement(),
                new LanguageKeywordElement(),
                new NamedSectionElement("languageReferenceRemarks"),
                new NonRenderedParentElement("legacy"),
                new ConvertibleElement("legacyBold", "strong"),
                new ConvertibleElement("legacyItalic", "em"),
                new LegacyLinkElement(),
                new ConvertibleElement("legacyUnderline", "u"),
                new ConvertibleElement("lineBreak", "br"),
                new LinkElement(),
                new ConvertibleElement("listItem", "li", true),
                new ConvertibleElement("literal", "span", "literal"),
                new ConvertibleElement("localUri", "em"),
                new NonRenderedParentElement("localizedText"),
                new ConvertibleElement("math", "em"),
                new MediaLinkElement(),
                new MediaLinkInlineElement(),
                new ConvertibleElement("newTerm", "em"),
                new NamedSectionElement("nextSteps"),
                new ConvertibleElement("parameterReference", "em"),
                new ConvertibleElement("phrase", "em"),
                new ConvertibleElement("placeholder", "em"),
                new NamedSectionElement("prerequisites"),
                new ProcedureElement(),
                new ConvertibleElement("quote", "blockquote"),
                new ConvertibleElement("quoteInline", "q"),
                new NamedSectionElement("reference"),
                new NamedSectionElement("relatedSections"),
                new RelatedTopicsElement(),
                new ConvertibleElement("replaceable", "em"),
                new NamedSectionElement("requirements"),
                new NamedSectionElement("returnValue"),
                new NamedSectionElement("robustProgramming"),
                new ConvertibleElement("row", "tr"),
                new SectionElement(),
                new NonRenderedParentElement("sections"),
                new NamedSectionElement("security"),
                new NonRenderedParentElement("snippets"),
                new ConvertibleElement("step", "li", true),
                new StepsElement(),
                new ConvertibleElement("subscript", "sub"),
                new ConvertibleElement("subscriptType", "sub"),
                new ConvertibleElement("superscript", "sup"),
                new ConvertibleElement("superscriptType", "sup"),
                new ConvertibleElement("system", "strong"),
                new ConvertibleElement("tableHeader", "thead"),
                new NamedSectionElement("textValue"),
                // The title element is ignored.  The section and table elements handle them as needed.
                new IgnoredElement("title"),
                new NonRenderedParentElement("type"),
                new ConvertibleElement("ui", "strong"),
                new ConvertibleElement("unmanagedCodeEntityReference", "strong"),
                new ConvertibleElement("userInput", "strong"),
                new ConvertibleElement("userInputLocalizable", "strong"),
                new NamedSectionElement("whatsNew"),

                // XML comments and reflection data file elements
                new ConvertibleElement("c", "span", "code"),
                new PassthroughElement("conceptualLink"),
                new NamedSectionElement("example"),
                new ImplementsElement(),
                new NoteElement("note"),
                new ConvertibleElement("paramref", "name", "span", "parameter"),
                new PreliminaryElement(),
                new NamedSectionElement("remarks"),
                new ReturnsElement(),
                new SeeElement(),
                // seeAlso should be a top-level element in the comments but may appear within other elements.
                // We'll ignore it if seen as they'll be handled manually by the See Also section processing.
                new IgnoredElement("seealso"),
                // For this presentation style, namespace/assembly info and inheritance hierarchy are part of
                // the definition (syntax) section.
                new SyntaxElementLanguageFilter(nameof(RequestExampleUrl), nameof(BaseSourceCodeUrl))
                {
                    NamespaceAndAssemblyInfoRenderer = RenderApiNamespaceAndAssemblyInformation,
                    InheritanceHierarchyRenderer = RenderApiInheritanceHierarchy
                },
                new TemplatesElement(),
                new ThreadsafetyElement(),
                new ConvertibleElement("typeparamref", "name", "span", "parameter"),
                new ValueElement(),
                new VersionsElement()
            });
        }

        /// <inheritdoc />
        protected override void CreateApiTopicSectionHandlers()
        {
            // API Topic sections will be rendered in this order by default
            this.AddApiTopicSectionHandlerRange(new[]
            {
                new ApiTopicSectionHandler(ApiTopicSectionType.Notices, t => RenderNotices(t)),
                new ApiTopicSectionHandler(ApiTopicSectionType.Summary, t => RenderApiSummarySection(t)),
                new ApiTopicSectionHandler(ApiTopicSectionType.SyntaxSection, t => RenderApiSyntaxSection(t)),
                new ApiTopicSectionHandler(ApiTopicSectionType.Remarks, t => RenderApiRemarksSection(t)),
                new ApiTopicSectionHandler(ApiTopicSectionType.Examples, t => RenderApiExamplesSection(t)),
                new ApiTopicSectionHandler(ApiTopicSectionType.MemberList, t => RenderApiMemberList(t)),
                new ApiTopicSectionHandler(ApiTopicSectionType.Events,
                    t => RenderApiSectionTable(t, "title_events", t.CommentsNode.Elements("event"))),
                new ApiTopicSectionHandler(ApiTopicSectionType.Exceptions,
                    t => RenderApiSectionTable(t, "title_exceptions", this.CommentsNode.Elements("exception"))),
                new ApiTopicSectionHandler(ApiTopicSectionType.Versions, t => RenderApiVersionsSection(t)),
                new ApiTopicSectionHandler(ApiTopicSectionType.Permissions,
                    t => RenderApiSectionTable(t, "title_permissions", t.CommentsNode.Elements("permission"))),
                new ApiTopicSectionHandler(ApiTopicSectionType.ThreadSafety,
                    t => t.RenderNode(t.CommentsNode.Element("threadsafety"))),
                new ApiTopicSectionHandler(ApiTopicSectionType.RevisionHistory,
                    t => RenderApiRevisionHistorySection(t)),
                new ApiTopicSectionHandler(ApiTopicSectionType.Bibliography,
                    t => RenderApiBibliographySection(t)),
                new ApiTopicSectionHandler(ApiTopicSectionType.SeeAlso, t => RenderApiSeeAlsoSection(t))
            });
        }

        /// <inheritdoc />
        protected override XDocument RenderTopic()
        {
            if(pageTemplate == null)
            {
                // Clear the top level style if auto-outlines are always shown
                if(this.AlwaysShowTopLevelAutoOutline)
                    ((AutoOutlineElement)this.ElementHandlerFor("autoOutline")).TopLevelStyleName = null;

                string localeSpecificStyleSheet = presentationStyle.ResolvePath($"css\\{this.Locale}.css");

                if(!File.Exists(localeSpecificStyleSheet))
                    localeSpecificStyleSheet = String.Empty;
                else
                {
                    localeSpecificStyleSheet = "<link rel=\"stylesheet\" " +
                        $"href=\"{this.StyleSheetPath + Path.GetFileName(localeSpecificStyleSheet)}\" />";
                }

                string resizableTocStyle = String.Empty, resizerStyle = "is-hidden", resizableContentStyle = String.Empty;

                if(this.ResizableTocColumn)
                {
                    resizableTocStyle = " toc-resizable";
                    resizerStyle = "toc-resizer";
                    resizableContentStyle = " toc-resizable-content";
                }

                pageTemplate = LoadTemplateFile(this.TopicTemplatePath, new[] {
                    ("{@Locale}", this.Locale),
                    ("{@LocaleLowercase}", this.Locale.ToLowerInvariant()),
                    ("{@IconPath}", this.IconPath),
                    ("{@StyleSheetPath}", this.StyleSheetPath),
                    ("{@LocaleSpecificStyleSheet}", localeSpecificStyleSheet),
                    ("{@ScriptPath}", this.ScriptPath),
                    ("{@DefaultLanguage}", this.DefaultLanguage),
                    ("{@ResizableTocStyle}", resizableTocStyle),
                    ("{@ResizerStyle}", resizerStyle),
                    ("{@ResizableContentStyle}", resizableContentStyle),
                });
            }

            // Set the default language and connect the language-specific text, and language filter on startup
            this.RegisterStartupScript(100, $"SetDefaultLanguage(\"{this.DefaultLanguage}\");");

            var document = new XDocument(pageTemplate);

            XElement html = document.Root, head = html.Element("head"),
                topicContent = html.Descendants().Where(d => d.Attribute("id")?.Value == "TopicContent").FirstOrDefault();

            this.CurrentElement = head ?? throw new InvalidOperationException("Page template is missing the head element");
            this.RenderHeaderMetadata();

            this.CurrentElement = topicContent ?? throw new InvalidOperationException("Page template is missing the \"TopicContent\" element");
            this.RenderPageTitleAndLogo(html.Element("body"));

            var headerPrelimContainer = html.Descendants().Where(d => d.Attribute("id")?.Value == "HeaderPrelimContainer").FirstOrDefault();

            if(headerPrelimContainer != null)
            {
                if(!this.HasHeaderText && !this.IsPreliminaryDocumentation)
                    headerPrelimContainer.Remove();
                else
                {
                    if(!this.HasHeaderText)
                    {
                        var headerText = headerPrelimContainer.Descendants().Where(
                            d => d.Attribute("id")?.Value == "HeaderTextContainer").FirstOrDefault();

                        headerText?.Remove();
                    }

                    if(!this.IsPreliminaryDocumentation)
                    {
                        var prelimDocs = headerPrelimContainer.Descendants().Where(
                            d => d.Attribute("id")?.Value == "PreliminaryContainer").FirstOrDefault();

                        prelimDocs?.Remove();
                    }
                }
            }

            var footer = html.Descendants().Where(d => d.Attribute("id")?.Value == "PageFooter").FirstOrDefault();

            if(footer != null)
            {
                if(!this.HasFooterContent && !this.HasWebsiteAdContent)
                    footer.Remove();
                else
                {
                    if(!this.HasFooterContent)
                    {
                        var footerContent = footer.Descendants().Where(
                            d => d.Attribute("id")?.Value == "FooterContent").FirstOrDefault();

                        footerContent?.Remove();
                    }

                    if(!this.HasWebsiteAdContent)
                    {
                        var adContent = footer.Descendants().Where(
                            d => d.Attribute("id")?.Value == "AdContent").FirstOrDefault();

                        adContent?.Remove();
                    }
                }
            }

            // Add language filter selections
            var languageFilter = html.Descendants().FirstOrDefault(d => d.Attribute("id")?.Value == "LanguageSelections") ??
                throw new InvalidOperationException("An element with the ID 'LanguageSelections' was not found " +
                    "to contain the language filter elements");

            if(this.LanguageFilter.Any())
            {
                foreach(var language in this.LanguageFilter)
                {
                    languageFilter.Add(new XElement("a",
                        new XAttribute("class", "dropdown-item languageFilterItem"),
                        new XAttribute("data-languageId", language.KeywordStyle),
                        new XElement("include", new XAttribute("item", language.SharedContentItemId))));
                }
            }
            else
            {
                // This presentation style requires at least one entry in the language filter dropdown.  If none
                // are defined, we'll add a default C# entry to the dropdown and hide it.
                languageFilter.Add(new XElement("a",
                    new XAttribute("class", "dropdown-item languageFilterItem"),
                    new XAttribute("data-languageId", "cs"),
                    new XElement("include", new XAttribute("item", "devlang_C#"))));

                var dropdownClass = languageFilter.Parent?.Parent?.Attribute("class");

                if(dropdownClass != null)
                    dropdownClass.Value += " is-hidden";
            }

            this.OnRenderStarting(document);

            // Add the topic content.  MAML topics are rendered purely off of the element types.  API topics
            // require custom formatting based on the member type in the topic.
            if(this.IsMamlTopic)
                this.RenderNode(this.TopicNode);
            else
            {
                foreach(var section in this.ApiTopicSections)
                {
                    section.RenderSection(this);
                    this.OnSectionRendered(section.SectionType, section.CustomSectionName);
                }
            }

            // Add the In This Article quick links and add all registered startup script blocks
            var body = html.Element("body") ?? throw new InvalidOperationException("Body element not found");

            this.RenderInThisArticleMenu(body);

            if(this.StartupScriptBlocks.Any())
                body.Add(new XElement("script", $"$(function(){{\r\n{String.Join("\r\n", this.StartupScriptBlocks)}\r\n}});"));

            if(this.StartupScriptBlockItemIds.Any())
            {
                var scriptItems = new XElement("script");
                
                body.Add(scriptItems);

                foreach(string id in this.StartupScriptBlockItemIds)
                    scriptItems.Add(new XElement("include", new XAttribute("item", id)));
            }

            this.OnRenderCompleted(document);

            return document;
        }

        /// <inheritdoc />
        public override (XElement Title, XElement Content) CreateSection(string uniqueId, bool localizedTitle,
          string title, string linkId)
        {
            string toggleSectionId = uniqueId + "Section";
            XElement titleElement = null;

            if(String.IsNullOrWhiteSpace(title))
            {
                if(localizedTitle)
                    throw new ArgumentException("Title cannot be null if it represents a localized item ID", nameof(title));
            }
            else
            {
                XNode titleContent, collapseToggle = null;

                if(localizedTitle)
                    titleContent = new XElement("include", new XAttribute("item", title));
                else
                    titleContent = new XText(title);

                if(this.CollapsibleSections)
                {
                    collapseToggle = new XElement("span",
                            new XAttribute("class", "icon toggleSection"),
                            new XAttribute("tabindex", "0"),
                        new XElement("i", new XAttribute("class", "fa fa-angle-down"), String.Empty));

                    this.RegisterStartupScript(5000, @"
$("".toggleSection"" ).click(function () {
    SectionExpandCollapse(this);
});

$("".toggleSection"").keypress(function () {
    SectionExpandCollapseCheckKey(this, event)
});");
                }

                titleElement = new XElement("h2", collapseToggle, titleContent);

                if(!String.IsNullOrWhiteSpace(linkId))
                    titleElement.Add(new XAttribute("id", linkId));
            }

            var contentElement = new XElement("div", new XAttribute("id", toggleSectionId));

            // Add some top padding if there is no title or the section is flush with the prior content
            if(titleElement == null)
                contentElement.Add(new XAttribute("class", "noTitle"));

            return (titleElement, contentElement);
        }

        /// <inheritdoc />
        /// <remarks>The returned content element is always null and the content should be inserted into the
        /// transformation's current element after adding the title element.</remarks>
        public override (XElement Title, XElement Content) CreateSubsection(bool localizedTitle, string title)
        {
            XElement titleElement = null;

            if(String.IsNullOrWhiteSpace(title))
            {
                if(localizedTitle)
                    throw new ArgumentException("Title cannot be null if it represents a localized item ID", nameof(title));
            }
            else
            {
                XNode titleContent;

                if(localizedTitle)
                    titleContent = new XElement("include", new XAttribute("item", title));
                else
                    titleContent = new XText(title);

                titleElement = new XElement("h4", titleContent);
            }

            return (titleElement, null);
        }
        #endregion

        #region General topic rendering helper methods
        //=====================================================================

        /// <summary>
        /// This is used to add topic metadata to the <c>head</c> element
        /// </summary>
        /// <remarks>The <see cref="TopicTransformationCore.CurrentElement" /> should be set to the <c>head</c>
        /// element before calling this.</remarks>
        private void RenderHeaderMetadata()
        {
            string topicDesc;
            
            if(!String.IsNullOrWhiteSpace(this.RobotsMetadata))
            {
                this.CurrentElement.Add(new XElement("meta", new XAttribute("name", "robots"),
                    new XAttribute("content", this.RobotsMetadata)));
            }

            this.CurrentElement.Add(new XElement("title", this.IsMamlTopic ? this.MamlTopicTitle() :
                this.ApiTopicTitle(true, true)));

            if(this.IsMamlTopic)
            {
                string tocTitle = this.MetadataNode.Element("tableOfContentsTitle")?.Value.NormalizeWhiteSpace();

                if(String.IsNullOrWhiteSpace(tocTitle))
                    tocTitle = ((XText)this.MamlTopicTitle()).Value;

                this.CurrentElement.Add(new XElement("meta", new XAttribute("name", "Title"),
                    new XAttribute("content", tocTitle)));
            }
            else
            {
                XNode title;

                if(this.ApiMember.ApiGroup == ApiMemberGroup.Namespace)
                {
                    // For namespaces only show the title without any descriptive text as the TOC title
                    title = new XText(this.ApiMember.Name);
                }
                else
                {
                    // Use simple titles except for operators which need the parameter details to differentiate
                    // them in many cases.
                    if(this.ApiMember.ApiTopicSubgroup == ApiMemberGroup.Overload ||
                      this.ApiMember.ApiSubSubgroup != ApiMemberGroup.Operator)
                    {
                        title = this.ApiTopicTocTitleSimple();
                    }
                    else
                        title = this.ApiTopicTitle(false, true);
                }

                this.CurrentElement.Add(new XElement("meta", new XAttribute("name", "Title"),
                    new XElement("includeAttribute",
                        new XAttribute("name", "content"),
                        new XAttribute("item", "tocTitle"),
                        new XElement("parameter", title))));
            }

            if(this.IsMamlTopic)
            {
                topicDesc = this.TopicNode.Descendants(Element.Ddue + "para").FirstOrDefault()?.Value.NormalizeWhiteSpace();

                this.CurrentElement.Add(new XElement("meta", new XAttribute("name", "Help.Id"),
                    new XAttribute("content", this.TopicNode.Attribute("id").Value)));

                var topicType = TopicType.FromElementName(this.TopicNode.Elements().First().Name.LocalName);

                if(topicType != null)
                {
                    this.CurrentElement.Add(new XElement("meta", new XAttribute("name", "Help.ContentType"),
                        new XAttribute("content", TopicType.DescriptionForTopicTypeGroup(topicType.ContentType))));
                }
                else
                {
                    this.CurrentElement.Add(new XElement("meta", new XAttribute("name", "Help.ContentType"),
                        new XAttribute("content", TopicType.DescriptionForTopicTypeGroup(TopicTypeGroup.Concepts))));
                }

                this.CurrentElement.Add(new XElement("meta", new XAttribute("name", "guid"),
                    new XAttribute("content", this.TopicNode.Attribute("id").Value)));
            }
            else
            {
                topicDesc = this.CommentsNode.Element("summary")?.Value.NormalizeWhiteSpace();

                this.CurrentElement.Add(new XElement("meta", new XAttribute("name", "Help.Id"),
                    new XAttribute("content", this.Key)));
                this.CurrentElement.Add(new XElement("meta", new XAttribute("name", "Help.ContentType"),
                    new XAttribute("content", TopicType.DescriptionForTopicTypeGroup(TopicTypeGroup.Reference))));

                // Insert container and filename metadata for an API topic
                string namespaceId = this.ReferenceNode.Element("containers")?.Element(
                    "namespace").Attribute("api").Value, namespaceName = "(Default Namespace)";

                // Get the namespace from the container node for most members
                if(namespaceId != null && namespaceId.Length > 2 && namespaceId[1] == ':')
                    namespaceName = namespaceId.Substring(2);
                else
                {
                    if(String.IsNullOrWhiteSpace(namespaceId))
                    {
                        // If it's a namespace, get the name from the API data node.  For all others, assume it's
                        // the default namespace
                        if((this.ApiMember.ApiGroup == ApiMemberGroup.NamespaceGroup ||
                          this.ApiMember.ApiGroup == ApiMemberGroup.Namespace) &&
                          !String.IsNullOrWhiteSpace(this.ApiMember.Name))
                        {
                            namespaceName = this.ApiMember.Name;
                        }
                    }
                }

                this.CurrentElement.Add(new XElement("meta", new XAttribute("name", "container"),
                    new XAttribute("content", namespaceName)));

                if(!String.IsNullOrWhiteSpace(this.ApiMember.TopicFilename))
                {
                    this.CurrentElement.Add(new XElement("meta", new XAttribute("name", "guid"),
                        new XAttribute("content", this.ApiMember.TopicFilename)));
                }
            }

            if(!String.IsNullOrWhiteSpace(topicDesc))
            {
                if(topicDesc.Length > 256)
                    topicDesc = topicDesc.Substring(0, 256);

                int pos = topicDesc.LastIndexOf('.');

                if(pos != -1)
                    topicDesc = topicDesc.Substring(0, pos + 1);

                this.CurrentElement.Add(new XElement("meta", new XAttribute("name", "Description"),
                    new XAttribute("content", topicDesc)));
            }
        }

        /// <summary>
        /// This is used to render the page title and optional logo
        /// </summary>
        /// <param name="body">A reference to the body element.  If defined, the logo will be inserted into the
        /// appropriate location.</param>
        /// <remarks>The <see cref="TopicTransformationCore.CurrentElement" /> should be set to the topic content
        /// element before calling this as the title will be inserted into it.</remarks>
        private void RenderPageTitleAndLogo(XElement body)
        {
            string logoFile = this.LogoFile;

            if(!String.IsNullOrWhiteSpace(logoFile))
            {
                var headerDiv = body.Descendants("div").FirstOrDefault(d => d.Attribute("id")?.Value == "Header") ??
                    throw new InvalidOperationException("A div element with the ID 'Header' was not found");

                string logoAltText = this.LogoAltText, logoUrl = this.LogoUrl;
                int logoWidth = this.LogoWidth, logoHeight = this.LogoHeight;

                var image = new XElement("img");

                if(!String.IsNullOrWhiteSpace(logoAltText))
                    image.Add(new XAttribute("alt", logoAltText));

                if(logoHeight > 0 && logoWidth > 0)
                    image.Add(new XAttribute("style", $"height: {logoHeight}px; width: {logoWidth}px;"));
                else
                {
                    if(logoHeight > 0)
                        image.Add(new XAttribute("style", $"height: {logoHeight}px;"));

                    if(logoWidth > 0)
                        image.Add(new XAttribute("style", $"width: {logoWidth}px;"));
                }

                image.Add(new XAttribute("src", this.IconPath + logoFile));

                if(!String.IsNullOrWhiteSpace(logoUrl))
                {
                    image = new XElement("a",
                        new XAttribute("target", "_blank"),
                        new XAttribute("rel", "noopener noreferrer"),
                        new XAttribute("href", logoUrl), new XElement(image));
                }

                switch(this.LogoPlacement)
                {
                    case LogoPlacement.Above:
                        switch(this.LogoAlignment)
                        {
                            case LogoAlignment.Right:
                                headerDiv.AddFirst(new XElement("div",
                                        new XAttribute("class", "pageHeader level mb-0 pt-0 px-2"),
                                    new XElement("div",
                                        new XAttribute("class", "level-left"), Element.NonBreakingSpace),
                                    new XElement("div",
                                        new XAttribute("class", "level-right"),
                                            new XElement("div",
                                                new XAttribute("class", "level-item"),
                                                image))));
                                break;

                            case LogoAlignment.Center:
                                headerDiv.AddFirst(new XElement("div",
                                    new XAttribute("class", "pageHeader is-centered mb-0 pt-0"),
                                    image));
                                break;

                            default:    // Left
                                headerDiv.AddFirst(new XElement("div",
                                    new XAttribute("class", "pageHeader pt-0 px-2"),
                                    image));
                                break;
                        }
                        break;

                    case LogoPlacement.Right:
                        var langFilterContainer = headerDiv.Descendants("div").FirstOrDefault(d =>
                            d.Attribute("id")?.Value == "LangFilterSearchContainer") ??
                            throw new InvalidOperationException("A div element with the ID 'LangFilterSearchContainer' was not found");

                        langFilterContainer.Add(new XElement("div",
                            new XAttribute("class", "level-item"),
                            image));
                        break;

                    default:        // Left
                        var titleContainer = headerDiv.Descendants("div").FirstOrDefault(d =>
                            d.Attribute("id")?.Value == "TitleContainer") ??
                            throw new InvalidOperationException("A div element with the ID 'TitleContainer' was not found");

                        titleContainer.AddFirst(new XElement("div",
                            new XAttribute("class", "level-item"),
                            image));
                        break;
                }
            }

            this.CurrentElement.Add(new XElement("h1",
                this.IsMamlTopic ? this.MamlTopicTitle() : this.ApiTopicTitle(false, false)));
        }
        #endregion

        #region API topic section handlers
        //=====================================================================

        /// <summary>
        /// This is used to render the preliminary and obsolete API notices
        /// </summary>
        /// <param name="transformation">The topic transformation to use</param>
        private static void RenderNotices(TopicTransformationCore transformation)
        {
            var preliminary = transformation.CommentsNode.Element("preliminary");
            var obsolete = transformation.ReferenceNode.AttributeOfType("T:System.ObsoleteAttribute");

            if(preliminary != null || obsolete != null)
            {
                var currentElement = transformation.CurrentElement;
                var notes = new XElement("span", new XAttribute("class", "tags"));

                currentElement.Add(new XElement("div", new XAttribute("id", "TopicNotices"), notes));

                transformation.CurrentElement = notes;

                if(preliminary != null)
                    transformation.RenderNode(preliminary);

                if(obsolete != null)
                {
                    notes.Add(new XElement("span",
                        new XAttribute("class", "tag is-danger is-medium"),
                        new XElement("include", new XAttribute("item", "boilerplate_obsoleteLong"))));
                }

                transformation.CurrentElement = currentElement;
            }
        }

        /// <summary>
        /// This is used to render the summary section
        /// </summary>
        /// <param name="transformation">The topic transformation to use</param>
        private static void RenderApiSummarySection(TopicTransformationCore transformation)
        {
            if(transformation.ApiMember.ApiTopicSubgroup != ApiMemberGroup.Overload)
                transformation.RenderNode(transformation.CommentsNode.Element("summary"));
            else
            {
                // Render the summary from the first overloads element.  There should only be one.
                var overloads = transformation.ReferenceNode.Descendants("overloads").FirstOrDefault();

                if(overloads != null)
                {
                    var summary = overloads.Element("summary");

                    if(summary != null)
                        transformation.RenderNode(summary);
                    else
                    {
                        var div = new XElement("div", new XAttribute("class", "summary"));

                        transformation.CurrentElement.Add(div);
                        transformation.RenderChildElements(div, overloads.Nodes());
                    }
                }
            }
        }

        /// <summary>
        /// Render the inheritance hierarchy section
        /// </summary>
        /// <param name="transformation">The topic transformation to use</param>
        /// <param name="content">The content element to which the information is added</param>
        private static void RenderApiInheritanceHierarchy(TopicTransformationCore transformation, XElement content)
        {
            XElement dl, dd, hierarchyItem, family = transformation.ReferenceNode.Element("family");
            bool isFirst = true;

            if(family != null)
            {
                XElement descendants = family.Element("descendents"), ancestors = family.Element("ancestors");

                dl = new XElement("dl", new XAttribute("class", "inheritanceHierarchy"),
                    new XElement("dt", new XElement("include", new XAttribute("item", "text_inheritance"))));
                dd = new XElement("dd");

                dl.Add(dd);
                content.Add(dl);

                if(ancestors != null)
                {
                    // Ancestor types are stored nearest to most distant so reverse them
                    foreach(var typeInfo in ancestors.Elements().Reverse())
                    {
                        if(!isFirst)
                        {
                            dd.Add(Element.NonBreakingSpace, Element.NonBreakingSpace, new XElement("span",
                                    new XAttribute("class", "icon is-small"),
                                new XElement("i",
                                    new XAttribute("class", "fa fa-arrow-right"), String.Empty)),
                                Element.NonBreakingSpace, Element.NonBreakingSpace);
                        }

                        transformation.RenderTypeReferenceLink(dd, typeInfo, false);
                        isFirst = false;
                    }
                }

                if(!isFirst)
                {
                    dd.Add(Element.NonBreakingSpace, Element.NonBreakingSpace, new XElement("span",
                            new XAttribute("class", "icon is-small"),
                        new XElement("i",
                            new XAttribute("class", "fa fa-arrow-right"), String.Empty)),
                        Element.NonBreakingSpace, Element.NonBreakingSpace);
                }

                dd.Add(new XElement("referenceLink",
                        new XAttribute("target", transformation.Key),
                        new XAttribute("show-container", false)));

                if(descendants != null)
                {
                    dl = new XElement("dl", new XAttribute("class", "inheritanceHierarchy"),
                        new XElement("dt", new XElement("include", new XAttribute("item", "text_derived"))));
                    dd = new XElement("dd");

                    dl.Add(dd);
                    content.Add(dl);

                    int count = 1, totalDescendants = descendants.Elements().Count();

                    foreach(var typeInfo in descendants.Elements().OrderBy(e => e.Attribute("api")?.Value))
                    {
                        hierarchyItem = new XElement("div");

                        if(count > 4 && totalDescendants > 5)
                            hierarchyItem.Add(new XAttribute("class", "is-hidden hiddenDescendant"));

                        dd.Add(hierarchyItem);
                        transformation.RenderTypeReferenceLink(hierarchyItem, typeInfo, true);

                        count++;
                    }

                    if(totalDescendants > 5)
                    {
                        dd.Add(new XElement("a", new XAttribute("class", "descendantsToggle hiddenDescendant"),
                            new XElement("include", new XAttribute("item", "text_moreInheritance")), " ",
                            new XElement("span", new XAttribute("class", "icon is-small"),
                            new XElement("i", new XAttribute("class", "fa fa-chevron-down"), String.Empty))));
                        dd.Add(new XElement("a", new XAttribute("class", "descendantsToggle hiddenDescendant is-hidden"),
                            new XElement("include", new XAttribute("item", "text_lessInheritance")), " ",
                            new XElement("span", new XAttribute("class", "icon is-small"),
                            new XElement("i", new XAttribute("class", "fa fa-chevron-up"), String.Empty))));

                        transformation.RegisterStartupScript(1000, @"$("".descendantsToggle"").click(function () {
    $("".hiddenDescendant"").toggleClass(""is-hidden"");
});");
                    }
                }
            }

            var implements = transformation.ReferenceNode.Element("implements");

            if(implements != null)
            {
                dl = new XElement("dl", new XAttribute("class", "implementsList"),
                    new XElement("dt", new XElement("include", new XAttribute("item", "text_implements"))));
                dd = new XElement("dd");

                dl.Add(dd);
                content.Add(dl);
                isFirst = true;

                foreach(var typeInfo in implements.Elements().OrderBy(e => e.Attribute("api")?.Value))
                {
                    if(!isFirst)
                        dd.Add(", ");

                    transformation.RenderTypeReferenceLink(dd, typeInfo, false);
                    isFirst = false;
                }
            }
        }

        /// <summary>
        /// This is used to render namespace and assembly information for an API topic
        /// </summary>
        /// <param name="transformation">The topic transformation to use</param>
        /// <param name="content">The content element to which the information is added</param>
        private static void RenderApiNamespaceAndAssemblyInformation(TopicTransformationCore transformation,
          XElement content)
        {
            // Only API member pages get namespace/assembly info
            if(transformation.ApiMember.ApiTopicGroup == ApiMemberGroup.List ||
               transformation.ApiMember.ApiTopicGroup == ApiMemberGroup.RootGroup ||
               transformation.ApiMember.ApiTopicGroup == ApiMemberGroup.Root ||
               transformation.ApiMember.ApiTopicGroup == ApiMemberGroup.NamespaceGroup ||
               transformation.ApiMember.ApiTopicGroup == ApiMemberGroup.Namespace)
            {
                return;
            }

            var containers = transformation.ReferenceNode.Element("containers");
            var libraries = containers.Elements("library");

            content.Add(new XElement("strong",
                new XElement("include", new XAttribute("item", "boilerplate_requirementsNamespace"))),
                Element.NonBreakingSpace,
                new XElement("referenceLink",
                    new XAttribute("target", containers.Element("namespace").Attribute("api").Value)),
                new XElement("br"));

            int separatorSize = 1;
            bool first = true;

            if(libraries.Count() > 1)
            {
                content.Add(new XElement("strong",
                    new XElement("include", new XAttribute("item", "boilerplate_requirementsAssemblies"))));
                separatorSize = 2;
            }
            else
            {
                content.Add(new XElement("strong",
                    new XElement("include", new XAttribute("item", "boilerplate_requirementsAssemblyLabel"))));
            }

            string separator = new String(Element.NonBreakingSpace, separatorSize);
            int maxVersionParts = ((AvaloniaHtmlTransformation)transformation).MaxVersionParts;

            foreach(var l in libraries)
            {
                if(!first)
                    content.Add(new XElement("br"));

                content.Add(separator);

                string version = l.Element("assemblydata").Attribute("version").Value,
                    extension = l.Attribute("kind").Value.Equals(
                        "DynamicallyLinkedLibrary", StringComparison.Ordinal) ? "dll" : "exe";
                string[] versionParts = version.Split(VersionNumberSeparators, StringSplitOptions.RemoveEmptyEntries);

                // Limit the version number parts if requested
                if(maxVersionParts > 1 && maxVersionParts < 5)
                    version = String.Join(".", versionParts, 0, maxVersionParts);

                content.Add(new XElement("include",
                        new XAttribute("item", "assemblyNameAndModule"),
                    new XElement("parameter", l.Attribute("assembly").Value),
                    new XElement("parameter", l.Attribute("module").Value),
                    new XElement("parameter", extension),
                    new XElement("parameter", version)));

                first = false;
            }

            // Show XAML XML namespaces for APIs that support XAML.  All topics that have auto-generated XAML
            // syntax get an "XMLNS for XAML" line in the Requirements section.  Topics with boilerplate XAML
            // syntax, e.g. "Not applicable", do NOT get this line.
            var xamlCode = transformation.SyntaxNode.Elements("div").Where(d => d.Attribute("codeLanguage")?.Value.Equals(
                "XAML", StringComparison.Ordinal) ?? false);

            if(xamlCode.Any())
            {
                var xamlXmlNS = xamlCode.Elements("div").Where(d => d.Attribute("class")?.Value == "xamlXmlnsUri");

                content.Add(new XElement("br"),
                    new XElement("strong",
                        new XElement("include", new XAttribute("item", "boilerplate_xamlXmlnsRequirements"))),
                    Element.NonBreakingSpace);

                if(xamlXmlNS.Any())
                {
                    first = true;

                    foreach(var d in xamlXmlNS)
                    {
                        if(!first)
                            content.Add(", ");

                        content.Add(d.Value.NormalizeWhiteSpace());
                        first = false;
                    }
                }
                else
                    content.Add(new XElement("include", new XAttribute("item", "boilerplate_unmappedXamlXmlns")));
            }
        }

        /// <summary>
        /// This is used to render the syntax section
        /// </summary>
        /// <param name="transformation">The topic transformation to use</param>
        private static void RenderApiSyntaxSection(TopicTransformationCore transformation)
        {
            // Only API member pages get a syntax section
            if(transformation.ApiMember.ApiTopicGroup != ApiMemberGroup.List &&
               transformation.ApiMember.ApiTopicGroup != ApiMemberGroup.RootGroup &&
               transformation.ApiMember.ApiTopicGroup != ApiMemberGroup.Root &&
               transformation.ApiMember.ApiTopicGroup != ApiMemberGroup.NamespaceGroup &&
               transformation.ApiMember.ApiTopicGroup != ApiMemberGroup.Namespace)
            {
                transformation.RenderNode(transformation.SyntaxNode);
            }
        }

        /// <summary>
        /// This is used to render a member list topic (root, root group, namespace group, namespace, enumeration,
        /// type, or type member list).
        /// </summary>
        /// <param name="transformation">The topic transformation to use</param>
        private static void RenderApiMemberList(TopicTransformationCore transformation)
        {
            switch(transformation.ApiMember)
            {
                case var t when t.ApiTopicGroup == ApiMemberGroup.RootGroup || t.ApiTopicGroup == ApiMemberGroup.Root:
                    RenderApiRootList(transformation);
                    break;

                case var t when t.ApiTopicGroup == ApiMemberGroup.NamespaceGroup:
                    RenderApiNamespaceGroupList(transformation);
                    break;

                case var t when t.ApiTopicGroup == ApiMemberGroup.Namespace:
                    RenderApiNamespaceList(transformation);
                    break;

                case var t when t.ApiTopicSubgroup == ApiMemberGroup.Enumeration:
                    RenderApiEnumerationMembersList(transformation);
                    break;

                case var t when t.ApiTopicGroup == ApiMemberGroup.Type || t.ApiTopicGroup == ApiMemberGroup.List:
                    RenderApiTypeMemberLists(transformation);
                    break;
            }
        }

        /// <summary>
        /// Render the list in a root group or root topic
        /// </summary>
        /// <param name="transformation">The topic transformation to use</param>
        private static void RenderApiRootList(TopicTransformationCore transformation)
        {
            var elements = transformation.ReferenceNode.Element("elements")?.Elements("element").OrderBy(
                e => e.Element("apidata").Attribute("name").Value).ToList();

            if((elements?.Count ?? 0) == 0)
                return;

            var (title, content) = transformation.CreateSection(elements[0].GenerateUniqueId(), true, "title_namespaces", null);

            transformation.CurrentElement.Add(title);
            transformation.CurrentElement.Add(content);

            var table = new XElement("table",
                    new XAttribute("id", "namespaceList"),
                    new XAttribute("class", "table is-hoverable"));

            content.Add(table);

            foreach(var e in elements)
            {
                string name = e.Element("apidata").Attribute("name").Value;
                var refLink = new XElement("referenceLink",
                    new XAttribute("target", e.Attribute("api").Value),
                    new XAttribute("qualified", "false"));
                var summaryCell = new XElement("td");

                if(name.Length == 0)
                    refLink.Add(new XElement("include", new XAttribute("item", "defaultNamespace")));

                table.Add(new XElement("tr",
                    new XElement("td", refLink),
                    summaryCell));

                var summary = e.Element("summary");

                if(summary != null)
                    transformation.RenderChildElements(summaryCell, summary.Nodes());
                else
                    summaryCell.Add(Element.NonBreakingSpace);
            }
        }

        /// <summary>
        /// Render the list in a namespace group topic
        /// </summary>
        /// <param name="transformation">The topic transformation to use</param>
        private static void RenderApiNamespaceGroupList(TopicTransformationCore transformation)
        {
            var elements = transformation.ReferenceNode.Element("elements")?.Elements("element").OrderBy(e =>
            {
                string name = e.Attribute("api").Value;
                return name.Substring(name.IndexOf(':') + 1);
            }).ToList();

            if((elements?.Count ?? 0) == 0)
                return;

            var (title, content) = transformation.CreateSection(elements[0].GenerateUniqueId(), true,
                "tableTitle_namespace", null);

            transformation.CurrentElement.Add(title);
            transformation.CurrentElement.Add(content);

            var table = new XElement("table",
                    new XAttribute("id", "namespaceList"),
                    new XAttribute("class", "table is-hoverable"));

            content.Add(table);

            foreach(var e in elements)
            {
                var summaryCell = new XElement("td");

                table.Add(new XElement("tr",
                    new XElement("td",
                        new XElement("referenceLink",
                            new XAttribute("target", e.Attribute("api").Value),
                            new XAttribute("qualified", "false"))),
                    summaryCell));

                var summary = e.Element("summary");

                if(summary != null)
                    transformation.RenderChildElements(summaryCell, summary.Nodes());
                else
                    summaryCell.Add(Element.NonBreakingSpace);
            }
        }

        /// <summary>
        /// Render the category lists in a namespace topic
        /// </summary>
        /// <param name="transformation">The topic transformation to use</param>
        private static void RenderApiNamespaceList(TopicTransformationCore transformation)
        {
            var elements = transformation.ReferenceNode.Element("elements").Elements("element").GroupBy(
                e => e.Element("apidata").Attribute("subgroup").Value).ToDictionary(k => k.Key, v => v);

            foreach(string key in new[] { "class", "structure", "interface", "delegate", "enumeration" })
            {
                if(elements.TryGetValue(key, out var group))
                {
                    var (title, content) = transformation.CreateSection(group.First().GenerateUniqueId(), true,
                        "tableTitle_" + key, null);

                    transformation.CurrentElement.Add(title);
                    transformation.CurrentElement.Add(content);

                    var table = new XElement("table",
                            new XAttribute("id", key + "List"),
                            new XAttribute("class", "table is-hoverable"));

                    content.Add(table);

                    foreach(var e in group.OrderBy(el => el.Attribute("api").Value))
                    {
                        var summaryCell = new XElement("td");

                        table.Add(new XElement("tr",
                            new XElement("td",
                                new XElement("referenceLink",
                                    new XAttribute("target", e.Attribute("api").Value),
                                    new XAttribute("qualified", "false"))),
                            summaryCell));

                        var summary = e.Element("summary");

                        if(summary != null)
                            transformation.RenderChildElements(summaryCell, summary.Nodes());

                        var obsoleteAttr = e.AttributeOfType("T:System.ObsoleteAttribute");
                        var prelimComment = e.Element("preliminary");

                        if(obsoleteAttr != null || prelimComment != null)
                        {
                            if(!summaryCell.IsEmpty)
                                summaryCell.Add(new XElement("br"));

                            if(obsoleteAttr != null)
                            {
                                summaryCell.Add(new XElement("span",
                                        new XAttribute("class", "tag is-danger"),
                                    new XElement("include",
                                        new XAttribute("item", "boilerplate_obsoleteShort"))));
                            }

                            if(prelimComment != null)
                            {
                                summaryCell.Add(new XElement("span",
                                        new XAttribute("class", "tag is-warning"),
                                    new XElement("include",
                                        new XAttribute("item", "preliminaryShort"))));
                            }
                        }

                        if(summaryCell.IsEmpty)
                            summaryCell.Add(Element.NonBreakingSpace);
                    }
                }
            }
        }

        /// <summary>
        /// Render the members of an enumeration
        /// </summary>
        /// <param name="transformation">The topic transformation to use</param>
        private static void RenderApiEnumerationMembersList(TopicTransformationCore transformation)
        {
            // Convert to this type so that we can access the argument shortcuts easily
            var thisTransform = (AvaloniaHtmlTransformation)transformation;

            var allMembers = transformation.ReferenceNode.Element("elements")?.Elements("element").ToList();

            if(allMembers == null)
                return;

            List<XElement> fieldMembers = new List<XElement>(), extensionsMethods = new List<XElement>();

            // Enumerations can have extension methods which need to be rendered in a separate section
            foreach(var m in allMembers)
            {
                XElement apiData = m.Element("apidata");

                // Some members such as inherited interface members on a derived interface, contain no
                // metadata and we'll ignore them.
                if(apiData == null)
                    continue;

                if(Enum.TryParse<ApiMemberGroup>(apiData.Attribute("subgroup")?.Value, true, out var subgroup) &&
                  subgroup == ApiMemberGroup.Field)
                {
                    fieldMembers.Add(m);
                }
                else
                    extensionsMethods.Add(m);
            }

            if(fieldMembers.Count != 0)
            {
                // Sort order is configurable for enumeration members
                EnumMemberSortOrder enumMemberSortOrder = thisTransform.EnumMemberSortOrder;

                var elements = fieldMembers.OrderBy(el => enumMemberSortOrder == EnumMemberSortOrder.Name ?
                    el.Element("apidata").Attribute("name").Value :
                    el.Element("value").Value.PadLeft(20, ' ')).ToList();

                var enumValues = elements.Select(e => e.Element("value").Value).ToList();
                bool includeEnumValues = thisTransform.IncludeEnumValues;
                int idx;

                if(includeEnumValues)
                {
                    EnumValueFormat enumFormat = thisTransform.FlagsEnumValueFormat;
                    int groupSize = thisTransform.IncludeIntegerEnumSeparators ? 3 : 0, minWidth = 0;
                    bool signedValues = enumValues.Any(v => v.Length > 0 && v[0] == '-');

                    if(enumFormat != EnumValueFormat.IntegerValue &&
                      thisTransform.ReferenceNode.AttributeOfType("T:System.FlagsAttribute") != null)
                    {
                        groupSize = thisTransform.FlagsEnumSeparatorSize;

                        if(groupSize != 0 && groupSize != 4 && groupSize != 8)
                            groupSize = 0;

                        // Determine the minimum width of the values
                        if(signedValues)
                        {
                            minWidth = enumValues.Select(v => TopicTransformationExtensions.FormatSignedEnumValue(v,
                                enumFormat, 0, 0)).Max(v => v.Length) - 2;
                        }
                        else
                        {
                            minWidth = enumValues.Select(v => TopicTransformationExtensions.FormatUnsignedEnumValue(v,
                                enumFormat, 0, 0)).Max(v => v.Length) - 2;
                        }

                        if(minWidth < 3)
                            minWidth = 2;
                        else
                        {
                            if((minWidth % 4) != 0)
                                minWidth += 4 - (minWidth % 4);
                        }
                    }
                    else
                        enumFormat = EnumValueFormat.IntegerValue;   // Enforce integer format for non-flags enums

                    for(idx = 0; idx < enumValues.Count; idx++)
                    {
                        if(signedValues)
                        {
                            enumValues[idx] = TopicTransformationExtensions.FormatSignedEnumValue(enumValues[idx],
                                enumFormat, minWidth, groupSize);
                        }
                        else
                        {
                            enumValues[idx] = TopicTransformationExtensions.FormatUnsignedEnumValue(enumValues[idx],
                                enumFormat, minWidth, groupSize);
                        }
                    }
                }

                var (title, content) = thisTransform.CreateSection(elements.First().GenerateUniqueId(), true,
                    "topicTitle_enumMembers", null);

                thisTransform.CurrentElement.Add(title);
                thisTransform.CurrentElement.Add(content);

                var table = new XElement("table",
                        new XAttribute("id", "enumMemberList"),
                        new XAttribute("class", "table is-hoverable"));

                content.Add(table);
                idx = 0;

                foreach(var e in elements)
                {
                    var summaryCell = new XElement("td");

                    XElement valueCell = null;

                    if(includeEnumValues)
                    {
                        valueCell = new XElement("td", enumValues[idx]);
                        idx++;
                    }

                    table.Add(new XElement("tr",
                        new XElement("td", e.Element("apidata").Attribute("name").Value),
                        valueCell,
                        summaryCell));

                    var summary = e.Element("summary");
                    var remarks = e.Element("remarks");

                    if(summary != null || remarks != null)
                    {
                        if(summary != null)
                            thisTransform.RenderChildElements(summaryCell, summary.Nodes());

                        // Enum members may have additional authored content in the remarks node
                        if(remarks != null)
                            thisTransform.RenderChildElements(summaryCell, remarks.Nodes());
                    }

                    if(e.AttributeOfType("T:System.ObsoleteAttribute") != null)
                    {
                        if(!summaryCell.IsEmpty)
                            summaryCell.Add(new XElement("br"));

                        summaryCell.Add(new XElement("span",
                                new XAttribute("class", "tag is-danger"),
                            new XElement("include",
                                new XAttribute("item", "boilerplate_obsoleteShort"))));
                    }

                    if(summaryCell.IsEmpty)
                        summaryCell.Add(Element.NonBreakingSpace);
                }
            }

            if(extensionsMethods.Count != 0)
                RenderApiTypeMemberLists(transformation);
        }

        /// <summary>
        /// Render type member lists
        /// </summary>
        /// <remarks>This is used for types and the member list subtopics</remarks>
        /// <param name="transformation">The topic transformation to use</param>
        private static void RenderApiTypeMemberLists(TopicTransformationCore transformation)
        {
            var allMembers = transformation.ReferenceNode.Element("elements")?.Elements("element").ToList();

            if((allMembers?.Count ?? 0) == 0)
                return;

            var overloads = allMembers.Where(e => e.Attribute("api").Value.StartsWith("Overload:",
                StringComparison.Ordinal)).ToList();

            // Remove overload topics and add their members to the full member list
            foreach(var overload in overloads)
            {
                allMembers.Remove(overload);
                allMembers.AddRange(overload.Elements("element"));
            }

            var memberGroups = new Dictionary<ApiMemberGroup, List<XElement>>
            {
                { ApiMemberGroup.Constructor, new List<XElement>() },
                { ApiMemberGroup.Property, new List<XElement>() },
                { ApiMemberGroup.Method, new List<XElement>() },
                { ApiMemberGroup.Event, new List<XElement>() },
                { ApiMemberGroup.Operator, new List<XElement>() },
                { ApiMemberGroup.Field, new List<XElement>() },
                { ApiMemberGroup.AttachedProperty, new List<XElement>() },
                { ApiMemberGroup.AttachedEvent, new List<XElement>() },
                { ApiMemberGroup.Extension, new List<XElement>() },
                { ApiMemberGroup.ExplicitInterfaceImplementation, new List<XElement>() },
                // Only used for overloads topics.  It won't appear on normal list pages.
                { ApiMemberGroup.Overload, new List<XElement>() },
            };

            if(transformation.ApiMember.ApiTopicSubgroup != ApiMemberGroup.Overload)
            {
                // Group the members by section type
                foreach(var m in allMembers)
                {
                    XElement apiData = m.Element("apidata"), memberData = m.Element("memberdata"),
                        procedureData = m.Element("proceduredata");

                    // Some members such as inherited interface members on a derived interface, contain no
                    // metadata and we'll ignore them.
                    if(apiData == null)
                        continue;

                    if(!Enum.TryParse<ApiMemberGroup>(apiData.Attribute("subgroup")?.Value, true, out var subgroup))
                        subgroup = ApiMemberGroup.Unknown;

                    if(!Enum.TryParse<ApiMemberGroup>(apiData.Attribute("subsubgroup")?.Value, true, out var subsubgroup))
                        subsubgroup = ApiMemberGroup.Unknown;

                    switch(m)
                    {
                        // The order of checks is important here and doesn't match the order of the rendered
                        // sections.  It minimizes the conditions we need to check in each subsequent case.
                        case var mbr when procedureData?.Attribute("eii")?.Value == "true":
                            memberGroups[ApiMemberGroup.ExplicitInterfaceImplementation].Add(mbr);
                            break;

                        case var mbr when subgroup == ApiMemberGroup.Constructor:
                            memberGroups[ApiMemberGroup.Constructor].Add(mbr);
                            break;

                        case var mbr when subgroup == ApiMemberGroup.Property && subsubgroup == ApiMemberGroup.Unknown:
                            memberGroups[ApiMemberGroup.Property].Add(mbr);
                            break;

                        case var mbr when subgroup == ApiMemberGroup.Method && subsubgroup == ApiMemberGroup.Unknown:
                            memberGroups[ApiMemberGroup.Method].Add(mbr);
                            break;

                        case var mbr when subgroup == ApiMemberGroup.Event && subsubgroup == ApiMemberGroup.Unknown:
                            memberGroups[ApiMemberGroup.Event].Add(mbr);
                            break;

                        case var mbr when subsubgroup == ApiMemberGroup.Operator:
                            memberGroups[ApiMemberGroup.Operator].Add(mbr);
                            break;

                        case var mbr when subgroup == ApiMemberGroup.Field:
                            memberGroups[ApiMemberGroup.Field].Add(mbr);
                            break;

                        case var mbr when subsubgroup == ApiMemberGroup.AttachedProperty:
                            memberGroups[ApiMemberGroup.AttachedProperty].Add(mbr);
                            break;

                        case var mbr when subsubgroup == ApiMemberGroup.AttachedEvent:
                            memberGroups[ApiMemberGroup.AttachedEvent].Add(mbr);
                            break;

                        case var mbr when subsubgroup == ApiMemberGroup.Extension:
                            memberGroups[ApiMemberGroup.Extension].Add(mbr);
                            break;

                        default:
                            // We shouldn't get here, but just in case...
                            Debug.WriteLine("Unhandled member type Subgroup: {0} Sub-subgroup: {1}", subgroup, subsubgroup);

                            if(Debugger.IsAttached)
                                Debugger.Break();
                            break;
                    }
                }
            }
            else
                memberGroups[ApiMemberGroup.Overload].AddRange(allMembers);

            // When called for an enumeration's extension methods, ignore fields as they've already been rendered
            if(transformation.ApiMember.ApiTopicSubgroup == ApiMemberGroup.Enumeration)
                memberGroups[ApiMemberGroup.Field].Clear();

            // Render each section with at least one member
            foreach(var memberType in new[] { ApiMemberGroup.Constructor, ApiMemberGroup.Property,
                ApiMemberGroup.Method, ApiMemberGroup.Event, ApiMemberGroup.Operator, ApiMemberGroup.Field,
                ApiMemberGroup.AttachedProperty, ApiMemberGroup.AttachedEvent, ApiMemberGroup.Extension,
                ApiMemberGroup.ExplicitInterfaceImplementation, ApiMemberGroup.Overload })
            {
                var members = memberGroups[memberType];

                if(members.Count == 0)
                    continue;

                var (title, content) = transformation.CreateSection(members.First().GenerateUniqueId(), true,
                    "tableTitle_" + memberType.ToString(), null);

                transformation.CurrentElement.Add(title);
                transformation.CurrentElement.Add(content);

                var table = new XElement("table",
                        new XAttribute("id", memberType + "List"),
                        new XAttribute("class", "table is-hoverable"));

                content.Add(table);

                // Sort by EII name if present else the member name and then by template count
                foreach(var e in members.OrderBy(el => el.Element("topicdata")?.Attribute("eiiName")?.Value ??
                    el.Element("apidata")?.Attribute("name").Value ?? String.Empty).ThenBy(
                    el => el.Element("templates")?.Elements()?.Count() ?? 0))
                {
                    XElement referenceLink = new XElement("referenceLink",
                            new XAttribute("target", e.Attribute("api").Value));
                    string showParameters = (!((AvaloniaHtmlTransformation)transformation).ShowParametersOnAllMethods &&
                        transformation.ApiMember.ApiTopicSubgroup != ApiMemberGroup.Overload &&
                        e.Element("memberdata").Attribute("overload") == null &&
                        !(e.Parent.Attribute("api")?.Value ?? String.Empty).StartsWith(
                            "Overload:", StringComparison.Ordinal)) ? "false" : "true";
                    bool isExtensionMethod = e.AttributeOfType("T:System.Runtime.CompilerServices.ExtensionAttribute") != null;

                    var summaryCell = new XElement("td");

                    switch(memberType)
                    {
                        case var t when t == ApiMemberGroup.Operator &&
                          (e.Element("apidata")?.Attribute("name")?.Value == "Explicit" ||
                          e.Element("apidata")?.Attribute("name")?.Value == "Implicit"):
                            referenceLink.Add(new XAttribute("show-parameters", "true"));
                            break;

                        case var t when t == ApiMemberGroup.Operator:
                            break;

                        case var t when t == ApiMemberGroup.Extension:
                            var extensionMethod = new XElement("extensionMethod");

                            foreach(var attr in e.Attributes())
                                extensionMethod.Add(new XAttribute(attr));

                            foreach(var typeEl in new[] { e.Element("apidata"), e.Element("templates"),
                              e.Element("parameters"), e.Element("containers") })
                            {
                                if(typeEl != null)
                                    extensionMethod.Add(new XElement(typeEl));
                            }

                            referenceLink.Add(new XAttribute("display-target", "extension"),
                                new XAttribute("show-parameters", showParameters), extensionMethod);
                            break;

                        default:
                            referenceLink.Add(new XAttribute("show-parameters", showParameters));
                            break;
                    }

                    table.Add(new XElement("tr", new XElement("td", referenceLink), summaryCell));

                    var summary = e.Element("summary");

                    if(summary != null)
                        transformation.RenderChildElements(summaryCell, summary.Nodes());

                    if(transformation.ApiMember.ApiTopicSubgroup != ApiMemberGroup.Overload)
                    {
                        if(memberType == ApiMemberGroup.Extension)
                        {
                            var parameter = new XElement("parameter");

                            summaryCell.Add(new XElement("br"),
                                new XElement("include", new XAttribute("item", "definedBy"),
                                parameter));

                            transformation.RenderTypeReferenceLink(parameter, e.Element("containers").Element("type"), false);
                        }
                        else
                        {
                            if(transformation.ApiMember.TypeTopicId != e.Element("containers").Element("type").Attribute("api").Value)
                            {
                                var parameter = new XElement("parameter");

                                summaryCell.Add(new XElement("br"),
                                    new XElement("include", new XAttribute("item", "inheritedFrom"),
                                    parameter));

                                transformation.RenderTypeReferenceLink(parameter, e.Element("containers").Element("type"), false);
                            }
                            else
                            {
                                if(e.Element("overrides")?.Element("member") != null)
                                {
                                    var parameter = new XElement("parameter");

                                    summaryCell.Add(new XElement("br"),
                                        new XElement("include", new XAttribute("item", "overridesMember"),
                                        parameter));

                                    transformation.RenderTypeReferenceLink(parameter, e.Element("overrides").Element("member"), true);
                                }
                            }
                        }
                    }

                    var obsoleteAttr = e.AttributeOfType("T:System.ObsoleteAttribute");
                    var prelimComment = e.Element("preliminary");

                    if(obsoleteAttr != null || prelimComment != null)
                    {
                        if(!summaryCell.IsEmpty)
                            summaryCell.Add(new XElement("br"));

                        if(obsoleteAttr != null)
                        {
                            summaryCell.Add(new XElement("span",
                                    new XAttribute("class", "tag is-danger"),
                                new XElement("include",
                                    new XAttribute("item", "boilerplate_obsoleteShort"))));
                        }

                        if(prelimComment != null)
                        {
                            summaryCell.Add(new XElement("span",
                                    new XAttribute("class", "tag is-warning"),
                                new XElement("include",
                                    new XAttribute("item", "preliminaryShort"))));
                        }
                    }

                    if(summaryCell.IsEmpty)
                        summaryCell.Add(Element.NonBreakingSpace);
                }
            }
        }

        /// <summary>
        /// Render a section with a title and a table containing the element content
        /// </summary>
        /// <param name="transformation">The topic transformation to use</param>
        /// <param name="sectionTitleItem">The section title include item</param>
        /// <param name="sectionElements">An enumerable list of the elements to render in the table</param>
        private static void RenderApiSectionTable(TopicTransformationCore transformation, string sectionTitleItem,
          IEnumerable<XElement> sectionElements)
        {
            if(sectionElements.Any())
            {
                var (title, content) = transformation.CreateSection(sectionElements.First().GenerateUniqueId(), true,
                    sectionTitleItem, null);

                transformation.CurrentElement.Add(title);
                transformation.CurrentElement.Add(content);

                var table = new XElement("table",
                        new XAttribute("class", "table is-hoverable"));

                content.Add(table);

                foreach(var se in sectionElements)
                {
                    var descCell = new XElement("td");

                    table.Add(new XElement("tr",
                        new XElement("td",
                            new XElement("referenceLink",
                                new XAttribute("target", se.Attribute("cref")?.Value ?? String.Empty),
                                new XAttribute("qualified", "false"))),
                        descCell));

                    transformation.RenderChildElements(descCell, se.Nodes());
                }
            }
        }

        /// <summary>
        /// This is used to render the remarks section
        /// </summary>
        /// <param name="transformation">The topic transformation to use</param>
        private static void RenderApiRemarksSection(TopicTransformationCore transformation)
        {
            // For overloads, render remarks from the first overloads element.  There should only be one.
            if(transformation.ApiMember.ApiTopicSubgroup != ApiMemberGroup.Overload)
                transformation.RenderNode(transformation.CommentsNode.Element("remarks"));
            else
            {
                var overloads = transformation.ReferenceNode.Descendants("overloads").FirstOrDefault();

                if(overloads != null)
                    transformation.RenderNode(overloads.Element("remarks"));
            }
        }

        /// <summary>
        /// This is used to render the examples section
        /// </summary>
        /// <param name="transformation">The topic transformation to use</param>
        private static void RenderApiExamplesSection(TopicTransformationCore transformation)
        {
            // For overloads, render examples from the overloads element.  There should only be one.
            if(transformation.ApiMember.ApiTopicSubgroup != ApiMemberGroup.Overload)
                transformation.RenderNode(transformation.CommentsNode.Element("example"));
            else
            {
                var overloads = transformation.ReferenceNode.Descendants("overloads").FirstOrDefault();

                if(overloads != null)
                    transformation.RenderNode(overloads.Element("example"));
            }
        }

        /// <summary>
        /// This is used to render the versions section
        /// </summary>
        /// <param name="transformation">The topic transformation to use</param>
        private static void RenderApiVersionsSection(TopicTransformationCore transformation)
        {
            // Only API member pages get version information
            if(transformation.ApiMember.ApiTopicGroup != ApiMemberGroup.List &&
               transformation.ApiMember.ApiTopicGroup != ApiMemberGroup.RootGroup &&
               transformation.ApiMember.ApiTopicGroup != ApiMemberGroup.Root &&
               transformation.ApiMember.ApiTopicGroup != ApiMemberGroup.NamespaceGroup &&
               transformation.ApiMember.ApiTopicGroup != ApiMemberGroup.Namespace)
            {
                foreach(var v in transformation.ReferenceNode.Elements("versions"))
                    transformation.RenderNode(v);
            }
        }

        /// <summary>
        /// Render the revision history section if applicable
        /// </summary>
        /// <param name="transformation">The topic transformation to use</param>
        private static void RenderApiRevisionHistorySection(TopicTransformationCore transformation)
        {
            var revisionHistory = transformation.CommentsNode.Element("revisionHistory");

            if(revisionHistory == null || revisionHistory.Attribute("visible")?.Value == "false")
                return;

            var revisions = revisionHistory.Elements("revision").Where(
                h => h.Attribute("visible")?.Value != "false");

            if(revisions.Any())
            {
                var (title, content) = transformation.CreateSection(revisionHistory.GenerateUniqueId(), true,
                    "title_revisionHistory", null);

                transformation.CurrentElement.Add(title);
                transformation.CurrentElement.Add(content);

                var table = new XElement("table",
                        new XAttribute("class", "table is-hoverable"),
                    new XElement("thead",
                        new XElement("tr",
                            new XElement("th",
                                new XElement("include", new XAttribute("item", "header_revHistoryDate"))),
                            new XElement("th",
                                new XElement("include", new XAttribute("item", "header_revHistoryVersion"))),
                            new XElement("th",
                                new XElement("include", new XAttribute("item", "header_revHistoryDescription"))))));

                content.Add(table);

                foreach(var rh in revisions)
                {
                    var descCell = new XElement("td");

                    table.Add(new XElement("tr",
                        new XElement("td", rh.Attribute("date")?.Value),
                        new XElement("td", rh.Attribute("version")?.Value),
                        descCell));

                    transformation.RenderChildElements(descCell, rh.Nodes());
                }
            }
        }

        /// <summary>
        /// Render the bibliography section if applicable
        /// </summary>
        /// <param name="transformation">The topic transformation to use</param>
        private static void RenderApiBibliographySection(TopicTransformationCore transformation)
        {
            if(transformation.ElementHandlerFor("bibliography") is BibliographyElement b)
            {
                if(b.DetermineCitations(transformation).Count != 0)
                {
                    // Use the first citation element as the element for rendering.  It's only needed to create
                    // a unique ID for the section.
                    var cite = transformation.DocumentNode.Descendants("cite").First();

                    b.Render(transformation, cite);
                }
            }
        }

        /// <summary>
        /// This renders the See Also section if applicable
        /// </summary>
        /// <param name="transformation">The topic transformation to use</param>
        private static void RenderApiSeeAlsoSection(TopicTransformationCore transformation)
        {
            // Render the see and seealso links using the see element handler as the processing is the same
            Element seeHandler = transformation.ElementHandlerFor("see"),
                conceptualLinkHandler = transformation.ElementHandlerFor("conceptualLink");

            // Get see also elements from comments excluding those in overloads comments
            List<XElement> seeAlsoNotInOverloads = transformation.CommentsNode.Descendants("seealso").Where(
                    s => !s.Ancestors("overloads").Any()).ToList(),
                seeAlsoHRef = seeAlsoNotInOverloads.Where(s => s.Attribute("href") != null).ToList(),
                seeAlsoCRef = seeAlsoNotInOverloads.Except(seeAlsoHRef).ToList();

            // Combine those with see also elements from element overloads comments
            var elements = transformation.ReferenceNode.Element("elements") ?? new XElement("elements");
            var elementOverloads = elements.Elements("element").SelectMany(e => e.Descendants("overloads")).ToList();

            seeAlsoHRef.AddRange(elementOverloads.Descendants("seealso").Where(s => s.Attribute("href") != null));
            seeAlsoCRef.AddRange(elementOverloads.Descendants("seealso").Where(s => s.Attribute("href") == null));

            // Get unique conceptual links from comments excluding those in overloads comments and combine them
            // with those in element overloads comments.
            var conceptualLinks = transformation.CommentsNode.Descendants("conceptualLink").Where(
                s => !s.Ancestors("overloads").Any()).Concat(
                    elementOverloads.Descendants("conceptualLink")).GroupBy(
                        c => c.Attribute("target")?.Value ?? String.Empty).Where(g => g.Key.Length != 0).Select(
                        g => g.First()).ToList();

            if(seeAlsoCRef.Count != 0 || seeAlsoHRef.Count != 0 || conceptualLinks.Count != 0 ||
              transformation.ApiMember.ApiTopicGroup == ApiMemberGroup.Type ||
              transformation.ApiMember.ApiTopicGroup == ApiMemberGroup.Member ||
              transformation.ApiMember.ApiTopicGroup == ApiMemberGroup.List)
            {
                // This has a fixed ID that matches the one used in MAML topics for the related topics section
                var (title, content) = transformation.CreateSection("seeAlso", true, "title_relatedTopics", null);

                transformation.CurrentElement.Add(title);
                transformation.CurrentElement.Add(content);

                var priorCurrentElement = transformation.CurrentElement;

                if(seeAlsoCRef.Count != 0 || transformation.ApiMember.ApiTopicGroup == ApiMemberGroup.Type ||
                  transformation.ApiMember.ApiTopicGroup == ApiMemberGroup.Member ||
                  transformation.ApiMember.ApiTopicGroup == ApiMemberGroup.List)
                {
                    var (subtitle, subsection) = transformation.CreateSubsection(true, "title_seeAlso_reference");

                    if(subtitle != null)
                        content.Add(subtitle);

                    if(subsection != null)
                        content.Add(subsection);
                    else
                        subsection = content;

                    RenderApiAutoGeneratedSeeAlsoLinks(transformation, subsection);

                    if(seeHandler != null)
                    {
                        foreach(var s in seeAlsoCRef)
                        {
                            var div = new XElement("div");
                            subsection.Add(div);

                            transformation.CurrentElement = div;
                            seeHandler.Render(transformation, s);
                        }
                    }
                }

                if((seeAlsoHRef.Count != 0 && seeHandler != null) || (conceptualLinks.Count != 0 &&
                  conceptualLinkHandler != null))
                {
                    var (subtitle, subsection) = transformation.CreateSubsection(true, "title_seeAlso_otherResources");

                    if(subtitle != null)
                        content.Add(subtitle);

                    if(subsection != null)
                        content.Add(subsection);
                    else
                        subsection = content;

                    if(seeHandler != null)
                    {
                        foreach(var s in seeAlsoHRef)
                        {
                            var div = new XElement("div");
                            subsection.Add(div);

                            transformation.CurrentElement = div;
                            seeHandler.Render(transformation, s);
                        }
                    }

                    if(conceptualLinkHandler != null)
                    {
                        foreach(var c in conceptualLinks)
                        {
                            var div = new XElement("div");
                            subsection.Add(div);

                            transformation.CurrentElement = div;
                            conceptualLinkHandler.Render(transformation, c);
                        }
                    }
                }

                transformation.CurrentElement = priorCurrentElement;
            }
        }

        /// <summary>
        /// Render auto-generated see also section links based on the API topic
        /// </summary>
        /// <param name="transformation">The topic transformation to use</param>
        /// <param name="subsection">The subsection to which the links are added</param>
        private static void RenderApiAutoGeneratedSeeAlsoLinks(TopicTransformationCore transformation,
          XElement subsection)
        {
            // Add a link to the containing type on all list and member topics
            if(transformation.ApiMember.ApiTopicGroup == ApiMemberGroup.Member ||
              transformation.ApiMember.ApiTopicGroup == ApiMemberGroup.List)
            {
                subsection.Add(new XElement("div",
                    new XElement("referenceLink",
                        new XAttribute("target", transformation.ApiMember.TypeTopicId),
                        new XAttribute("display-target", "format"),
                        new XElement("include",
                            new XAttribute("item", "boilerplate_seeAlsoTypeLink"),
                            new XElement("parameter", "{0}"),
                            new XElement("parameter", transformation.ApiMember.TypeApiSubgroup)))));
            }

            // Add a link to the overload topic
            if(!String.IsNullOrWhiteSpace(transformation.ApiMember.OverloadTopicId))
            {
                subsection.Add(new XElement("div",
                    new XElement("referenceLink",
                        new XAttribute("target", transformation.ApiMember.OverloadTopicId),
                        new XAttribute("display-target", "format"),
                        new XAttribute("show-parameters", "false"),
                        new XElement("include",
                            new XAttribute("item", "boilerplate_seeAlsoOverloadLink"),
                            new XElement("parameter", "{0}")))));
            }

            // Add a link to the namespace topic
            string namespaceId = transformation.ReferenceNode.Element("containers")?.Element("namespace")?.Attribute("api")?.Value;

            if(!String.IsNullOrWhiteSpace(namespaceId))
            {
                subsection.Add(new XElement("div",
                    new XElement("referenceLink",
                        new XAttribute("target", namespaceId),
                        new XAttribute("display-target", "format"),
                        new XElement("include",
                            new XAttribute("item", "boilerplate_seeAlsoNamespaceLink"),
                            new XElement("parameter", "{0}")))));
            }
        }

        /// <summary>
        /// Render the In This Article quick links menu items
        /// </summary>
        private void RenderInThisArticleMenu(XElement body)
        {
            var inThisArticleMenu = body.Descendants("ul").FirstOrDefault(
                u => u.Attribute("id")?.Value == "InThisArticleMenu");

            if(inThisArticleMenu == null || this.AlwaysShowTopLevelAutoOutline)
                return;

            var parent = inThisArticleMenu;
            string lastElementName = null;
            var linkToHeaderMap = new Dictionary<XElement, XElement>();

            // h2 and h3 are treated as top-level sections.  h4 is treated as a subsection
            foreach(var header in body.Descendants().Where(d => d.Name.LocalName == "h2" ||
              d.Name.LocalName == "h3" || d.Name.LocalName == "h4"))
            {
                if((lastElementName == "h4" && lastElementName != header.Name.LocalName) ||
                  (header.Name.LocalName == "h4" && lastElementName != "h4"))
                {
                    // Returning to the prior level or nesting?
                    if(String.Compare(header.Name.LocalName, lastElementName, StringComparison.Ordinal) < 0)
                        parent = parent.Parent.Parent;
                    else
                    {
                        if(parent.Elements().Any())
                            parent = parent.Elements().Last();

                        var ul = new XElement("ul", new XAttribute("class", "menu"));
                        
                        parent.Add(ul);
                        parent = ul;
                    }
                }

                // The header may contain literal text or an include item at this point
                var include = header.Descendants("include").FirstOrDefault();
                XElement li;

                if(include != null)
                {
                    li = new XElement("li",
                        new XElement("a",
                            new XAttribute("class", "quickLink"),
                            new XElement(include)));
                }
                else
                {
                    li = new XElement("li",
                        new XElement("a",
                            new XAttribute("class", "quickLink"),
                            header.Value.NormalizeWhiteSpace()));
                }

                parent.Add(li);
                linkToHeaderMap.Add(li, header);

                lastElementName = header.Name.LocalName;
            }

            // Remove submenus with only one entry.  Typically this is the See Also/Reference subsection entry.
            foreach(var ul in inThisArticleMenu.Descendants("ul").ToList())
                if(ul.Elements().Count() == 1)
                    ul.Remove();

            // Tag all headers related to the remaining list item elements so they can be found by the script to
            // get their locations.
            bool hasLinks = false;

            foreach(var li in inThisArticleMenu.Descendants("li"))
            {
                if(linkToHeaderMap.TryGetValue(li, out XElement h))
                {
                    if(h.Attribute("class") != null)
                        h.Attribute("class").Value += " quickLinkHeader";
                    else
                        h.Add(new XAttribute("class", "quickLinkHeader"));

                    hasLinks = true;
                }
            }

            if(hasLinks)
                this.RegisterStartupScript(500, "InitializeQuickLinks();");
        }
        #endregion
    }
}
