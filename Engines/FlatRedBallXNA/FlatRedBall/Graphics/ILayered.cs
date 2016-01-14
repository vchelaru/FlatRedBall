using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FlatRedBall.Graphics
{
    public interface ILayered
    {
        Layer Layer
        {
            get;
        }
    }
}
