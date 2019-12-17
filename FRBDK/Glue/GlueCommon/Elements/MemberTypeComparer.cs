using System;
using System.Collections.Generic;
using System.Text;

namespace FlatRedBall.Glue.Elements
{
    public class MemberTypeComparer : IEqualityComparer<MemberWithType>
    {
        public bool AreEqual(MemberWithType first, MemberWithType second)
        {
            return first.Member == second.Member;
        }

        public bool Equals(MemberWithType x, MemberWithType y)
        {
            return x.Member == y.Member;
        }

        public int GetHashCode(MemberWithType obj)
        {
            return obj.Member.GetHashCode();
        }
    }
}
