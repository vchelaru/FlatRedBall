using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FlatRedBall.Math.Geometry;

namespace PolygonEditor.ViewModels
{
    public class CircleViewModel
    {
        Circle backingData;

        public CircleViewModel(Circle backingData)
        {
            this.backingData = backingData;
        }
    }
}
