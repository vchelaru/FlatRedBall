// *
// * Copyright (C) 2008 Roger Alsing : http://www.rogeralsing.com
// *
// * This library is free software; you can redistribute it and/or modify it
// * under the terms of the GNU Lesser General Public License 2.1 or later, as
// * published by the Free Software Foundation. See the included license.txt
// * or http://www.gnu.org/copyleft/lesser.html for details.
// *
// *

namespace Alsing.Text.PatternMatchers
{
    public class PatternMatchReference
    {
        public IPatternMatcher Matcher;
        public PatternMatchReference NextSibling;
        public object[] Tags;
        public bool NeedSeparators;

        public PatternMatchReference(IPatternMatcher matcher)
        {
            Matcher = matcher;
        }
    }
}