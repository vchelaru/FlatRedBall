using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using FlatRedBall.AnimationEditorForms.Data;
using FlatRedBall.AnimationEditorForms.Preview;
using ToolsUtilities;
using FlatRedBall.AnimationEditorForms.CommandsAndState;

namespace FlatRedBall.AnimationEditorForms.IO
{
    public class IoManager : Singleton<IoManager>
    {
        FilePath GetCompanionFileFor(FilePath fileName)
        {

            string extension = "aeproperties";

            string location = fileName.RemoveExtension() + "." + extension;
            return location;
        }

        public void SaveCompanionFileFor(FilePath fileName)
        {
            AESettingsSave settingsSave = new AESettingsSave();
            settingsSave.OffsetMultiplier = PreviewManager.Self.OffsetMultiplier;

            settingsSave.HorizontalGuides.AddRange(PreviewManager.Self.HorizontalGuides);
            settingsSave.VerticalGuides.AddRange(PreviewManager.Self.VerticalGuides);

            settingsSave.ExpandedNodes.Clear();
            settingsSave.ExpandedNodes.AddRange(TreeViewManager.Self.GetExpandedNodeAnimationChainNames());
            settingsSave.UnitType = AppState.Self.UnitType;
            settingsSave.GridSize = AppState.Self.GridSize;
            settingsSave.SnapToGrid = AppState.Self.IsSnapToGridChecked;

            var locationToSave = GetCompanionFileFor(fileName);

            try
            {
                FileManager.XmlSerialize(settingsSave, locationToSave.FullPath);
            }
            catch(Exception e)
            {
                MessageBox.Show("Could not save companion file " + locationToSave + "\n\n" + e.ToString());
            }
        }

        public void LoadAndApplyCompanionFileFor(string achxFile)
        {
            var fileToLoad = GetCompanionFileFor(achxFile);

            bool succeeded = false;
            AESettingsSave loadedInstance = null;

            if (fileToLoad.Exists())
            {
                try
                {
                    loadedInstance = 
                        FileManager.XmlDeserialize<AESettingsSave>(fileToLoad.FullPath);

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

            if(loadedInstance.ExpandedNodes != null)
            {
                TreeViewManager.Self.ExpandNodes(loadedInstance.ExpandedNodes);
            }
            AppState.Self.UnitType = loadedInstance.UnitType;
            AppState.Self.IsSnapToGridChecked = loadedInstance.SnapToGrid;
            AppState.Self.GridSize = loadedInstance.GridSize;


        }
    }
}
