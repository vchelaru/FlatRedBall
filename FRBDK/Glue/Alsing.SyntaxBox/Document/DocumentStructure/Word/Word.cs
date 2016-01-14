// *
// * Copyright (C) 2008 Roger Alsing : http://www.RogerAlsing.com
// *
// * This library is free software; you can redistribute it and/or modify it
// * under the terms of the GNU Lesser General Public License 2.1 or later, as
// * published by the Free Software Foundation. See the included license.txt
// * or http://www.gnu.org/copyleft/lesser.html for details.
// *
// *

using System.Drawing;

namespace Alsing.SourceCode
{
    /// <summary>
    /// Word types
    /// </summary>
    public enum WordType
    {
        /// <summary>
        /// The word is a normal word/text
        /// </summary>
        Word = 0,
        /// <summary>
        /// The word is a space char
        /// </summary>
        Space = 1,
        /// <summary>
        /// The word is a tab char
        /// </summary>
        Tab = 2
    }

    /// <summary>
    /// The word object class represents a word in a Row object
    /// </summary>
    public sealed class Word
    {
        #region General Declarations

        /// <summary>
        /// Color of the error wave lines
        /// </summary>
        public Color ErrorColor = Color.Red;

        /// <summary>
        /// True if the word has error wave lines
        /// </summary>
        public bool HasError;

        /// <summary>
        /// The ToolTip text for the word
        /// </summary>
        public string InfoTip;

        /// <summary>
        /// The pattern that created this word
        /// </summary>
        public Pattern Pattern; //the pattern that found this word

        /// <summary>
        /// The parent row
        /// </summary>
        public Row Row; //the row that holds this word

        /// <summary>
        /// The parent span
        /// </summary>
        public Span Span; //the span that this word is located in

        /// <summary>
        /// The style of the word
        /// </summary>
        public TextStyle Style; //the style of the word

        /// <summary>
        /// The text of the word
        /// </summary>
        public string Text; //the text in the word

        /// <summary>
        /// The type of the word
        /// </summary>
        public WordType Type; //word type , space , tab , word

        #endregion

        /// <summary>
        /// Gets the index of the word in the parent row
        /// </summary>
        public int Index
        {
            get { return Row.IndexOf(this); }
        }

        /// <summary>
        /// Returns the column where the word starts on the containing row.
        /// </summary>
        public int Column
        {
            get
            {
                int x = 0;
                foreach (Word w in Row)
                {
                    if (w == this)
                        return x;
                    x += w.Text.Length;
                }
                return - 1;
            }
        }
    }
}