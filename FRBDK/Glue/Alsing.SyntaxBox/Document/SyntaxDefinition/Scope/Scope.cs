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
    /// The Scope class defines what patterns starts and ends a spanDefinition
    /// </summary>
    public sealed class Scope
    {
        /// <summary>
        /// Gets or Sets if the scope patterns is case sensitive
        /// </summary>
        public bool CaseSensitive;

        public bool CauseIndent;

        /// <summary>
        /// Gets or Sets if this block should be expanded or collapsed by default
        /// </summary>
        public bool DefaultExpanded = true;

        /// <summary>
        /// List of end patterns
        /// </summary>
        public PatternCollection EndPatterns = new PatternCollection();

        /// <summary>
        /// The text that should be displayed if the owner block is collapsed
        /// </summary>
        public string ExpansionText = "";

        /// <summary>
        /// Gets or Sets if the scope patterns should be case normalized
        /// </summary>
        public bool NormalizeCase = true;

        /// <summary>
        /// The owner spanDefinition
        /// </summary>
        public SpanDefinition Parent;

        /// <summary>
        /// spanDefinition that should be started directly after this block have ended
        /// </summary>
        public SpanDefinition spawnSpanOnEnd;

        /// <summary>
        /// spanDefinition that should be started directly after this block have started
        /// </summary>
        public SpanDefinition spawnSpanOnStart;

        /// <summary>
        /// The Start trigger Pattern
        /// </summary>
        public Pattern Start;

        /// <summary>
        /// The style that should be applied to the start and end patterns
        /// </summary>
        public TextStyle Style;
    }
}