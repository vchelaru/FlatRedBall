using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RenderingLibrary.Graphics;

namespace GlueTestProject.GumRuntimes
{
    public partial class NineSliceRuntime
    {
        public NineSlice InternalNineSlice
        {
            get
            {
                return this.ContainedNineSlice;
            }
        }
    }
}
