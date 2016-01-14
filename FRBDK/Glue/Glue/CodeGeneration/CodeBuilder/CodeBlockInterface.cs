namespace FlatRedBall.Glue.CodeGeneration.CodeBuilder
{
    public class CodeBlockInterface : CodeBlockBase
    {
        public CodeBlockInterface(ICodeBlock pParent, string pPre, string pName, string pPost) : base(pParent)
        {
            PreCodeLines.Add(new CodeLine(StringHelper.SpaceStrings(pPre, "interface", pName, pPost)));
            PreCodeLines.Add(new CodeLine("{"));
            PostCodeLines.Add(new CodeLine("}"));
        }
    }

    public static class CodeBlockInterfaceExtensions
    {
        public static ICodeBlock Interface(this ICodeBlock pCodeBlock, string pPre, string pName, string pPost)
        {
            return new CodeBlockInterface(pCodeBlock, pPre, pName, pPost);
        }

        internal static ICodeBlock Interface(this ICodeBlock pCodeBlock, string pName,
            bool Public = false,
            bool Private = false,
            bool Protected = false,
            bool Internal = false,
            bool ProtectedInternal = false,
            bool Static = false,
            bool Partial = false,
            bool Abstract = false,
            bool Sealed = false)
        {
            return pCodeBlock.Interface(
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
                , pName,
                "");

        }
    }
}
