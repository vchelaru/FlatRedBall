namespace FlatRedBall.Glue.CodeGeneration.CodeBuilder
{
    public class CodeBlockNamespace : CodeBlockBase
    {
        public override bool IndentBody => true;

        public CodeBlockNamespace(ICodeBlock pParent, string value) : base(pParent)
        {
            PreCodeLines.Add(new CodeLine("namespace " + (string.IsNullOrEmpty(value) ? "" : value)));
            PreCodeLines.Add(new CodeLine("{"));
            PostCodeLines.Add(new CodeLine("}"));
            this.TabCount = 0;
        }
    }

    public static class CodeBlockNamespaceExtensions
    {
        public static CodeBlockNamespace Namespace(this ICodeBlock pCodeBase, string pValue)
        {
            return new CodeBlockNamespace(pCodeBase, pValue);
        }
    }
}
