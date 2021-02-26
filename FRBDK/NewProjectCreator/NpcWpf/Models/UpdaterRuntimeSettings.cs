
using ToolsUtilities;

namespace FRBDKUpdater
{
    /// <summary>
    /// Contains information about what to update.  This is a general-purpose
    /// class which can be used to download any other type, as opposed to the 
    /// FrbdkUpdaterSettings.cs class which contains information specific to updating
    /// the FRBDK.
    /// </summary>
    public class UpdaterRuntimeSettings
    {
        public const string RuntimeSettingsExtension = "ursx";

        public string FileToDownload;
        public string LocationToSaveFile;
        public string FormTitle;


        public static UpdaterRuntimeSettings FromFile(string fileName)
        {
            return FileManager.XmlDeserialize<UpdaterRuntimeSettings>(fileName);
        }

        public void Save(string fileName)
        {
            FileManager.XmlSerialize(this, fileName);
        }
    }
}
