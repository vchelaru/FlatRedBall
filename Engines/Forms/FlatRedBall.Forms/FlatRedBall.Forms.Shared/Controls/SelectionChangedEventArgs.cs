using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

using InteractiveGue = global::Gum.Wireframe.GraphicalUiElement;
namespace FlatRedBall.Forms.Controls;

public class SelectionChangedEventArgs
{
    public IList RemovedItems { get; private set; } = new List<Object>();
    public IList AddedItems { get; private set; } = new List<Object>();
}
