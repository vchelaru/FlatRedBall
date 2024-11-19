using System;
using System.Collections.Generic;
using System.Text;
using Gum.Wireframe;

using InteractiveGue = global::Gum.Wireframe.GraphicalUiElement;
namespace FlatRedBall.Forms.Controls;

public class UserControl : FrameworkElement
{
    public UserControl() : base() { }

    public UserControl(GraphicalUiElement visual) : base(visual) { }
}
