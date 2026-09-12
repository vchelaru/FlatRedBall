namespace FlatRedBall.Glue.CodeGeneration.CodeBuilder
{
    public class CodeBlockTry : CodeBlockBase
    {
        public override bool IndentBody => true;

        public CodeBlockTry(ICodeBlock pParent) : base(pParent)
        {
            PreCodeLines.Add(new CodeLine("try"));
            PreCodeLines.Add(new CodeLine("{"));
            PostCodeLines.Add(new CodeLine("}"));
        }
    }

    public class CodeBlockCatch : CodeBlockBase
    {
        public override bool IndentBody => true;

        public CodeBlockCatch(ICodeBlock pParent, string pCondition)
            : base(pParent)
        {
            PreCodeLines.Add(new CodeLine("catch(" + (string.IsNullOrEmpty(pCondition) ? "" : pCondition) + ")"));
            PreCodeLines.Add(new CodeLine("{"));
            PostCodeLines.Add(new CodeLine("}"));
        }
    }

    public class CodeBlockFinally : CodeBlockBase
    {
        public override bool IndentBody => true;

        public CodeBlockFinally(ICodeBlock pParent)
            : base(pParent)
        {
            PreCodeLines.Add(new CodeLine("finally"));
            PreCodeLines.Add(new CodeLine("{"));
            PostCodeLines.Add(new CodeLine("}"));
        }
    }

    public static class CodeBlockExceptionExtensions
    {
        public static ICodeBlock Try(this ICodeBlock pParent)
        {
            return new CodeBlockTry(pParent);
        }

        public static ICodeBlock Catch(this ICodeBlock pParent, string pCondition)
        {
            return new CodeBlockCatch(pParent, pCondition);
        }

        public static ICodeBlock Finally(this ICodeBlock pParent)
        {
            return new CodeBlockFinally(pParent);
        }
    }
}
