// *
// * Copyright (C) 2008 Roger Alsing : http://www.rogeralsing.com
// *
// * This library is free software; you can redistribute it and/or modify it
// * under the terms of the GNU Lesser General Public License 2.1 or later, as
// * published by the Free Software Foundation. See the included license.txt
// * or http://www.gnu.org/copyleft/lesser.html for details.
// *
// *

namespace Alsing.Text
{
    public struct MatchResult
    {
        public bool Found;
        public int Index;
        public int Length;
        public object[] Tags;
        public string Text;

        public static MatchResult NoMatch
        {
            get
            {
                var result = new MatchResult {Found = false};
                return result;
            }
        }

        public override string ToString()
        {
            if (Found == false)
                return "no match"; // do not localize

            if (Tags != null)
                return Tags + "  " + Index + "  " + Length;

            return "MatchResult";
        }

        public string GetText()
        {
            if (Text != null)
                return Text.Substring(Index, Length);

            return "";
        }
    }
}