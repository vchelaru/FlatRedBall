using EditorObjects.IoC;
using FlatRedBall.Glue;
using FlatRedBall.Glue.Plugins.ExportedInterfaces;
using FlatRedBall.IO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OfficialPlugins.ContentPipelinePlugin
{
    public class SaveLoadLogic
    {
        public static string SettingsFileLocation
        {
            get
            {
                return Container.Get<IGlueState>().ProjectSpecificSettingsFolder + "ContentPipelineSettings.xml";
            }
        }

        public static void SaveSettings(SettingsSave settings)
        {
            FileManager.XmlSerialize(settings, SettingsFileLocation);
        }
        public static SettingsSave LoadSettings()
        {
            if(FileManager.FileExists(SettingsFileLocation))
            {
                return FileManager.XmlDeserialize<SettingsSave>(SettingsFileLocation);
            }
            else
            {
                return null;
            }
        }
    }
}
