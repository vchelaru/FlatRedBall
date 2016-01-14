using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using System.Xml.Serialization;

namespace FlatRedBall.Glue.Plugins.Rss
{
    public class RssItem
    {
        //<title>Tiled Map Editor tmx to scnx|nntx|shcx Glue integration toolkit</title>
            //<link>http://www.gluevault.com/plug/44-tiled-map-editor-tmx-scnxnntxshcx-glue-integration-toolkit</link>
            //<description>f=&quot;http://www.mapeditor.org/&quot;&gt</description>
            //<comments>http://www.gluevault.com/plug/44-tiled-map-editor-tmx-scnxnntxshcx-glue-integration-toolkit#comments</comments>
            //<category domain="http://www.gluevault.com/file-type/plug">Plug In</category>
            //<category domain="http://www.gluevault.com/flatredball/recommended">Recommended</category>
            //<pubDate>Fri, 10 Feb 2012 05:59:44 +0000</pubDate>
            //<dc:creator>kainazzzo</dc:creator>
        //<guid isPermaLink="false">44 at http://www.gluevault.com</guid>
        #region Properties

        [XmlElement("Title")]
        public string Title { get; set; }

        [XmlElement("description")]
        public string Description { get; set; }

        [XmlElement("comments")]
        public string Comments { get; set; }

        [XmlElement("pubDate")]
        public string PublishedDate { get; set; }

        public string DirectLink
        {
            get;
            set;
        }

        [XmlIgnore]
        public string GlueLink
        {
            get
            {
                int startIndex = Description.LastIndexOf("<a href=\"http://www.gluevault.com/plug/") + 
                    "<a href=\"".Length;

                int endIndex = Description.IndexOf("\"", startIndex);

                string substring = Description.Substring(startIndex, endIndex - startIndex);
                return substring;
            }
        }
        #endregion

        public static RssItem FromXElement(XElement xElement)
        {
            RssItem newItem = new RssItem();

            var subElement = xElement.Element("title");
            if (subElement != null)
            {
                newItem.Title = subElement.Value;
            }

            subElement = ParseDescription(xElement, newItem, subElement);

            subElement = xElement.Element("comments");
            if (subElement != null)
            {
                newItem.Comments = subElement.Value;
            }

            subElement = xElement.Element("pubDate");
            if (subElement != null)
            {
                newItem.PublishedDate = xElement.Element("pubDate").Value;
            }
            return newItem;
        }

        private static XElement ParseDescription(XElement xElement, RssItem newItem, XElement subElement)
        {
            subElement = xElement.Element("description");
            if (subElement != null)
            {
                newItem.Description = subElement.Value;

                int indexOfComment = newItem.Description.IndexOf("<!--");

                if (indexOfComment != -1)
                {
                    string commentSection = newItem.Description.Substring(indexOfComment);

                    int indexOfLinkStart = commentSection.IndexOf("/><a href=\"http://www.gluevault.com/system/files/");
                    if (indexOfLinkStart != -1)
                    {
                        indexOfLinkStart = indexOfLinkStart + "/><a href=\"".Length;

                        string stringContainingFileAtStart = commentSection.Substring(indexOfLinkStart);

                        int endOfFile = stringContainingFileAtStart.IndexOf("\"");

                        string file = stringContainingFileAtStart.Substring(0, endOfFile);

                        newItem.DirectLink = file;
                    }
                }
            }
            return subElement;
        }

        public override string ToString()
        {
            return Title;
        }
    }
}
