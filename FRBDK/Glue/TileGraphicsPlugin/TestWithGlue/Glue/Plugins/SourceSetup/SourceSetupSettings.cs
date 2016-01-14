using System;
using System.Xml.Serialization;
using FlatRedBall.IO;

namespace PluginTestbed.SourceSetup
{
    [XmlRoot("SourceSetupSave")]
    public class SourceSetupSettings
    {
        #region Fields

        private string _selectedEngineDirectory = String.Empty;
        private bool _useSource = false;

        const string Filename = "SourceSetupSettings.xml";

        #endregion

        public SourceSetupSettings()
        {
            SetDefaultPath();
        }

        public string SelectedEngineDirectory
        {
            get { return _selectedEngineDirectory; }
            set { _selectedEngineDirectory = value; }
        }

        public bool UseSource { get; set; }

        public static SourceSetupSettings LoadSettings()
        {
            return LoadSettings(FileManager.UserApplicationData);
        }

        public static SourceSetupSettings LoadSettings(string userAppPath)
        {
            var fileName = userAppPath + @"FRBDK/" + Filename;

            if (FileManager.FileExists(fileName))
            {

                var pS = FileManager.XmlDeserialize<SourceSetupSettings>(fileName);
                return pS;
            }
            return new SourceSetupSettings();
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

        public void SetDefaultPath()
        {
            _selectedEngineDirectory = @"C:\FlatRedBallProjects\Engines\";
        }
    }
}
