using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FlatRedBall.Glue.Plugins.Interfaces
{
    public interface IContentFileChange : IPlugin
    {
        void ReactToFileChange(string fileName);
    }
}
