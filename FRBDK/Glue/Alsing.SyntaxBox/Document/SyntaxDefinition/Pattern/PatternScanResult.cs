// *
// * Copyright (C) 2008 Roger Alsing : http://www.RogerAlsing.com
// *
// * This library is free software; you can redistribute it and/or modify it
// * under the terms of the GNU Lesser General Public License 2.1 or later, as
// * published by the Free Software Foundation. See the included license.txt
// * or http://www.gnu.org/copyleft/lesser.html for details.
// *
// *

namespace Alsing.SourceCode
{
    /// <summary>
    /// PatternScanResult struct is redurned by the Pattern class when an .IndexIn call has been performed.
    /// </summary>
    public struct PatternScanResult
    {
        /// <summary>
        /// The index on which the pattern was found in the source string
        /// </summary>
        public int Index;

        /// <summary>
        /// The string that was found , this is always the same as the pattern StringPattern property if the pattern is a simple pattern.
        /// if the pattern is complex this field will contain the string that was found by the scan.
        /// </summary>
        public string Token;
    }
}