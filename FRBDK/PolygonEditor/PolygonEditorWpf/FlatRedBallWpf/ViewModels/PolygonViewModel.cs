using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FlatRedBall.Math.Geometry;

namespace PolygonEditor.ViewModels
{
    public class PolygonViewModel
    {
        Polygon backingData;

        public PolygonViewModel(Polygon backingData)
        {
            this.backingData = backingData;
        }
    }
}
