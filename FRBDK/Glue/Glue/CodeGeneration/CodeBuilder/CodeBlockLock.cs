namespace FlatRedBall.Glue.CodeGeneration.CodeBuilder
{
    public class CodeBlockLock : CodeBlockBase
    {
        protected override bool IndentBody => true;

        public CodeBlockLock(ICodeBlock pParent, string pCondition) : base(pParent)
        {
            PreCodeLines.Add(new CodeLine("lock (" + (string.IsNullOrEmpty(pCondition) ? "" : pCondition) + ")"));
            PreCodeLines.Add(new CodeLine("{"));
            PostCodeLines.Add(new CodeLine("}"));
        }
    }

    public static class CodeBlockLockExtensions
    {
        public static ICodeBlock Lock(this ICodeBlock pCodeBlock, string pCondition)
        {
            return new CodeBlockLock(pCodeBlock, pCondition);
        }
    }
}
