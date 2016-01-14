using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using FlatRedBall.AnimationEditorForms.Data;
using FlatRedBall.AnimationEditorForms.Preview;
using ToolsUtilities;

namespace FlatRedBall.AnimationEditorForms.IO
{
    public class IoManager : Singleton<IoManager>
    {
        string GetCompanionFileFor(string fileName)
        {

            string extension = "aeproperties";

            string location = FileManager.RemoveExtension(fileName) + "." + extension;
            return location;
        }

        public void SaveCompanionFileFor(string fileName)
        {
            AESettingsSave settingsSave = new AESettingsSave();
            settingsSave.OffsetMultiplier = PreviewManager.Self.OffsetMultiplier;

            settingsSave.HorizontalGuides.AddRange(PreviewManager.Self.HorizontalGuides);
            settingsSave.VerticalGuides.AddRange(PreviewManager.Self.VerticalGuides);


            string locationToSave = GetCompanionFileFor(fileName);

            try
            {
                FileManager.XmlSerialize(settingsSave, locationToSave);
            }
            catch(Exception e)
            {
                MessageBox.Show("Could not save companion file " + locationToSave + "\n\n" + e.ToString());
            }
        }

        public void LoadAndApplyCompanionFileFor(string achxFile)
        {
            string fileToLoad = GetCompanionFileFor(achxFile);

            bool succeeded = false;
            AESettingsSave loadedInstance = null;

            if (System.IO.File.Exists(fileToLoad))
            {
                try
                {
                    loadedInstance = 
                        FileManager.XmlDeserialize<AESettingsSave>(fileToLoad);

                    succeeded = true;
                }
                catch
                {
                    succeeded = false;
                }
            }

            if (succeeded)
            {
                ApplySettings(loadedInstance);
            }
        }

        private void ApplySettings(AESettingsSave loadedInstance)
        {
            PreviewManager.Self.OffsetMultiplier = loadedInstance.OffsetMultiplier;

            PreviewManager.Self.HorizontalGuides = loadedInstance.HorizontalGuides;
            PreviewManager.Self.VerticalGuides = loadedInstance.VerticalGuides;
        }
    }
}
