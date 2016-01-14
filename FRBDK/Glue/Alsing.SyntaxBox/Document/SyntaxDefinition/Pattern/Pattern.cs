// *
// * Copyright (C) 2008 Roger Alsing : http://www.RogerAlsing.com
// *
// * This library is free software; you can redistribute it and/or modify it
// * under the terms of the GNU Lesser General Public License 2.1 or later, as
// * published by the Free Software Foundation. See the included license.txt
// * or http://www.gnu.org/copyleft/lesser.html for details.
// *
// *

using System.Text.RegularExpressions;

namespace Alsing.SourceCode
{
    /// <summary>
    /// A Pattern is a specific string or a RegEx pattern that is used by the parser.
    /// There are two types of patterns , Simple and Complex.
    /// 
    /// Simple Patterns are patterns that consists of a simple fixed string eg. "void" or "for".
    /// Complex Patterns are patterns that consists of RegEx patterns , eg hex numbers or urls can be described as regex patterns.
    /// </summary>
    public sealed partial class Pattern
    {
        public static readonly string DefaultSeparators = ".,+-*^\\/()[]{}@:;'?£$#%& \t=<>";

        #region PUBLIC PROPERTY SEPARATORS

        private string _Separators = DefaultSeparators;

        public string Separators
        {
            get { return _Separators; }
            set { _Separators = value; }
        }

        #endregion

        private string _StringPattern = "";
        public BracketType BracketType = BracketType.None;

        /// <summary>
        /// Category of the pattern
        /// Built in categories are:
        /// URL
        /// MAIL
        /// FILE
        /// </summary>
        public string Category;

        /// <summary>
        /// Gets if the pattern is a simple string or a RegEx pattern
        /// </summary>
        public bool IsComplex;

        /// <summary>
        /// Get or Sets if this pattern needs separator chars before and after it in order to be valid.
        /// </summary>
        public bool IsKeyword;

        public bool IsMultiLineBracket = true;

        /// <summary>
        /// Gets or Sets if the pattern is a separator pattern .
        /// A separator pattern can be "End Sub" in VB6 , whenever that pattern is found , the SyntaxBoxControl will render a horizontal separator line.
        /// NOTE: this should not be mixed up with separator chars.
        /// </summary>
        public bool IsSeparator;

        /// <summary>
        /// For internal use only
        /// </summary>
        public string LowerStringPattern = "";

        public Pattern MatchingBracket;

        /// <summary>
        /// The owning PatternList , eg a specific KeywordList or OperatorList
        /// </summary>
        public PatternList Parent;

        internal Regex rx;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="pattern"></param>
        /// <param name="iscomplex"></param>
        public Pattern(string pattern, bool iscomplex)
        {
            StringPattern = pattern;
            if (iscomplex)
            {
                IsComplex = true;
                rx = new Regex(StringPattern, RegexOptions.Compiled);
            }
            else
            {
                IsComplex = false;
            }
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="pattern"></param>
        /// <param name="iscomplex"></param>
        /// <param name="separator"></param>
        /// <param name="keyword"></param>
        public Pattern(string pattern, bool iscomplex, bool separator, bool keyword)
        {
            Init(pattern, iscomplex, separator, keyword);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="pattern"></param>
        /// <param name="separator"></param>
        /// <param name="keyword"></param>
        /// <param name="escapeChar"></param>
        public Pattern(string pattern, bool separator, bool keyword, string escapeChar)
        {
            escapeChar = Regex.Escape(escapeChar);
            string escapePattern = string.Format("(?<=((?<!{0})({0}{0})*))({1})",
                                              escapeChar, pattern);
            Init(escapePattern, true, separator, keyword);
        }

        /// <summary>
        /// Gets or Sets the the text of the pattern
        /// this only applies if the pattern is a simple pattern.
        /// </summary>
        public string StringPattern
        {
            get { return _StringPattern; }
            set
            {
                _StringPattern = value;
                LowerStringPattern = _StringPattern.ToLowerInvariant();
            }
        }

        /// <summary>
        /// Returns true if the pattern contains separator chars<br/>
        /// (This is used by the parser)
        /// </summary>
        public bool ContainsSeparator
        {
            get
            {
                foreach (char c in StringPattern)
                {
                    if (Separators.IndexOf(c) >= 0)
                        return true;
                }
                return false;
            }
        }
        private void Init(string pattern, bool isComplex, bool separator, bool keyword)
        {
            StringPattern = pattern;
            IsSeparator = separator;
            IsKeyword = keyword;
            IsComplex = isComplex;
            if (isComplex)
                rx = new Regex(StringPattern, RegexOptions.Compiled);
        }        
    }
}