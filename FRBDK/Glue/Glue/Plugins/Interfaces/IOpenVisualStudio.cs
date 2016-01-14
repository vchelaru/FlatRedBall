namespace FlatRedBall.Glue.Plugins.Interfaces
{
    public interface IOpenVisualStudio : IPlugin
    {
        bool OpenSolution(string solution);
        bool OpenProject(string project);
    }
}
