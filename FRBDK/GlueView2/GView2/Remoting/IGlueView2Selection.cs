using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlatRedBall.GlueView2
{
    public interface IGlueView2Selection
    {
        void LoadGluxFile(string glueProjectFileName);
        void ShowElement(string name);


    }
}
