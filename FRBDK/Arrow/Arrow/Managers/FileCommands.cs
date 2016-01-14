using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ArrowDataConversion;
using FlatRedBall.Arrow.DataTypes;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.IO;
using Microsoft.Win32;

namespace FlatRedBall.Arrow.Managers
{
    public class FileCommands
    {
        public void ShowLoadUI()
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            bool? result = openFileDialog.ShowDialog();

            if (result.HasValue && result.Value)
            {
                string fileName = openFileDialog.FileName;

                LoadProject(fileName);
            }
        }

        public void LoadProject(string fileName)
        {
            string extension = FileManager.GetExtension(fileName);

            // better be a .arox
            ArrowProjectSave arrowProject = FileManager.XmlDeserialize<ArrowProjectSave>(fileName);

            ArrowState.Self.CurrentArrowProject = arrowProject;

            ArrowProjectToGlueProjectConverter converter = new ArrowProjectToGlueProjectConverter();
            GlueProjectSave glueProjectSave = converter.ToGlueProjectSave(ArrowState.Self.CurrentArrowProject);

            ArrowState.Self.CurrentGlueProjectSave = glueProjectSave;
            ArrowState.Self.CurrentGluxFileLocation = fileName;
        }

        public void SaveProject()
        {
            string fileToSave = null;

            if (string.IsNullOrEmpty(ArrowState.Self.CurrentGluxFileLocation))
            {
                SaveFileDialog fileDialog = new SaveFileDialog();
                fileDialog.Filter = "*.arox|*.arox";

                var result = fileDialog.ShowDialog();

                if (result.HasValue && result.Value)
                {
                    fileToSave = fileDialog.FileName;
                    ArrowState.Self.CurrentGluxFileLocation = fileToSave;
                }

            }
            else
            {
                fileToSave = ArrowState.Self.CurrentGluxFileLocation;
            }

            if (!string.IsNullOrEmpty(fileToSave))
            {
                FileManager.XmlSerialize(ArrowState.Self.CurrentArrowProject, fileToSave);
            }
        }

        internal void GenerateGlux(bool save = true)
        {
            ArrowProjectToGlueProjectConverter converter = new ArrowProjectToGlueProjectConverter();
            GlueProjectSave glueProjectSave = converter.ToGlueProjectSave(ArrowState.Self.CurrentArrowProject);

            if (glueProjectSave != null)
            {
                foreach (var screen in glueProjectSave.Screens)
                {
                    foreach (var nos in screen.AllNamedObjects)
                    {
                        nos.UpdateCustomProperties();
                    }
                }
                foreach (var entity in glueProjectSave.Entities)
                {
                    foreach (var nos in entity.AllNamedObjects)
                    {
                        nos.UpdateCustomProperties();
                    }
                }
            }

            ArrowState.Self.CurrentGlueProjectSave = glueProjectSave;

            if (save)
            {
                SaveGlux();
            }
        }

        public void SaveGlux()
        {
            string whereToSave = FileManager.RemoveExtension(ArrowState.Self.CurrentGluxFileLocation) + ".Arrow.Generated.glux";

            FileManager.XmlSerialize(ArrowState.Self.CurrentGlueProjectSave, whereToSave);
        }

        internal void ShowLoadProject()
        {
            OpenFileDialog fileDialog = new OpenFileDialog();
            fileDialog.Filter = "*.arox|*.arox";

            var result = fileDialog.ShowDialog();

            if (result.HasValue && result.Value)
            {
                LoadProject(fileDialog.FileName);
            }
        }
    }
}
