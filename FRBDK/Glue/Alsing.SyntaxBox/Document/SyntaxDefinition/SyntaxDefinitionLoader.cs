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
using System.Collections;
using System.Drawing;
using System.Xml;

namespace Alsing.SourceCode
{
    /// <summary>
    /// 
    /// </summary>
    public class SyntaxDefinitionLoader
    {
        private Hashtable spanDefinitionLookup = new Hashtable();
        private Hashtable styleLookup = new Hashtable();
        private SyntaxDefinition syntaxDefinition = new SyntaxDefinition();


        /// <summary>
        /// Load a specific syntax file
        /// </summary>
        /// <param name="File">File name</param>
        /// <returns>SyntaxDefinition object</returns>
        public SyntaxDefinition Load(string File)
        {
            styleLookup = new Hashtable();
            spanDefinitionLookup = new Hashtable();
            syntaxDefinition = new SyntaxDefinition();

            var xmlDocument = new XmlDocument();
            xmlDocument.Load(File);
            ReadLanguageDefinition(xmlDocument);

            return syntaxDefinition;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="File"></param>
        /// <param name="Separators"></param>
        /// <returns></returns>
        public SyntaxDefinition Load(string File, string Separators)
        {
            styleLookup = new Hashtable();
            spanDefinitionLookup = new Hashtable();
            syntaxDefinition = new SyntaxDefinition {Separators = Separators};

            var xmlDocument = new XmlDocument();
            xmlDocument.Load(File);
            ReadLanguageDefinition(xmlDocument);

            if (syntaxDefinition.mainSpanDefinition == null)
            {
                throw new Exception("no main block found in syntax");
            }

            return syntaxDefinition;
        }

        /// <summary>
        /// Load a specific syntax from an xml string
        /// </summary>
        /// <param name="XML"></param>
        /// <returns></returns>
        public SyntaxDefinition LoadXML(string XML)
        {
            styleLookup = new Hashtable();
            spanDefinitionLookup = new Hashtable();
            syntaxDefinition = new SyntaxDefinition();

            var xmlDocument = new XmlDocument();
            xmlDocument.LoadXml(XML);
            ReadLanguageDefinition(xmlDocument);

            if (syntaxDefinition.mainSpanDefinition == null)
            {
                throw new Exception("no main block found in syntax");
            }


            return syntaxDefinition;
        }

        private void ReadLanguageDefinition(XmlNode xml)
        {
            ParseLanguage(xml["Language"]);
        }

        private void ParseLanguage(XmlNode node)
        {
            //get syntax name and startblock
            string Name = "";
            string StartBlock = "";

            foreach (XmlAttribute att in node.Attributes)
            {
                if (att.Name.ToLowerInvariant() == "name")
                    Name = att.Value;

                if (att.Name.ToLowerInvariant() == "startblock")
                    StartBlock = att.Value;
            }

            syntaxDefinition.Name = Name;
            syntaxDefinition.mainSpanDefinition = GetBlock(StartBlock);

            foreach (XmlNode n in node.ChildNodes)
            {
                if (n.NodeType == XmlNodeType.Element)
                {
                    if (n.Name.ToLowerInvariant() == "filetypes")
                        ParseFileTypes(n);
                    if (n.Name.ToLowerInvariant() == "block")
                        ParseBlock(n);
                    if (n.Name.ToLowerInvariant() == "style")
                        ParseStyle(n);
                }
            }
        }

        private void ParseFileTypes(XmlNode node)
        {
            foreach (XmlNode n in node.ChildNodes)
            {
                if (n.NodeType == XmlNodeType.Element)
                {
                    if (n.Name.ToLowerInvariant() == "filetype")
                    {
                        //add filetype
                        string Extension = "";
                        string Name = "";
                        foreach (XmlAttribute a in n.Attributes)
                        {
                            if (a.Name.ToLowerInvariant() == "name")
                                Name = a.Value;
                            if (a.Name.ToLowerInvariant() == "extension")
                                Extension = a.Value;
                        }
                        var ft = new FileType {Extension = Extension, Name = Name};
                        syntaxDefinition.FileTypes.Add(ft);
                    }
                }
            }
        }

        private void ParseBlock(XmlNode node)
        {
            string Name = "", Style = "";
            bool IsMultiline = false;
            bool TerminateChildren = false;
            Color BackColor = Color.Transparent;
            foreach (XmlAttribute att in node.Attributes)
            {
                if (att.Name.ToLowerInvariant() == "name")
                    Name = att.Value;
                if (att.Name.ToLowerInvariant() == "style")
                    Style = att.Value;
                if (att.Name.ToLowerInvariant() == "ismultiline")
                    IsMultiline = bool.Parse(att.Value);
                if (att.Name.ToLowerInvariant() == "terminatechildren")
                    TerminateChildren = bool.Parse(att.Value);
                if (att.Name.ToLowerInvariant() == "backcolor")
                {
                    BackColor = Color.FromName(att.Value);
                    //Transparent =false;
                }
            }

            //create block object here
            SpanDefinition bl = GetBlock(Name);
            bl.BackColor = BackColor;
            bl.Name = Name;
            bl.MultiLine = IsMultiline;
            bl.Style = GetStyle(Style);
            bl.TerminateChildren = TerminateChildren;


            foreach (XmlNode n in node.ChildNodes)
            {
                if (n.NodeType == XmlNodeType.Element)
                {
                    if (n.Name.ToLowerInvariant() == "scope")
                    {
                        //bool IsComplex=false;
                        //bool IsSeparator=false;
                        string Start = "";
                        string End = "";
                        string style = "";
                        string text = "";
                        string EndIsSeparator = "";
                        string StartIsComplex = "false";
                        string EndIsComplex = "false";
                        string StartIsKeyword = "false";
                        string EndIsKeyword = "false";
                        string spawnStart = "";
                        string spawnEnd = "";
                        string EscapeChar = "";
                        string CauseIndent = "false";

                        bool expanded = true;

                        foreach (XmlAttribute att in n.Attributes)
                        {
                            switch (att.Name.ToLowerInvariant())
                            {
                                case "start":
                                    Start = att.Value;
                                    break;
                                case "escapechar":
                                    EscapeChar = att.Value;
                                    break;
                                case "end":
                                    End = att.Value;
                                    break;
                                case "style":
                                    style = att.Value;
                                    break;
                                case "text":
                                    text = att.Value;
                                    break;
                                case "defaultexpanded":
                                    expanded = bool.Parse(att.Value);
                                    break;
                                case "endisseparator":
                                    EndIsSeparator = att.Value;
                                    break;
                                case "startiskeyword":
                                    StartIsKeyword = att.Value;
                                    break;
                                case "startiscomplex":
                                    StartIsComplex = att.Value;
                                    break;
                                case "endiscomplex":
                                    EndIsComplex = att.Value;
                                    break;
                                case "endiskeyword":
                                    EndIsKeyword = att.Value;
                                    break;
                                case "spawnblockonstart":
                                    spawnStart = att.Value;
                                    break;
                                case "spawnblockonend":
                                    spawnEnd = att.Value;
                                    break;
                                case "causeindent":
                                    CauseIndent = att.Value;
                                    break;
                            }
                        }
                        if (Start != "")
                        {
                            //bl.StartPattern =new Pattern (Pattern,IsComplex,false,IsSeparator);
                            //bl.StartPatterns.Add (new Pattern (Pattern,IsComplex,IsSeparator,true));
                            bool blnStartIsComplex = bool.Parse(StartIsComplex);
                            bool blnEndIsComplex = bool.Parse(EndIsComplex);
                            bool blnCauseIndent = bool.Parse(CauseIndent);

                            var scope = new Scope {Style = GetStyle(style), ExpansionText = text, DefaultExpanded = expanded, CauseIndent = blnCauseIndent};

                            var StartP = new Pattern(Start, blnStartIsComplex, false, bool.Parse(StartIsKeyword));
                            Pattern endPattern = EscapeChar != "" ? new Pattern(End, false, bool.Parse(EndIsKeyword), EscapeChar) : new Pattern(End, blnEndIsComplex, false, bool.Parse(EndIsKeyword));

                            if (EndIsSeparator != "")
                                endPattern.IsSeparator = bool.Parse(EndIsSeparator);

                            scope.Start = StartP;
                            scope.EndPatterns.Add(endPattern);
                            bl.ScopePatterns.Add(scope);
                            if (spawnStart != "")
                            {
                                scope.spawnSpanOnStart = GetBlock(spawnStart);
                            }
                            if (spawnEnd != "")
                            {
                                scope.spawnSpanOnEnd = GetBlock(spawnEnd);
                            }
                        }
                    }
                    if (n.Name.ToLowerInvariant() == "bracket")
                    {
                        //bool IsComplex=false;
                        //bool IsSeparator=false;
                        string Start = "";
                        string End = "";
                        string style = "";

                        string StartIsComplex = "false";
                        string EndIsComplex = "false";

                        string StartIsKeyword = "false";
                        string EndIsKeyword = "false";
                        string IsMultiLineB = "true";

                        foreach (XmlAttribute att in n.Attributes)
                        {
                            switch (att.Name.ToLowerInvariant())
                            {
                                case "start":
                                    Start = att.Value;
                                    break;
                                case "end":
                                    End = att.Value;
                                    break;
                                case "style":
                                    style = att.Value;
                                    break;
                                case "endisseparator":
                                    if (att.Name.ToLowerInvariant() == "startisseparator")
                                        if (att.Name.ToLowerInvariant() == "startiskeyword")
                                            StartIsKeyword = att.Value;
                                    break;
                                case "startiscomplex":
                                    StartIsComplex = att.Value;
                                    break;
                                case "endiscomplex":
                                    EndIsComplex = att.Value;
                                    break;
                                case "endiskeyword":
                                    EndIsKeyword = att.Value;
                                    break;
                                case "ismultiline":
                                    IsMultiLineB = att.Value;
                                    break;
                            }
                        }
                        if (Start != "")
                        {
                            var pl = new PatternList {Style = GetStyle(style)};

                            bool blnStartIsComplex = bool.Parse(StartIsComplex);
                            bool blnEndIsComplex = bool.Parse(EndIsComplex);
                            bool blnIsMultiLineB = bool.Parse(IsMultiLineB);

                            var StartP = new Pattern(Start, blnStartIsComplex, false, bool.Parse(StartIsKeyword));
                            var EndP = new Pattern(End, blnEndIsComplex, false, bool.Parse(EndIsKeyword));

                            StartP.MatchingBracket = EndP;
                            EndP.MatchingBracket = StartP;
                            StartP.BracketType = BracketType.StartBracket;
                            EndP.BracketType = BracketType.EndBracket;
                            StartP.IsMultiLineBracket = EndP.IsMultiLineBracket = blnIsMultiLineB;

                            pl.Add(StartP);
                            pl.Add(EndP);
                            bl.OperatorsList.Add(pl);
                        }
                    }
                }

                if (n.Name.ToLowerInvariant() == "keywords")
                    foreach (XmlNode cn in n.ChildNodes)
                    {
                        if (cn.Name.ToLowerInvariant() == "patterngroup")
                        {
                            var pl = new PatternList();
                            bl.KeywordsList.Add(pl);
                            foreach (XmlAttribute att in cn.Attributes)
                            {
                                switch (att.Name.ToLowerInvariant())
                                {
                                    case "style":
                                        pl.Style = GetStyle(att.Value);
                                        break;
                                    case "name":
                                        pl.Name = att.Value;
                                        break;
                                    case "normalizecase":
                                        pl.NormalizeCase = bool.Parse(att.Value);
                                        break;
                                    case "casesensitive":
                                        pl.CaseSensitive = bool.Parse(att.Value);
                                        break;
                                }
                            }
                            foreach (XmlNode pt in cn.ChildNodes)
                            {
                                if (pt.Name.ToLowerInvariant() == "pattern")
                                {
                                    bool IsComplex = false;
                                    bool IsSeparator = false;
                                    string Category = null;
                                    string Pattern = "";
                                    if (pt.Attributes != null)
                                    {
                                        foreach (XmlAttribute att in pt.Attributes)
                                        {
                                            switch (att.Name.ToLowerInvariant())
                                            {
                                                case "text":
                                                    Pattern = att.Value;
                                                    break;
                                                case "iscomplex":
                                                    IsComplex = bool.Parse(att.Value);
                                                    break;
                                                case "isseparator":
                                                    IsSeparator = bool.Parse(att.Value);
                                                    break;
                                                case "category":
                                                    Category = (att.Value);
                                                    break;
                                            }
                                        }
                                    }
                                    if (Pattern != "")
                                    {
                                        var pat = new Pattern(Pattern, IsComplex, IsSeparator, true) {Category = Category};
                                        pl.Add(pat);
                                    }
                                }
                                else if (pt.Name.ToLowerInvariant() == "patterns")
                                {
                                    string Patterns = pt.ChildNodes[0].Value;
                                    Patterns = Patterns.Replace("\t", " ");
                                    while (Patterns.IndexOf("  ") >= 0)
                                        Patterns = Patterns.Replace("  ", " ");


                                    foreach (string Pattern in Patterns.Split())
                                    {
                                        if (Pattern != "")
                                            pl.Add(new Pattern(Pattern, false, false, true));
                                    }
                                }
                            }
                        }
                    }
                //if (n.Name == "Operators")
                //	ParseStyle(n);
                if (n.Name.ToLowerInvariant() == "operators")
                    foreach (XmlNode cn in n.ChildNodes)
                    {
                        if (cn.Name.ToLowerInvariant() == "patterngroup")
                        {
                            var pl = new PatternList();
                            bl.OperatorsList.Add(pl);
                            foreach (XmlAttribute att in cn.Attributes)
                            {
                                switch (att.Name.ToLowerInvariant())
                                {
                                    case "style":
                                        pl.Style = GetStyle(att.Value);
                                        break;
                                    case "name":
                                        pl.Name = att.Value;
                                        break;
                                    case "normalizecase":
                                        pl.NormalizeCase = bool.Parse(att.Value);
                                        break;
                                    case "casesensitive":
                                        pl.CaseSensitive = bool.Parse(att.Value);
                                        break;
                                }
                            }

                            foreach (XmlNode pt in cn.ChildNodes)
                            {
                                if (pt.Name.ToLowerInvariant() == "pattern")
                                {
                                    bool IsComplex = false;
                                    bool IsSeparator = false;
                                    string Pattern = "";
                                    string Category = null;
                                    if (pt.Attributes != null)
                                    {
                                        foreach (XmlAttribute att in pt.Attributes)
                                        {
                                            switch (att.Name.ToLowerInvariant())
                                            {
                                                case "text":
                                                    Pattern = att.Value;
                                                    break;
                                                case "iscomplex":
                                                    IsComplex = bool.Parse(att.Value);
                                                    break;
                                                case "isseparator":
                                                    IsSeparator = bool.Parse(att.Value);
                                                    break;
                                                case "category":
                                                    Category = (att.Value);
                                                    break;
                                            }
                                        }
                                    }
                                    if (Pattern != "")
                                    {
                                        var pat = new Pattern(Pattern, IsComplex, IsSeparator, false) {Category = Category};
                                        pl.Add(pat);
                                    }
                                }
                                else if (pt.Name.ToLowerInvariant() == "patterns")
                                {
                                    string Patterns = pt.ChildNodes[0].Value;
                                    Patterns = Patterns.Replace("\t", " ");
                                    while (Patterns.IndexOf("  ") >= 0)
                                        Patterns = Patterns.Replace("  ", " ");

                                    foreach (string Pattern in Patterns.Split())
                                    {
                                        if (Pattern != "")
                                            pl.Add(new Pattern(Pattern, false, false, false));
                                    }
                                }
                            }
                        }
                    }

                if (n.Name.ToLowerInvariant() == "childblocks")
                {
                    foreach (XmlNode cn in n.ChildNodes)
                    {
                        if (cn.Name.ToLowerInvariant() == "child")
                        {
                            foreach (XmlAttribute att in cn.Attributes)
                                if (att.Name.ToLowerInvariant() == "name")
                                    bl.childSpanDefinitions.Add(GetBlock(att.Value));
                        }
                    }
                }
            }
        }


        //done
        private TextStyle GetStyle(string Name)
        {
            if (styleLookup[Name] == null)
            {
                var s = new TextStyle();
                styleLookup.Add(Name, s);
            }

            return (TextStyle) styleLookup[Name];
        }

        //done
        private SpanDefinition GetBlock(string Name)
        {
            if (spanDefinitionLookup[Name] == null)
            {
                var b = new SpanDefinition(syntaxDefinition);
                spanDefinitionLookup.Add(Name, b);
            }

            return (SpanDefinition) spanDefinitionLookup[Name];
        }

        //done
        private void ParseStyle(XmlNode node)
        {
            string Name = "";
            string ForeColor = "", BackColor = "";
            bool Bold = false, Italic = false, Underline = false;


            foreach (XmlAttribute att in node.Attributes)
            {
                switch (att.Name.ToLowerInvariant())
                {
                    case "name":
                        Name = att.Value;
                        break;
                    case "forecolor":
                        ForeColor = att.Value;
                        break;
                    case "backcolor":
                        BackColor = att.Value;
                        break;
                    case "bold":
                        Bold = bool.Parse(att.Value);
                        break;
                    case "italic":
                        Italic = bool.Parse(att.Value);
                        break;
                    case "underline":
                        Underline = bool.Parse(att.Value);
                        break;
                }
            }

            TextStyle st = GetStyle(Name);

            if (BackColor != "")
            {
                st.BackColor = Color.FromName(BackColor);
            }

            st.ForeColor = Color.FromName(ForeColor);
            st.Bold = Bold;
            st.Italic = Italic;
            st.Underline = Underline;
            st.Name = Name;
        }
    }
}