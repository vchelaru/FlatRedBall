namespace FlatRedBall.Glue.CodeGeneration.CodeBuilder
{
    public class CodeBlockProperty : CodeBlockBase
    {
        public CodeBlockProperty(ICodeBlock pParent, string pPre, string pName) : base(pParent)
        {
            PreCodeLines.Add(new CodeLine(StringHelper.SpaceStrings(pPre, pName)));
            PreCodeLines.Add(new CodeLine("{"));
            PostCodeLines.Add(new CodeLine("}"));
        }
    }

    public class CodeBlockGet : CodeBlockBase
    {
        public CodeBlockGet(ICodeBlock pParent, string pPre) : base(pParent)
        {
            PreCodeLines.Add(new CodeLine(StringHelper.SpaceStrings(pPre,"get")));
            PreCodeLines.Add(new CodeLine("{"));
            PostCodeLines.Add(new CodeLine("}"));
        }
    }

    public class CodeBlockSet : CodeBlockBase
    {
        public CodeBlockSet(ICodeBlock pParent, string pPre)
            : base(pParent)
        {
            PreCodeLines.Add(new CodeLine(StringHelper.SpaceStrings(pPre,"set")));
            PreCodeLines.Add(new CodeLine("{"));
            PostCodeLines.Add(new CodeLine("}"));
        }
    }

    public class CodeBlockAutoProperty : CodeBlockBase
    {
        public CodeBlockAutoProperty(ICodeBlock pParent, string pPre, string pName, string pGetPre, string pSetPre) : base(pParent)
        {
            PreCodeLines.Add(new CodeLine(StringHelper.SpaceStrings(pPre,pName,"{", pGetPre, "get;", pSetPre, "set;", "}")));
        }
    }

    public static class CodeBlockPropertyExtension
    {
        public static ICodeBlock AutoProperty(this ICodeBlock pParent, string pPre, string pName)
        {
            return new CodeBlockAutoProperty(pParent, pPre, pName, null, null);
        }

        public static ICodeBlock AutoProperty(this ICodeBlock pParent, string pPre, string pName, string pGetPre, string pSetPre)
        {
            return new CodeBlockAutoProperty(pParent, pPre, pName, pGetPre, pSetPre);
        }

        public static ICodeBlock Property(this ICodeBlock pParent, string pPre, string pName)
        {
            return new CodeBlockProperty(pParent, pPre, pName);
        }

        public static ICodeBlock Get(this ICodeBlock pParent, string pPre)
        {
            return new CodeBlockGet(pParent, pPre);
        }

        public static ICodeBlock Get(this ICodeBlock pParent)
        {
            return new CodeBlockGet(pParent, "");
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
            return pCodeBlock.AutoProperty(
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
