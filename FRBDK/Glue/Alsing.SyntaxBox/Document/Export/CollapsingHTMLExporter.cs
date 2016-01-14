// *
// * Copyright (C) 2008 Roger Alsing : http://www.RogerAlsing.com
// *
// * This library is free software; you can redistribute it and/or modify it
// * under the terms of the GNU Lesser General Public License 2.1 or later, as
// * published by the Free Software Foundation. See the included license.txt
// * or http://www.gnu.org/copyleft/lesser.html for details.
// *
// *

using System;
using System.Drawing;
using System.Globalization;
using System.Text;

namespace Alsing.SourceCode.SyntaxDocumentExporters
{
    /// <summary>
    /// Html exporter class
    /// </summary>
    public class CollapsingHTMLExporter
    {
        private StringBuilder sb;


        /// <summary>
        /// Exports the content of a SyntaxDocument to a HTML formatted string
        /// </summary>
        /// <param name="doc">SyntaxDocument object to export from</param>
        /// <param name="ImagePath">File path tho the images to use in the HTML string</param>
        /// <returns></returns>
        public string Export(SyntaxDocument doc, string ImagePath)
        {
            return Export(doc, Color.Transparent, ImagePath, "");
        }


        /// <summary>
        /// Exports the content of a SyntaxDocument to a HTML formatted string
        /// </summary>
        /// <param name="doc">SyntaxDocument object to export from</param>
        /// <param name="BGColor">HTML color string to use as background color</param>
        /// <param name="ImagePath">File path tho the images to use in the HTML string</param>
        /// <param name="Style">HTML style string that should be applied to the output</param>
        /// <returns></returns>
        public string Export(SyntaxDocument doc, Color BGColor,
                             string ImagePath, string Style)
        {
            sb = new StringBuilder();
            doc.ParseAll(true);
            int i = 0;

            string guid = DateTime.Now.Ticks.ToString
                (CultureInfo.InvariantCulture);

            //style=\"font-family:courier new;font-size:13px;\"
            if (BGColor.A == 0)
                Out("<table  ><tr><td nowrap><div style=\"" + Style + "\">");
            else
                Out("<table  style=\"background-color:" + GetHTMLColor(BGColor) + ";" +
                    Style + "\"><tr><td nowrap><div>");
            foreach (Row r in doc)
            {
                i++;
                if (r.CanFold)
                {
                    RenderCollapsed(r.VirtualCollapsedRow, r, i, ImagePath, guid);
                    Out("<div style=\"display:block;\" id=\"open" + guid + "_" +
                        i.ToString(CultureInfo.InvariantCulture) +
                        "\">");

                    string img = "minus.gif";
                    try
                    {
                        if (r.expansion_StartSpan.Parent.Parent == null)
                            img = "minusNoTopLine.gif";
                    }
                    catch {}
                    Out("<img src=\"" + ImagePath + img + "\" align=top onclick=\"open" +
                        guid + "_" + i.ToString
                                         (CultureInfo.InvariantCulture) +
                        ".style.display='none'; closed" + guid + "_" + i.ToString
                                                                           (CultureInfo.InvariantCulture) +
                        ".style.display='block'; \">");
                }
                else
                {
                    if (r.CanFoldEndPart)
                    {
                        Out("<img src=\"" + ImagePath + "L.gif\"  align=top>");
                    }
                    else
                    {
                        if (r.HasExpansionLine)
                        {
                            Out("<img src=\"" + ImagePath + "I.gif\"  align=top>");
                        }
                        else
                        {
                            Out("<img src=\"" + ImagePath + "clear.gif\"  align=top>");
                        }
                    }
                }
                foreach (Word w in r)
                {
                    write(w.Text, w.Style);
                }
                if (r.CanFoldEndPart)
                    Out("</div>\n");
                else
                    Out("<br>\n");
            }
            Out("</div></td></tr></table>");

            return sb.ToString();
        }


        private void RenderCollapsed(Row r, Row TrueRow, int i, string ImagePath,
                                     string guid)
        {
            Out("<div style=\"display:none;\" id=\"closed" + guid + "_" + i.ToString
                                                                              (CultureInfo.InvariantCulture) + "\">");
            string img = "plus.gif";
            try
            {
                if (TrueRow.expansion_StartSpan.Parent.Parent == null)
                    img = "PlusNoLines.gif";
            }
            catch {}


            Out("<img src=\"" + ImagePath + img + "\" align=top onclick=\"open" +
                guid + "_" + i.ToString
                                 (CultureInfo.InvariantCulture) +
                ".style.display='block'; closed" + guid + "_" + i.ToString
                                                                    (CultureInfo.InvariantCulture) +
                ".style.display='none'; \">");

            foreach (Word w in r)
            {
                write(w.Text, w.Style);
            }

            Out("</div>");
        }

        private void write(string text, TextStyle s)
        {
            if (s != null)
            {
                if (s.Bold)
                    Out("<b>");
                if (s.Italic)
                    Out("<i>");
                if (s.Transparent)
                    Out("<span style=\"color:" + GetHTMLColor(s.ForeColor) + "\">");
                else
                    Out("<span style=\"color:" + GetHTMLColor(s.ForeColor) +
                        ";background-color:" + GetHTMLColor(s.BackColor) + ";\">");
            }

            text = text.Replace("&", "&amp;");
            text = text.Replace("<", "&lt;");
            text = text.Replace(">", "&gt;");
            text = text.Replace(" ", "&nbsp;");
            text = text.Replace("\t", "&nbsp;&nbsp;&nbsp;&nbsp;");
            Out(text);

            if (s != null)
            {
                Out("</span>");

                if (s.Italic)
                    Out("</i>");
                if (s.Bold)
                    Out("</b>");
            }
        }

        private static string GetHTMLColor(Color c)
        {
            return string.Format("#{0}{1}{2}", c.R.ToString("x2"), c.G.ToString("x2"),
                                 c.B.ToString("x2"));
        }

        private void Out(string text)
        {
            sb.Append(text);
        }
    }
}