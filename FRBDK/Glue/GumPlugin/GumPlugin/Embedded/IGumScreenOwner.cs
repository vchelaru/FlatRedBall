using System;
using System.Collections.Generic;
using System.Text;

namespace FlatRedBall.Gum
{
    public interface IGumScreenOwner
    {
        global::Gum.Wireframe.GraphicalUiElement GumScreen { get; }
        void RefreshLayout();
    }
}
