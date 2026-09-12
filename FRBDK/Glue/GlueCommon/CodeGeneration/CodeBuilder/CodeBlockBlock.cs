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
        /// <summary>
        /// Creates an indented block with { and } brackets.
        /// </summary>
        /// <param name="pCodeBlock"></param>
        /// <returns></returns>
        public static ICodeBlock Block(this ICodeBlock pCodeBlock)
        {
            var block = new CodeBlockBlock(pCodeBlock);
            block.IndentBody = true;
            return block;
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
