using FlatRedBall.Glue.SaveClasses;

namespace FlatRedBall.Glue.CodeGeneration.CodeBuilder
{

    public class CodeBlockProperty : CodeBlockBase
    {
        public override bool IndentBody => true;
        public CodeBlockProperty(ICodeBlock pParent, string pPre, string pName) : base(pParent)
        {
            PreCodeLines.Add(new CodeLine(StringHelper.SpaceStrings(pPre, pName)));
            PreCodeLines.Add(new CodeLine("{"));
            PostCodeLines.Add(new CodeLine("}"));
        }
    }

    public class CodeBlockGet : CodeBlockBase
    {
        public override bool IndentBody => true;
        public CodeBlockGet(ICodeBlock pParent, string pPre) : base(pParent)
        {
            PreCodeLines.Add(new CodeLine(StringHelper.SpaceStrings(pPre,"get")));
            PreCodeLines.Add(new CodeLine("{"));
            PostCodeLines.Add(new CodeLine("}"));
        }
    }

    public class CodeBlockAutoGet : CodeBlockBase
    {
        public CodeBlockAutoGet(ICodeBlock pParent) : base(pParent)
        {
            PreCodeLines.Add(new CodeLine("get;"));
        }
    }

    public class CodeBlockSet : CodeBlockBase
    {
        public override bool IndentBody => true;
        public CodeBlockSet(ICodeBlock pParent, string pPre)
            : base(pParent)
        {
            PreCodeLines.Add(new CodeLine(StringHelper.SpaceStrings(pPre,"set")));
            PreCodeLines.Add(new CodeLine("{"));
            PostCodeLines.Add(new CodeLine("}"));
        }
    }

    public class CodeBlockAutoSet : CodeBlockBase
    {
        public CodeBlockAutoSet(ICodeBlock pParent) : base(pParent)
        {
            PreCodeLines.Add(new CodeLine("set;"));
        }
    }


    public class CodeBlockAutoProperty : CodeBlockBase
    {
        public CodeBlockAutoProperty(ICodeBlock pParent, string pPre, string propertyName, string getPrefix, string setPrefix) : base(pParent)
        {
            PreCodeLines.Add(new CodeLine(StringHelper.SpaceStrings(pPre, propertyName, "{", getPrefix, "get;", setPrefix, "set;", "}")));
        }
    }

    public static class CodeBlockPropertyExtension
    {
        /// <summary>
        /// Creates an auto property.
        /// </summary>
        /// <param name="parentCodeBlock">The parent code block, like the entire class</param>
        /// <param name="propertyPrefix">Property prefix, like "public int"</param>
        /// <param name="propertyName">Property name like "CurrentHealth"</param>
        /// <returns>The code block</returns>
        public static ICodeBlock AutoProperty(this ICodeBlock parentCodeBlock, string propertyPrefix, string propertyName)
        {
            return new CodeBlockAutoProperty(parentCodeBlock, propertyPrefix, propertyName, null, null);
        }

        public static ICodeBlock AutoProperty(this ICodeBlock parentCodeBlock, string propertyPrefix, string propertyName, string getterPrefix, string setterPrefix)
        {
            return new CodeBlockAutoProperty(parentCodeBlock, propertyPrefix, propertyName, getterPrefix, setterPrefix);
        }

        public static ICodeBlock Property(this ICodeBlock parentCodeBlock, string pPre, string pName)
        {
            return new CodeBlockProperty(parentCodeBlock, pPre, pName);
        }

        public static ICodeBlock Get(this ICodeBlock parentCodeBlock, string pPre)
        {
            return new CodeBlockGet(parentCodeBlock, pPre);
        }


        public static ICodeBlock AutoGet(this ICodeBlock parentCodeBlock)
        {
            return new CodeBlockAutoGet(parentCodeBlock);
        }

        public static ICodeBlock Get(this ICodeBlock pParent)
        {
            return new CodeBlockGet(pParent, "");
        }

        public static ICodeBlock AutoSet(this ICodeBlock pParent)
        {
            return new CodeBlockAutoSet(pParent);
        }

        public static ICodeBlock Set(this ICodeBlock pParent, string pPre)
        {
            return new CodeBlockSet(pParent, pPre);
        }

        public static ICodeBlock Set(this ICodeBlock pParent)
        {
            return new CodeBlockSet(pParent, "");
        }

        internal static ICodeBlock AutoProperty(this ICodeBlock pCodeBlock, string pName,
            Scope scope = Scope.Public,
            bool Static = false,
            bool Override = false,
            bool Virtual = false,
            string Type = null)
        {
            return pCodeBlock.AutoProperty(
                StringHelper.Modifiers(
                Public: scope == Scope.Public,
                Private: scope == Scope.Private,
                Protected: scope == Scope.Protected,
                Internal: scope == Scope.Internal,
                ProtectedInternal: false,
                Static: Static,
                Override: Override,
                Virtual: Virtual,
                Type: Type
                )
                , pName);
        }

        internal static ICodeBlock Property(this ICodeBlock pCodeBlock, string pName,
            bool Public = false,
            bool Private = false,
            bool Protected = false,
            bool Internal = false,
            bool ProtectedInternal = false,
            bool Static = false,
            bool Override = false,
            bool Virtual = false,
            string Type = null)
        {
            return pCodeBlock.Property(
                StringHelper.Modifiers(
                Public: Public,
                Private: Private,
                Protected: Protected,
                Internal: Internal,
                ProtectedInternal: ProtectedInternal,
                Static: Static,
                Override: Override,
                Virtual: Virtual,
                Type: Type
                )
                , pName);
        }
    }
}
