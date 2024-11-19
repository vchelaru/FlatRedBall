using System;
using System.Collections;
using System.Collections.Generic;

#if FRB
namespace FlatRedBall.Forms.Controls;
#else
namespace MonoGameGum.Forms.Controls;
#endif

public class SelectionChangedEventArgs
{
    public IList RemovedItems { get; private set; } = new List<Object>();
    public IList AddedItems { get; private set; } = new List<Object>();
}
