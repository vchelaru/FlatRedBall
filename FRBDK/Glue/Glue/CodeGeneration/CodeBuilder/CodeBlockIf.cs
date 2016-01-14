namespace FlatRedBall.Glue.CodeGeneration.CodeBuilder
{
    public class CodeBlockIf : CodeBlockBase
    {
        public CodeBlockIf(ICodeBlock pParent, string pCondition) : base(pParent)
        {
            PreCodeLines.Add(new CodeLine("if (" + (string.IsNullOrEmpty(pCondition) ? "" : pCondition) + ")"));
            PreCodeLines.Add(new CodeLine("{"));
            PostCodeLines.Add(new CodeLine("}"));
        }
    }

    public class CodeBlockElseIf : CodeBlockBase
    {
        public CodeBlockElseIf(ICodeBlock pParent, string pCondition) : base(pParent)
        {
            PreCodeLines.Add(new CodeLine("else if (" + (string.IsNullOrEmpty(pCondition) ? "" : pCondition) + ")"));
            PreCodeLines.Add(new CodeLine("{"));
            PostCodeLines.Add(new CodeLine("}"));
        }
    }

    public class CodeBlockElse : CodeBlockBase
    {
        public CodeBlockElse(ICodeBlock pParent) : base(pParent)
        {
            PreCodeLines.Add(new CodeLine("else"));
            PreCodeLines.Add(new CodeLine("{"));
            PostCodeLines.Add(new CodeLine("}"));
        }
    }

    public static class CodeBlockIfExtensions
    {
        public static ICodeBlock If(this ICodeBlock pCodeBlock, string pCondition)
        {
            return new CodeBlockIf(pCodeBlock, pCondition);
        }

        public static ICodeBlock ElseIf(this ICodeBlock pCodeBlock, string pCondition)
        {
            return new CodeBlockElseIf(pCodeBlock, pCondition);
        }

        public static ICodeBlock Else(this ICodeBlock pCodeBlock)
        {
            return new CodeBlockElse(pCodeBlock);
        }
    }
}
