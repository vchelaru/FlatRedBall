namespace FlatRedBall.Glue.Plugins.ExportedInterfaces.CommandInterfaces
{
    public interface IOpenCommands
    {
        /// <summary>
        /// Opens another application using Glue's settings for opening new application.
        /// </summary>
        /// <param name="path">Path of exe to run.</param>
        /// <param name="parameters">Parameters to pass to the exe.</param>
        void OpenExternalApplication(string path, string parameters);


    }
}
