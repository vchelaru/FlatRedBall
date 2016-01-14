using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FlatRedBall.Attributes
{
    public enum SortingStyle
    {
        None,
        First
    }
        

    public class ContentSorting : System.Attribute
    {
        public SortingStyle SortingStyle;

        public ContentSorting(SortingStyle sortingStyle)
        {
            SortingStyle = sortingStyle;
        }
    }
}
