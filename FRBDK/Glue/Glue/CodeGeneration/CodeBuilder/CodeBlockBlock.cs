namespace FlatRedBall.Glue.CodeGeneration.CodeBuilder
{
    public class CodeBlockBlock : CodeBlockBase
    {
        public CodeBlockBlock(ICodeBlock pParent) : base(pParent)
        {
            PreCodeLines.Add(new CodeLine("{"));
            PostCodeLines.Add(new CodeLine("}"));
        }
    }

    public static class CodeBlockBlockExtensions
    {
        public static ICodeBlock Block(this ICodeBlock pCodeBlock)
        {
            return new CodeBlockBlock(pCodeBlock);
        }

        public static ICodeBlock InsertBlock(this ICodeBlock pCodeBlock, ICodeBlock pChildBlock)
        {
            if (pChildBlock == null) return pCodeBlock;

            pCodeBlock.BodyCodeLines.Add(pChildBlock);
            pChildBlock.Parent = pCodeBlock;

            return pCodeBlock;
        }
    }
}
