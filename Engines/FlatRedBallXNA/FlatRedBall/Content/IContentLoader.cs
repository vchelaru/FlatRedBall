using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FlatRedBall.Content
{
    interface IContentLoader<T>
    {
        T Load(string absoluteFileName);    
    }
}
