using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using TmxEditor.GraphicalDisplay.Tilesets;
using RenderingLibrary;
using RenderingLibrary.Content;
using System.Reflection;
using FlatRedBall.SpecializedXnaControls;
using System.IO;
using TMXGlueLib.DataTypes;
using FlatRedBall.IO;
using TmxEditor.Controllers;

namespace TmxEditor
{
    public partial class Form1 : Form
    {
        private TmxEditorControl tmxEditorControl1;



        public Form1()
        {
            InitializeComponent();
            this.tmxEditorControl1 = new TmxEditor.TmxEditorControl();

            // 
            // tmxEditorControl1
            // 
            this.tmxEditorControl1.Location = new System.Drawing.Point(12, 27);
            this.tmxEditorControl1.Name = "tmxEditorControl1";
            this.tmxEditorControl1.Size = new System.Drawing.Size(468, 404);
            this.tmxEditorControl1.TabIndex = 5;
            this.tmxEditorControl1.Dock = DockStyle.Fill;

            this.Controls.Add(this.tmxEditorControl1);
            this.tmxEditorControl1.BringToFront();

        }

        
        private void loadTMXToolStripMenuItem_Click(object sender, EventArgs e)
        {
            string fileName = ShowLoadFile("tmx");
            tmxEditorControl1.LoadFile(fileName);

        }
        
        private void loadTilesetPropertiesFromToolStripMenuItem_Click(object sender, EventArgs e)
        {
            string fileName = ShowLoadFile("tmx");
            tmxEditorControl1.LoadTilesetProperties(fileName);

        }

        private string ShowLoadFile(string extension)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "(*." + extension + ")|*." + extension + "";
            var result = openFileDialog.ShowDialog();

            if (result == System.Windows.Forms.DialogResult.OK)
            {
                return openFileDialog.FileName;
            }
            else
            {
                return null;
            }
        }

        private void saveTMXAsToolStripMenuItem_Click(object sender, EventArgs e)
        {

            string fileName = SaveFile("tmx");
            if (!string.IsNullOrEmpty(fileName))
            {
                ProjectManager.Self.SaveTiledMapSave(fileName);
            }
        }

        private string SaveFile(string extension)
        {
            SaveFileDialog fileDialog = new SaveFileDialog();
            fileDialog.Filter = "(*." + extension + ")|*." + extension + "";
            var result = fileDialog.ShowDialog();

            if (result == System.Windows.Forms.DialogResult.OK)
            {
                return fileDialog.FileName;
            }
            else
            {
                return null;
            }
        }



        private void Form1_Resize(object sender, EventArgs e)
        {
            ToolComponentManager.Self.ReactToWindowResize();
        }

        private void saveTILBToolStripMenuItem_Click(object sender, EventArgs e)
        {
            string fileName = SaveFile("tilb");

            if (!string.IsNullOrEmpty(fileName))
            {
                // Eventually read this from somewhere?
                float zOffset = 0;
                using (FileStream stream = File.OpenWrite(fileName))
                using(BinaryWriter writer = new BinaryWriter(stream))
                {
                    ReducedTileMapInfo rtmi = 
                        ReducedTileMapInfo.FromTiledMapSave( 
                            ProjectManager.Self.TiledMapSave , 
                            1,
                            zOffset,
                            FileManager.GetDirectory( ProjectManager.Self.LastLoadedFile), TMXGlueLib.FileReferenceType.NoDirectory);

                    rtmi.WriteTo(writer);

                    writer.Close();
                    stream.Close();


                    Verify(rtmi, fileName);


                }

            }
        }

        private void Verify(ReducedTileMapInfo rtmi, string fileName)
        {
            using(FileStream stream = File.OpenRead(fileName))
            using(BinaryReader reader = new BinaryReader(stream))
            {
                ReducedTileMapInfo compareAgainst = ReducedTileMapInfo.ReadFrom(reader);

                string original;
                string fromFile;

                FileManager.XmlSerialize(rtmi, out original);
                FileManager.XmlSerialize(compareAgainst, out fromFile);

                if (original != fromFile)
                {
                    throw new Exception("NONONO");
                }
            }
        }
    }
}
