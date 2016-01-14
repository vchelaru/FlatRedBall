using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FlatRedBall.Managers
{
    public interface IManager
    {
        void Update();
        void UpdateDependencies();
    }
}
