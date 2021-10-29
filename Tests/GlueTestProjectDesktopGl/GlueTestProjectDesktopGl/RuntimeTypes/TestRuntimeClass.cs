using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GlueTestProject.RuntimeTypes
{
    public class TestRuntimeClass
    {
        public bool IsDestroyed
        {
            get;
            private set;
        }

        public void Destroy()
        {
            IsDestroyed = true;
        }
    }
}
