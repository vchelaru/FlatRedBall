using System.Reflection;

namespace FlatRedBall.Glue.CodeGeneration.CodeBuilder
{
    public class CodeBlockClass : CodeBlockBase
    {
        public override bool IndentBody => true;

        public CodeBlockClass(ICodeBlock pParent, string pPre, string pName, string pPost) : base(pParent)
        {
            PreCodeLines.Add(new CodeLine(StringHelper.SpaceStrings(pPre, "class", pName, pPost)));
            PreCodeLines.Add(new CodeLine("{"));
            PostCodeLines.Add(new CodeLine("}"));
        }
    }

    public class CodeBlockStruct : CodeBlockBase
    {
        public override bool IndentBody => true;

        public CodeBlockStruct(ICodeBlock pParent, string pPre, string pName)
            : base(pParent)
        {
            PreCodeLines.Add(new CodeLine(StringHelper.SpaceStrings(pPre, "struct", pName)));
            PreCodeLines.Add(new CodeLine("{"));
            PostCodeLines.Add(new CodeLine("}"));
        }
    }

    public class CodeBlockEnum : CodeBlockBase
    {
        public override bool IndentBody => true;

        public CodeBlockEnum(ICodeBlock pParent, string pPre, string pName)
            : base(pParent)
        {
            PreCodeLines.Add(new CodeLine(StringHelper.SpaceStrings(pPre, "enum", pName)));
            PreCodeLines.Add(new CodeLine("{"));
            PostCodeLines.Add(new CodeLine("}"));
        }
    }

    public static class CodeBlockClassExtensions
    {
        public static ICodeBlock Class(this ICodeBlock pCodeBlock, string pPre, string pName, string pPost = null)
        {
            return new CodeBlockClass(pCodeBlock, pPre, pName, pPost);
        }

        internal static ICodeBlock Class(this ICodeBlock pCodeBlock,
            string pName,
            bool Public            = false,
            bool Private           = false,
            bool Protected         = false,
            bool Internal          = false,
            bool ProtectedInternal = false,
            bool Static            = false,
            bool Partial           = false,
            bool Abstract          = false,
            bool Sealed            = false)
        {
            return pCodeBlock.Class(
                StringHelper.Modifiers(
                    Public: Public, 
                    Private: Private, 
                    Protected: Protected, 
                    Internal: Internal, 
                    ProtectedInternal: ProtectedInternal, 
                    Static: Static,
                    Partial: Partial,
                    Abstract: Abstract,
                    Sealed: Sealed
                    )
                , pName
                ,"");
        }

        public static ICodeBlock Struct(this ICodeBlock pParent, string pPre, string pName)
        {
            return new CodeBlockStruct(pParent, pPre, pName);
        }

        public static ICodeBlock Enum(this ICodeBlock pParent, string pPre, string pName)
        {
            return new CodeBlockEnum(pParent, pPre, pName);
        }
    }
}
