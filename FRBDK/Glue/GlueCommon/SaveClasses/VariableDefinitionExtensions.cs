using FlatRedBall.Glue.Parsing;

namespace FlatRedBall.Glue.Elements
{
    /// <summary>
    /// Moved from <c>VariableDefinitionExtensionMethods</c> (Glue.csproj) in #2276. <c>TypeManager.Parse</c>
    /// was a pure forwarder to <see cref="TypeConversion.Parse"/>, so this calls it directly.
    /// </summary>
    public static class VariableDefinitionExtensions
    {
        public static object GetCastedDefaultValue(this VariableDefinition variable) =>
            TypeConversion.Parse(variable.Type, variable.DefaultValue);
    }
}
