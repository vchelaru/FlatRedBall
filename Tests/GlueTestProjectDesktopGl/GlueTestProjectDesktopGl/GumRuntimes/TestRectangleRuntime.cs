using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GlueTestProject.GumRuntimes
{
    public partial class TestRectangleRuntime
    {
        public SpriteRuntime Sprite
        {
            get
            {
                return this.SpriteInstance as SpriteRuntime;
            }
        }
    }
}
