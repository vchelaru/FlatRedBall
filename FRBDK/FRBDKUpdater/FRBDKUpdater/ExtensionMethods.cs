using System.IO;

namespace FRBDKUpdater
{
    public static class ExtensionMethods
    {
        public static void Empty(this DirectoryInfo directory)
        {
            foreach (var file in directory.GetFiles())
            {
                file.Delete();
            }
            foreach (var subDirectory in directory.GetDirectories())
            {
                try
                {
                    subDirectory.Delete(true);
                }
                catch(IOException e)
                {
                    Messaging.Alert(@"Could not delete " + subDirectory.FullName + @"\n\nPlease delete this manually then click OK to continue");
                    throw e;
                }
            }
        }
    }
}
