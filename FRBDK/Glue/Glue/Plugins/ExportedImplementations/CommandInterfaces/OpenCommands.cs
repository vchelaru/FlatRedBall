using FlatRedBall.Glue.IO;
using FlatRedBall.Glue.Plugins.ExportedInterfaces.CommandInterfaces;

namespace FlatRedBall.Glue.Plugins.ExportedImplementations.CommandInterfaces
{
    class OpenCommands : IOpenCommands
    {
        public void OpenExternalApplication(string path, string parameters)
        {
            ProcessManager.OpenProcess(path, parameters);
        }

    }
}
