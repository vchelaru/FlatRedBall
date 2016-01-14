using System;
using System.Xml.Serialization;
using FlatRedBall.IO;

namespace OfficialPlugins.FrbUpdater
{
    [XmlRoot("FRBUpdaterSave")]
    public class FrbUpdaterSettings
    {
        #region Fields

        private string _selectedSource = String.Empty;

        const string Filename = "FRBUpdaterSettings.xml";

        #endregion

        public string SelectedSource
        {
            get { return _selectedSource; }
            set { _selectedSource = value; }
        }

        public bool AutoUpdate { get; set; }

        public static FrbUpdaterSettings LoadSettings()
        {
            return LoadSettings(FileManager.UserApplicationData);
        }

        public static FrbUpdaterSettings LoadSettings(string userAppPath)
        {
            var fileName = userAppPath + @"FRBDK/" + Filename;

            if (FileManager.FileExists(fileName))
            {

                var pS = FileManager.XmlDeserialize<FrbUpdaterSettings>(fileName);
                return pS;
            }
            return new FrbUpdaterSettings();
        }

        public void SaveSettings()
        {
            SaveSettings(FileManager.UserApplicationData);
        }

        public void SaveSettings(string userAppPath)
        {
            var fileName = userAppPath + @"FRBDK/" + Filename;

            FileManager.XmlSerialize(this, fileName);
        }

    }
}
