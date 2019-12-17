using System;
using System.Collections.Generic;
using System.Text;

namespace FlatRedBall.Glue.Elements
{
    public class MemberWithType
    {
        public string Member;
        public string Type;

        public override string ToString()
        {
            return string.Format("{0} ({1})", Member, Type);
        }
    }
}
