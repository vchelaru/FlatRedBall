namespace FlatRedBall.Glue.CodeGeneration.CodeBuilder
{
    public class CodeBlockWhile : CodeBlockBase
    {
        public override bool IndentBody => true;

        public CodeBlockWhile(ICodeBlock pParent, string pCondition) : base(pParent)
        {
            PreCodeLines.Add(new CodeLine("while (" + (string.IsNullOrEmpty(pCondition) ? "" : pCondition) + ")"));
            PreCodeLines.Add(new CodeLine("{"));
            PostCodeLines.Add(new CodeLine("}"));
        }
    }

    public class CodeBlockDoWhile : CodeBlockBase
    {
        public override bool IndentBody => true;

        public CodeBlockDoWhile(ICodeBlock pParent, string pCondition)
            : base(pParent)
        {
            PreCodeLines.Add(new CodeLine("do"));
            PreCodeLines.Add(new CodeLine("{"));
            PostCodeLines.Add(new CodeLine("}"));
            PostCodeLines.Add(new CodeLine("while (" + (string.IsNullOrEmpty(pCondition) ? "" : pCondition) + ")"));
        }
    }

    public class CodeBlockFor : CodeBlockBase
    {
        public override bool IndentBody => true;

        public CodeBlockFor(ICodeBlock pParent, string pCondition)
            : base(pParent)
        {
            PreCodeLines.Add(new CodeLine("for (" + (string.IsNullOrEmpty(pCondition) ? "" : pCondition) + ")"));
            PreCodeLines.Add(new CodeLine("{"));
            PostCodeLines.Add(new CodeLine("}"));
        }
    }

    public class CodeBlockForEach : CodeBlockBase
    {
        public override bool IndentBody => true;

        public CodeBlockForEach(ICodeBlock pParent, string pCondition)
            : base(pParent)
        {
            PreCodeLines.Add(new CodeLine("foreach (" + (string.IsNullOrEmpty(pCondition) ? "" : pCondition) + ")"));
            PreCodeLines.Add(new CodeLine("{"));
            PostCodeLines.Add(new CodeLine("}"));
        }
    }

    public static class CodeBlockIterationsExtensions
    {
        public static ICodeBlock While(this ICodeBlock pParent, string pCondition)
        {
            return new CodeBlockWhile(pParent, pCondition);
        }

        public static ICodeBlock DoWhile(this ICodeBlock pParent, string pCondition)
        {
            return new CodeBlockDoWhile(pParent, pCondition);
        }

        public static ICodeBlock For(this ICodeBlock pParent, string pCondition)
        {
            return new CodeBlockFor(pParent, pCondition);
        }

        public static ICodeBlock ForEach(this ICodeBlock pParent, string pCondition)
        {
            return new CodeBlockForEach(pParent, pCondition);
        }
    }
}
