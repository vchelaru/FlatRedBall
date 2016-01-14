using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FlatRedBall.Arrow.DataTypes
{
    public class ArrowIntentSave
    {

        public string Name { get; set; }

        public List<ArrowIntentComponentSave> Components { get; set; }


        public ArrowIntentSave()
        {
            Components = new List<ArrowIntentComponentSave>();

        }
    }
}
