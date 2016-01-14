// *
// * Copyright (C) 2008 Roger Alsing : http://www.RogerAlsing.com
// *
// * This library is free software; you can redistribute it and/or modify it
// * under the terms of the GNU Lesser General Public License 2.1 or later, as
// * published by the Free Software Foundation. See the included license.txt
// * or http://www.gnu.org/copyleft/lesser.html for details.
// *
// *

using System.Collections;
using System.Collections.Generic;
using System.Drawing;

namespace Alsing.SourceCode
{
    /// <summary>
    /// spanDefinition class
    /// </summary>
    /// <remarks>
    /// The spanDefinition class represents a specific code/text element<br/>
    /// such as a string , comment or the code itself.<br/>
    /// <br/>
    /// a spanDefinition  can contain keywords , operators , scopes and child spans.<br/>
    /// <br/>
    /// <br/>
    /// For example , if we where to describe the syntax C#<br/>
    /// we would have the following span:<br/>
    /// <br/>
    /// Code span						- the spanDefinition containing all the keywords and operators.<br/>
    /// Singleline comment span		    - a spanDefinition that starts on // terminates at the end of a line.<br/>
    /// Multiline comment span			- a spanDefinition that starts on /* can span multiple rows and terminates on */.<br/>
    /// String span					    - a spanDefinition that starts on " terminates on " or at the end of a line.<br/>
    /// Char span						- a spanDefinition that starts on ' terminates on ' or at the end of a line.<br/>
    /// <br/>
    /// <b>CHILD SPANS:</b><br/>
    /// The code span would have all the other spans as childspans , since they can only appear inside the<br/>
    /// code span . A string can for example never exist inside a comment in C#.<br/>
    /// a spanDefinition can also have itself as a child span.<br/>
    /// For example , the C# Code span can have itself as a childspan and use the scope patterns "{" and "}"<br/>
    /// this way we can accomplish FOLDING since the parser will know where a new scope starts and ends.<br/>
    /// <br/>
    /// <b>SCOPES:</b><br/>
    /// Scopes describe what patterns starts and what patterns end a specific spanDefinition.<br/>
    /// For example , the C# Multiline Comment have the scope patterns /* and */<br/>
    /// <br/>
    /// <b>KEYWORDS:</b><br/>
    /// A Keyword is a pattern that can only exist between separator chars.<br/>
    /// For example the keyword "for" in c# is valid if it is contained in this string " for ("<br/>
    /// but it is not valid if the containing string is " MyFormat "<br/>
    /// <br/>
    /// <b>OPERATORS:</b><br/>
    /// Operators is the same thing as keywords but are valid even if there are no separator chars around it.<br/>
    /// In most cases operators are only one or two chars such as ":" or "->"<br/>
    /// operators in this context should not be mixed up with code operators such as "and" or "xor" in VB6<br/>
    /// in this context they are keywords.<br/>
    ///<br/>
    /// <br/>
    ///</remarks>
    public class SpanDefinition
    {
        private readonly List<Pattern> tmpSimplePatterns = new List<Pattern>();

        /// <summary>
        /// The background color of a span.
        /// </summary>
        public Color BackColor = Color.Transparent;

        /// <summary>
        /// A list containing which spanDefinitions are valid child spans in a specific span.
        /// eg. strings and comments are child spans for a code span
        /// </summary>
        public SpanDefinitionList childSpanDefinitions = new SpanDefinitionList();

        public PatternCollection ComplexPatterns = new PatternCollection();

        /// <summary>
        /// A list of keyword groups.
        /// For example , one keyword group could be "keywords" and another could be "datatypes"
        /// theese groups could have different color shemes assigned to them.
        /// </summary>
        public PatternListList KeywordsList; //new PatternListList (this);

        public Hashtable LookupTable = new Hashtable();

        /// <summary>
        /// Gets or Sets if the spanDefinition can span multiple lines or if it should terminate at the end of a line.
        /// </summary>
        public bool MultiLine;

        /// <summary>
        /// The name of this span.
        /// names are not required for span but can be a good help when interacting with the parser.
        /// </summary>
        public string Name = "";

        /// <summary>
        /// A list of operator groups.
        /// Each operator group can contain its own operator patterns and its own color shemes.
        /// </summary>
        public PatternListList OperatorsList; //new PatternListList (this);	

        /// <summary>
        /// A list of scopes , most span only contain one scope , eg a scope with start and end patterns "/*" and "*/"
        /// for multiline comments, but in some cases you will need more scopes , eg. PHP uses both "&lt;?" , "?&gt;" and "&lt;?PHP" , "PHP?&gt;"
        /// </summary>
        public ScopeList ScopePatterns;

        /// <summary>
        /// The style to use when colorizing the content of a span,
        /// meaning everything in this span except keywords , operators and childspans.
        /// </summary>
        public TextStyle Style;

        /// <summary>
        /// Gets or Sets if the parser should terminate any child span when it finds an end scope pattern for this span.
        /// for example %&gt; in asp terminates any asp span even if it appears inside an asp string.
        /// </summary>
        public bool TerminateChildren;


        /// <summary>
        /// Default spanDefinition constructor
        /// </summary>
        public SpanDefinition(SyntaxDefinition parent) : this()
        {
            Parent = parent;
            Parent.ChangeVersion();
        }

        public SpanDefinition()
        {
            KeywordsList = new PatternListList(this);
            OperatorsList = new PatternListList(this);

            Style = new TextStyle();
            KeywordsList.Parent = this;
            KeywordsList.IsKeyword = true;
            OperatorsList.Parent = this;
            OperatorsList.IsOperator = true;
            ScopePatterns = new ScopeList(this);
        }

        #region PUBLIC PROPERTY PARENT

        public SyntaxDefinition Parent { get; set; }

        #endregion

        /// <summary>
        /// Returns false if any color has been assigned to the backcolor property
        /// </summary>
        public bool Transparent
        {
            get { return (BackColor.A == 0); }
        }

        public void ResetLookupTable()
        {
            LookupTable.Clear();
            tmpSimplePatterns.Clear();
            ComplexPatterns.Clear();
        }

        public void AddToLookupTable(Pattern pattern)
        {
            if (pattern.IsComplex)
            {
                ComplexPatterns.Add(pattern);
                return;
            }
            tmpSimplePatterns.Add(pattern);
        }

        public void BuildLookupTable()
        {
            tmpSimplePatterns.Sort(new PatternComparer());
            foreach (Pattern p in tmpSimplePatterns)
            {
                if (p.StringPattern.Length <= 2)
                {
                    char c = p.StringPattern[0];

                    if (!p.Parent.CaseSensitive)
                    {
                        char c1 = char.ToLowerInvariant(c);
                        if (LookupTable[c1] == null)
                            LookupTable[c1] = new PatternCollection();

                        var patterns = LookupTable[c1] as PatternCollection;
                        if (patterns != null)
                            if (!patterns.Contains(p))
                                patterns.Add(p);

                        char c2 = char.ToUpper(c);
                        if (LookupTable[c2] == null)
                            LookupTable[c2] = new PatternCollection();

                        patterns = LookupTable[c2] as PatternCollection;
                        if (patterns != null)
                            if (!patterns.Contains(p))
                                patterns.Add(p);
                    }
                    else
                    {
                        if (LookupTable[c] == null)
                            LookupTable[c] = new PatternCollection();

                        var patterns = LookupTable[c] as PatternCollection;
                        if (patterns != null)
                            if (!patterns.Contains(p))
                                patterns.Add(p);
                    }
                }
                else
                {
                    string c = p.StringPattern.Substring(0, 3).ToLowerInvariant();

                    if (LookupTable[c] == null)
                        LookupTable[c] = new PatternCollection();

                    var patterns = LookupTable[c] as PatternCollection;
                    if (patterns != null)
                        if (!patterns.Contains(p))
                            patterns.Add(p);
                }
            }
        }
    }

    public class PatternComparer : IComparer<Pattern>
    {
        public int Compare(Pattern x, Pattern y)
        {
            return y.StringPattern.Length.CompareTo(x.StringPattern.Length);
        }
    }
}