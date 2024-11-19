using System;
using System.Collections.Generic;
using System.Text;
using Gum.Wireframe;

#if FRB
using InteractiveGue = global::Gum.Wireframe.GraphicalUiElement;
namespace FlatRedBall.Forms.Controls;
#else

#endif

public class UserControl : FrameworkElement
{
    public UserControl() : base() { }

    public UserControl(GraphicalUiElement visual) : base(visual) { }
}
