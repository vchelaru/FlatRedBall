using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace FlatRedBall.Graphics
{
    internal class RenderBreakList : List<RenderBreak>
    {
        public int GetNumberOfElementsForIndex(int renderBreakIndex, IList allElements)
        {
            if (renderBreakIndex == this.Count - 1)
            {
                return allElements.Count - this[renderBreakIndex].ItemNumber;
            }
            else
            {
                return this[renderBreakIndex + 1].ItemNumber - this[renderBreakIndex].ItemNumber;
            }
        }
    }
}
