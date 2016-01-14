using FlatRedBall.IO;
using System.Xml.Serialization;

namespace FlatRedBall.Glue.Settings
{
    [XmlRoot("PreferencesSave")]
	public class PreferenceSettings
	{
		#region Fields

        private bool _childExternalApps;
        private bool _generateTombstoningCode = true;
        private bool _showHiddenNodes;

        const string Filename = "PreferenceSettings.xml";

		#endregion

        public bool ChildExternalApps
        {
            get
            {
                return _childExternalApps;
            }

            set
            {
                _childExternalApps = value;
                // We used to call 
                // SaveSettings here, 
                // but that's bad because
                // the setter gets called
                // when you deserialize.
                //SaveSettings();
            }
        }

        public bool GenerateTombstoningCode
        {
            get
            {
                return _generateTombstoningCode;
            }

            set
            {
                _generateTombstoningCode = value;
            }
        }

        public bool ShowHiddenNodes
        {
            get
            {
                return _showHiddenNodes;
            }

            set
            {
                _showHiddenNodes = value;
            }
        }

        public static PreferenceSettings LoadSettings()
        {
            var fileName = FileManager.UserApplicationDataForThisApplication + Filename;

            if (!FileManager.FileExists(fileName))
            {
                return new PreferenceSettings();
            }

            var pS = FileManager.XmlDeserialize<PreferenceSettings>(fileName);
            return pS;
        }

        public void SaveSettings()
		{
            FileManager.XmlSerialize(this, FileManager.UserApplicationDataForThisApplication + Filename);
		}
	}
}
