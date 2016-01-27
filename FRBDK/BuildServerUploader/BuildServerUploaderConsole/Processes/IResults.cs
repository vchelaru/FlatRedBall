namespace BuildServerUploaderConsole.Processes
{
    public interface IResults
    {
        void WriteMessage(string message);

        void Send();
    }
}
