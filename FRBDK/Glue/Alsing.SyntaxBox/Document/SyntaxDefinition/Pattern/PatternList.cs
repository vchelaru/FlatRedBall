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

namespace Alsing.SourceCode
{
    /// <summary>
    /// A List containing patterns.
    /// this could be for example a list of keywords or operators
    /// </summary>
    public sealed class PatternList : IEnumerable
    {
        private readonly PatternCollection patterns = new PatternCollection();

        /// <summary>
        /// Gets or Sets if this list contains case seinsitive patterns
        /// </summary>		
        public bool CaseSensitive;

        /// <summary>
        /// For public use only
        /// </summary>
        public PatternCollection ComplexPatterns = new PatternCollection();

        /// <summary>
        /// The name of the pattern list
        /// </summary>
        public string Name = "";

        /// <summary>
        /// Gets or Sets if the patterns in this list should be case normalized
        /// </summary>
        public bool NormalizeCase;

        /// <summary>
        /// 
        /// </summary>
        public PatternListList Parent;

        /// <summary>
        /// The parent spanDefinition of this list
        /// </summary>
        public SpanDefinition parentSpanDefinition;

        /// <summary>
        /// for public use only
        /// </summary>
        public Hashtable SimplePatterns = new Hashtable();

        /// <summary>
        /// 
        /// </summary>
        public Hashtable SimplePatterns1Char = new Hashtable();

        /// <summary>
        /// For public use only
        /// </summary>
        public Hashtable SimplePatterns2Char = new Hashtable();

        /// <summary>
        /// Gets or Sets the TextStyle that should be assigned to patterns in this list
        /// </summary>
        public TextStyle Style = new TextStyle();

        /// <summary>
        /// 
        /// </summary>
        public PatternList()
        {
            SimplePatterns = new Hashtable(CaseInsensitiveHashCodeProvider.Default,
                                           CaseInsensitiveComparer.Default);
        }

        #region IEnumerable Members

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public IEnumerator GetEnumerator()
        {
            return patterns.GetEnumerator();
        }

        #endregion

        /// <summary>
        /// 
        /// </summary>
        /// <param name="Pattern"></param>
        /// <returns></returns>
        public Pattern Add(Pattern Pattern)
        {
            if (Parent != null && Parent.Parent != null &&
                Parent.Parent.Parent != null)
            {
                Pattern.Separators = Parent.Parent.Parent.Separators;
                Parent.Parent.Parent.ChangeVersion();
            }

            if (!Pattern.IsComplex && !Pattern.ContainsSeparator)
            {
                //store pattern in lookuptable if it is a simple pattern
                string s;

                if (Pattern.StringPattern.Length >= 2)
                    s = Pattern.StringPattern.Substring(0, 2);
                else
                    s = Pattern.StringPattern.Substring(0, 1) + " ";

                s = s.ToLowerInvariant();

                if (Pattern.StringPattern.Length == 1)
                {
                    SimplePatterns1Char[Pattern.StringPattern] = Pattern;
                }
                else
                {
                    if (SimplePatterns2Char[s] == null)
                        SimplePatterns2Char[s] = new PatternCollection();
                    var ar = (PatternCollection) SimplePatterns2Char[s];
                    ar.Add(Pattern);
                }

                if (CaseSensitive)
                    SimplePatterns[Pattern.LowerStringPattern] = Pattern;
                else
                    SimplePatterns[Pattern.StringPattern] = Pattern;
            }
            else
            {
                ComplexPatterns.Add(Pattern);
            }

            patterns.Add(Pattern);
            if (Pattern.Parent == null)
                Pattern.Parent = this;
            else
            {
                throw (new Exception("Pattern already assigned to another PatternList"));
            }
            return Pattern;
        }

        /// <summary>
        /// 
        /// </summary>
        public void Clear()
        {
            patterns.Clear();
        }
    }
}