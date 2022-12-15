using System;
using System.Collections.Generic;
using System.Text;

namespace FlatRedBall.Forms
{
    public enum TextWrapping
    {
        // todo - support wrap with overflow

        /// <summary>
        /// No line wrapping is performed.
        /// </summary>
        NoWrap = 1,

        /// <summary>
        /// Line-breaking occurs if the line overflows beyond the available block width,
        /// even if the standard line breaking algorithm cannot determine any line break
        /// opportunity, as in the case of a very long word constrained in a fixed-width
        /// container with no scrolling allowed.
        /// </summary>
        Wrap = 2
    }
}
