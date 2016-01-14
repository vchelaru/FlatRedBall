// *
// * Copyright (C) 2008 Roger Alsing : http://www.rogeralsing.com
// *
// * This library is free software; you can redistribute it and/or modify it
// * under the terms of the GNU Lesser General Public License 2.1 or later, as
// * published by the Free Software Foundation. See the included license.txt
// * or http://www.gnu.org/copyleft/lesser.html for details.
// *
// *

using System;
using Alsing.Text.PatternMatchers;

namespace Alsing.Text
{
    public partial class TokenTree
    {
        public void AddPattern(IPatternMatcher matcher, bool caseSensitive, bool needSeparators, object[] tags)
        {
            if (matcher == null)
                throw new ArgumentNullException("matcher");

            AddPattern(null, matcher, caseSensitive, needSeparators, tags);
        }

        public void AddPattern(string prefix, IPatternMatcher matcher, bool caseSensitive, bool needSeparators, object[] tags)
        {
            if (string.IsNullOrEmpty(prefix))
            {
                AddPatternWithoutPrefix(matcher, caseSensitive, needSeparators, tags);
            }
            else if (caseSensitive)
            {
                AddPatternWithCaseSensitivePrefix(prefix, matcher, needSeparators, tags);
            }
            else
            {
                AddPatternWithCaseInsensitivePrefix(prefix, matcher, needSeparators, tags);
            }
        }

        

        public void AddToken(string text, bool caseSensitive, bool needSeparators, object[] tags)
        {
            if (string.IsNullOrEmpty(text))
                throw new ArgumentNullException(text);

            if (caseSensitive)
            {
                AddCaseSensitiveToken(text, needSeparators, tags);
            }
            else
            {
                AddCaseInsensitiveToken(text, needSeparators, tags);
            }
        }
    }
}
