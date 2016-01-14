using System.Linq;
using System.Text;

namespace FlatRedBall.Glue.CodeGeneration.CodeBuilder
{
    public class StringHelper
    {
        public static string SpaceStrings(params string[] strings)
        {
            var bldr = new StringBuilder();
            var first = true;

            foreach (var t in strings.Where(t => !string.IsNullOrEmpty(t)))
            {
                if (first)
                {
                    bldr.Append(t);
                    first = false;
                }
                else
                {
                    bldr.Append(" ");
                    bldr.Append(t.Trim());
                }
            }

            return bldr.ToString();
        }

        public static string Modifiers(
            bool Public = false,
            bool Private = false,
            bool Protected = false,
            bool Internal = false,
            bool ProtectedInternal = false,
            bool Static = false,
            bool Partial = false,
            bool Abstract = false,
            bool Sealed = false,
            bool Const = false,
            bool Event = false,
            bool Extern = false,
            bool Override = false,
            bool ReadOnly = false,
            bool Virtual = false,
            bool New = false,
            string Type = null,
            string Name = null
            )
        {
            string scope = null;
            if (Public)
            {
                scope = "public";
            }
            else if (Private)
            {
                scope = "private";
            }
            else if (Protected)
            {
                scope = "protected";
            }
            else if (Internal)
            {
                scope = "internal";
            }
            else if (ProtectedInternal)
            {
                scope = "protected internal";
            }

            var strStatic = Static ? "static" : null;
            var strPartial = Partial ? "partial" : null;
            var strAbstractSealed = Abstract ? "abstract" : Sealed ? "sealed" : null;
            var strConst = Const ? "const" : null;
            var strEvent = Event ? "event" : null;
            var strExtern = Extern ? "extern" : null;
            var strOverride = Override ? "override" : null;
            var strReadOnly = ReadOnly ? "readonly" : null;
            var strVirtual = Virtual ? "virtual" : null;
            var strNew = New ? "new" : null;

            return SpaceStrings(scope, strStatic, strPartial, strAbstractSealed, strConst, strEvent, strExtern,
                                strOverride, strReadOnly, strVirtual, strNew, Type, Name);
        }
    }
}
