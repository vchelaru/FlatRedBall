using ToolsUtilities;

namespace FlatRedBall.Glue.Plugins.ExportedInterfaces.CommandInterfaces
{
    /// <summary>The result of <see cref="ICompilerCommands.CompileAsync"/>.</summary>
    public class CompileGeneralResponse : GeneralResponse
    {
        public bool WasCancelled { get; set; }
    }
}
