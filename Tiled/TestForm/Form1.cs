using System;
using System.ComponentModel;
using System.Windows.Forms;
using FlatRedBall.Content;
using FlatRedBall.Content.AI.Pathfinding;
using FlatRedBall.Content.Math.Geometry;
using FlatRedBall.Content.Scene;
using TMXGlueLib;

namespace TestForm
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void tmxButton_Click(object sender, EventArgs e)
        {
            tmxFileDialog.ShowDialog();
        }

        private void TmxFiledialogOk(object sender, CancelEventArgs e)
        {
            tmxFilename.Text = tmxFileDialog.FileName;
        }

        

        private void tmxDestinationButton_Click(object sender, EventArgs e)
        {
            DialogResult result = tmxFolderDialog.ShowDialog();
            if (result == DialogResult.OK)
            {
                tmxDestinationFolder.Text = tmxFolderDialog.SelectedPath;
            }
        }

        private void tmxConvertToScnx_Click(object sender, EventArgs e)
        {
            TiledMapSave.Offset = new Tuple<float, float, float>(float.Parse(offsetX.Text), float.Parse(offsetY.Text),
                                                                 float.Parse(offsetZ.Text));
            TiledMapSave save = TiledMapSave.FromFile(tmxFilename.Text);
            SceneSave ses = save.ToSceneSave(1.0f);
            string pathtosave = tmxDestinationFolder.Text + GetFilename(tmxFilename.Text) + ".scnx";
            ses.Save(pathtosave);
        }

        private void tmxConvertToNntx_Click(object sender, EventArgs e)
        {
            TiledMapSave.Offset = new Tuple<float, float, float>(float.Parse(offsetX.Text), float.Parse(offsetY.Text),
                                                                 float.Parse(offsetZ.Text));
            TiledMapSave save = TiledMapSave.FromFile(tmxFilename.Text);
            NodeNetworkSave nns = save.ToNodeNetworkSave();
            string pathtosave = tmxDestinationFolder.Text + GetFilename(tmxFilename.Text) + ".nntx";
            nns.Save(pathtosave);
        }

        private void tmxConvertToShcx_Click(object sender, EventArgs e)
        {
            TiledMapSave.Offset = new Tuple<float, float, float>(float.Parse(offsetX.Text), float.Parse(offsetY.Text),
                                                                 float.Parse(offsetZ.Text));
            TiledMapSave save = TiledMapSave.FromFile(tmxFilename.Text);
            ShapeCollectionSave scs = save.ToShapeCollectionSave(null);
            string pathtosave = tmxDestinationFolder.Text + GetFilename(tmxFilename.Text) + ".shcx";
            scs.Save(pathtosave);
        }

        private static string GetFilename(string filepath)
        {
            return filepath.Substring(filepath.LastIndexOf("\\", StringComparison.Ordinal) + 1).Replace(".tmx", "");
        }

        private void tmxCSVButton_Click(object sender, EventArgs e)
        {
            TiledMapSave save = TiledMapSave.FromFile(tmxFilename.Text);
            string csv = save.ToCSVString(type: TiledMapSave.CSVPropertyType.Tile, layerName: tmxLayerName.Text);
            System.IO.File.WriteAllText(tmxDestinationFolder.Text + GetFilename(tmxFilename.Text) + "_tile.csv", csv);
        }

        private void tmxLayerCSVButton_Click(object sender, EventArgs e)
        {
            TiledMapSave save = TiledMapSave.FromFile(tmxFilename.Text);
            string csv = save.ToCSVString(type: TiledMapSave.CSVPropertyType.Layer, layerName: tmxLayerName.Text);
            System.IO.File.WriteAllText(tmxDestinationFolder.Text + GetFilename(tmxFilename.Text) + "_layer.csv", csv);
        }

        private void button1_Click(object sender, EventArgs e)
        {
            TiledMapSave save = TiledMapSave.FromFile(tmxFilename.Text);
            string csv = save.ToCSVString(type: TiledMapSave.CSVPropertyType.Object, layerName: tmxLayerName.Text);
            System.IO.File.WriteAllText(tmxDestinationFolder.Text + GetFilename(tmxFilename.Text) + "_object.csv", csv);
        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }





    }
}
