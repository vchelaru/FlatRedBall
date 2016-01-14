using EditorObjects.EditorSettings;
using FlatRedBall;
using FlatRedBall.Content.Math.Splines;
using FlatRedBall.Content.Scene;
using FlatRedBall.IO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using ToolTemplate;

namespace SplineEditor.Commands
{
    public class FileCommands
    {
        public void Save()
        {
            if (string.IsNullOrEmpty(EditorData.SplineList.Name))
            {
                MessageBox.Show("The Spline must first be saved using the \"Save As\" command");
            }
            else
            {

                SplineSaveList ssl = SplineSaveList.FromSplineList(EditorData.SplineList);
                string fileName = EditorData.SplineList.Name;

                ssl.Save(fileName);

                // Save the settings file
                SplineEditorSettingsSave sess = new SplineEditorSettingsSave();

                sess.BoundsCamera = CameraSave.FromCamera(EditorData.BoundsCamera, true);
                sess.ViewCamera = CameraSave.FromCamera(SpriteManager.Camera, false);

                FileManager.XmlSerialize(sess,
                    FileManager.RemoveExtension(fileName) + ".splsetx");
            }
        }

        public void SaveAs()
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog();
            saveFileDialog.Filter = "Spline XML (*.splx)|*.splx";


            if (EditorData.SplineList != null && !string.IsNullOrEmpty(EditorData.SplineList.Name))
            {
                saveFileDialog.FileName = EditorData.SplineList.Name;
            }


            var dialogResult = saveFileDialog.ShowDialog();

            if (dialogResult == DialogResult.OK)
            {
                EditorData.SplineList.Name = saveFileDialog.FileName;

                Save();
            }
        }

        public void Load()
        {
            OpenFileDialog fileDialog = new OpenFileDialog();

            fileDialog.FileName = "*.splx";

            var dialogResult = fileDialog.ShowDialog();

            if (dialogResult == DialogResult.OK)
            {
                LoadSplineFromFileName(fileDialog.FileName);

            }
        }

        public void LoadSplineFromFileName(string fileName)
        {
            EditorData.LoadSplines(fileName);

            try
            {
                // See if there is a settings file
                string settingsFile = FileManager.RemoveExtension(fileName) + ".splsetx";

                if (FileManager.FileExists(settingsFile))
                {
                    SplineEditorSettingsSave sess = FileManager.XmlDeserialize<SplineEditorSettingsSave>(settingsFile);

                    sess.BoundsCamera.SetCamera(EditorData.BoundsCamera);
                    sess.ViewCamera.SetCamera(SpriteManager.Camera);

                }
            }
            catch (Exception e)
            {
                // no big deal, just a settings file

            }
        }

        internal void LoadScene()
        {
            OpenFileDialog fileDialog = new OpenFileDialog();

            fileDialog.FileName = "*.scnx";

            var dialogResult = fileDialog.ShowDialog();

            if (dialogResult == DialogResult.OK)
            {
                EditorData.LoadScene(fileDialog.FileName);
            }
        }
    }
}
