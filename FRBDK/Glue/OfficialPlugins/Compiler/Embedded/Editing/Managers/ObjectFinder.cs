{CompilerDirectives}

using System;
using System.Collections.Generic;
using System.Text;
using GlueControl.Models;

namespace GlueControl.Managers
{
    public class ObjectFinder
    {
        static ObjectFinder mSelf = new ObjectFinder();
        public static ObjectFinder Self => mSelf;
        public GlueProjectSave GlueProject { get; set; }

    }
}
