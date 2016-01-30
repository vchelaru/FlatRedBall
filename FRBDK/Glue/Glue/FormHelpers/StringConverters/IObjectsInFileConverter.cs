using FlatRedBall.Glue.SaveClasses;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GluePropertyGridClasses.StringConverters
{
    public interface IObjectsInFileConverter
    {
        ReferencedFileSave ReferencedFileSave
        {
            get;
        }
    }
}
