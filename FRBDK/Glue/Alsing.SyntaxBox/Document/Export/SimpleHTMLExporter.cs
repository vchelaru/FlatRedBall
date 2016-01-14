using System;
using System.Drawing;
using System.Text;

namespace Alsing.SourceCode.SyntaxDocumentExporters
{
    public class SimpleHTMLExporter
    {
        private static StringBuilder sb;

        private static void write(string text, TextStyle s)
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

        private static void Out(string text)
        {
            sb.Append(text);
        }

        public static string Export(SyntaxDocument doc, string CssClass)
        {
            sb = new StringBuilder();
            doc.ParseAll(true);

            Out("<div class=\"" + CssClass + "\">" + Environment.NewLine);
            foreach (Row r in doc)
            {
                foreach (Word w in r)
                {
                    write(w.Text, w.Style);
                }

                Out("<br>" + Environment.NewLine);
            }
            Out("</div>");

            return sb.ToString();
        }
    }
}