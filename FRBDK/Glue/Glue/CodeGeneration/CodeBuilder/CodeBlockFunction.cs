namespace FlatRedBall.Glue.CodeGeneration.CodeBuilder
{
    public class CodeBlockFunction : CodeBlockBase
    {
        public CodeBlockFunction(ICodeBlock parent,string pre, string name, string parameters) : base(parent)
        {
            PreCodeLines.Add(new CodeLine(StringHelper.SpaceStrings(pre, name, "(") + (string.IsNullOrEmpty(parameters) ? "" : parameters) + ")"));
            PreCodeLines.Add(new CodeLine("{"));
            PostCodeLines.Add(new CodeLine("}"));
        }
    }

    public static class CodeBlockFunctionExtensions
    {
        public static ICodeBlock Function(this ICodeBlock codeBlock, string pre, string name, string parameters)
        {
            return new CodeBlockFunction(codeBlock, pre, name, parameters);
        }

        public static ICodeBlock Constructor(this ICodeBlock codeBlock, string pre, string name, string parameters, string baseOrThisCall = null)
        {
            var toReturn = codeBlock.Function(pre, name, parameters);

            if (!string.IsNullOrEmpty(baseOrThisCall))
            {
                // Insert at index 1, after the function header, but before the opening {
                toReturn.PreCodeLines.Insert(1, new CodeLine("\t: " + baseOrThisCall));
            }

            return toReturn;

        }

        internal static ICodeBlock Function(this ICodeBlock codeBlock, string name, string parameters,
            bool Public = false, 
            bool Private = false, 
            bool Protected = false, 
            bool Internal = false, 
            bool ProtectedInternal = false, 
            bool Static = false, 
            bool Override = false,
            bool Virtual = false,
            bool New = false,
            string Type = null)
        {
            var modifiers =
                                StringHelper.Modifiers(
                Public: Public,
                Private: Private,
                Protected: Protected,
                Internal: Internal,
                ProtectedInternal: ProtectedInternal,
                Static: Static,
                Override: Override,
                Virtual: Virtual,
                Type: Type,
                New: New
                );

            return codeBlock.Function(
                modifiers
                ,name
                ,parameters
                );
        }
    }
}
