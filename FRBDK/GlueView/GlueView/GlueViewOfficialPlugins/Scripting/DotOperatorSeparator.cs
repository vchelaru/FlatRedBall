using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GlueViewOfficialPlugins.Scripting
{
    /// <summary>
    /// The parsing system we're using doesn't understand
    /// the dot-operator.  Therefore something like this:
    /// float someValue = this.Sprite.Y;
    /// must be converted to:
    /// float someValue = DotOperator(this, DotOperator(sprite, y))
    /// </summary>
    class DotOperatorSeparator
    {
    }
}
