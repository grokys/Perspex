using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Sandcastle.Core;
using Sandcastle.Core.PresentationStyle.Transformation;
using Sandcastle.Core.PresentationStyle.Transformation.Elements;

namespace SpecialAttributeRendererPlugin
{
    public class CustomTag : Element
    {
        /// <summary>
        /// Constructor
        /// </summary>
        public CustomTag(string content) : base("customTag")
        {
            Content = content;
        }

        public string Content { get; }

        public override void Render(TopicTransformationCore transformation, XElement element)
        {
            if (transformation == null)
                throw new ArgumentNullException(nameof(transformation));

            if (element == null)
                throw new ArgumentNullException(nameof(element));

            // Rendering can be adjusted based on the help file format
            switch (transformation.SupportedFormats)
            {
                case HelpFileFormats.OpenXml:
                case HelpFileFormats.Markdown:
                    // No custom formatting support for these so just render the element content
                    transformation.RenderChildElements(transformation.CurrentElement, element.Nodes());
                    break;

                default:
                    // Help 1/Website
                    var span = new XElement("span",
                        new XAttribute("class", element.Attribute("style")?.Value ?? "Style1"));

                    transformation.CurrentElement.Add(span);
                    transformation.RenderChildElements(span, element.Nodes());
                    
                    transformation.RenderChildElements(span, [new XText(Content)]);
                    break;
            }
        }
    }
}
