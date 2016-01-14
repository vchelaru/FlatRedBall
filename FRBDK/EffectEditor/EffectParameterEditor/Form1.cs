using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using FlatRedBall.Graphics.Model;
using EffectEditor.HelperClasses;
using System.IO;
using FlatRedBall.Graphics;
using FlatRedBall;
using Microsoft.Xna.Framework.Graphics;
using EffectParameterEditor.HelperClasses;
using FlatRedBall.Content.Model;
using FlatRedBall.Instructions;
using Microsoft.Xna.Framework;

namespace EffectParameterEditor
{
    public partial class Form1 : Form
    {
        MaterialEditor mMaterial;

        string DefaultEffectString = "Default Effect";
        string DefaultParametersString = "Default Parameters";
        string NewParametersString = "New Parameters...";

        PositionedModel mCurrentModel;
        ModelEditor mModelEditor;

        Dictionary<string, PositionedModel> mModelDictionary = new Dictionary<string, PositionedModel>();

        public Form1()
        {
            InitializeComponent();

            modelViewControl1.ModelLoaded = ModelLoaded;
            // Allow model drag-drop
            modelViewControl1.AllowDrop = true;
            modelViewControl1.DragEnter +=new DragEventHandler(modelViewPanel_DragEnter);
            modelViewControl1.DragDrop += new DragEventHandler(modelViewPanel_DragDrop);

            InstructionManager.Instructions.Add(new MethodInstruction<Form1>(
                this, "InitializeAssets", new object[] { }, 0.0));
        }

        public void InitializeAssets()
        {
            // Load invisible effect
            if (mMaterial != null && modelViewControl1.GraphicsDevice != null)
            {
                mMaterial.InvisibleEffect = FlatRedBallServices.Load<Effect>(@"Content\simple.fx");
            }
        }

        #region File Operations

        #region File loading methods

        private void LoadModel(string fileName)
        {
            modelViewControl1.AddModel(fileName);
        }

        private void LoadEffect(string fileName)
        {
            mMaterial.AddEffect(fileName, meshPartBox.SelectedIndex);
            CreateEffectsList();
        }

        private void LoadParameters(string fileName)
        {
            mMaterial.AddParameters(fileName, mMaterial.Parts[meshPartBox.SelectedIndex].EffectName, meshPartBox.SelectedIndex);
            CreateParametersList(mMaterial.Parts[meshPartBox.SelectedIndex].EffectName);
        }

        #endregion

        #region File Menu

        private void loadParametersToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (openParxFileDialog.ShowDialog() == DialogResult.OK)
            {
                string fileName = openParxFileDialog.FileName;

                LoadParameters(fileName);
            }
        }

        private void loadModelToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (openModelDialog.ShowDialog() == DialogResult.OK)
            {
                LoadModel(openModelDialog.FileName);
            }
        }

        private void loadEffectToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (openEffectDialog.ShowDialog() == DialogResult.OK)
            {
                LoadEffect(openEffectDialog.FileName);
            }
        }

        #endregion

        #region Drag and Drop

        void modelViewPanel_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                // Check if the files list contains an X or XNB file
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);

                string extension = Path.GetExtension(files[0]).ToLower();
                bool canLoad = (extension == "x" || extension == "xnb");

                e.Effect = DragDropEffects.Copy;
            }
            else
            {
                e.Effect = DragDropEffects.None;
            }
        }

        void modelViewPanel_DragDrop(object sender, DragEventArgs e)
        {
            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);

            string ext = Path.GetExtension(files[0]).ToLower();
            if (files != null && files.Length > 0 &&
                (ext == "x" || ext == "xnb" || ext == ".x" || ext == ".xnb"))
            {
                LoadModel(files[0]);
            }
            else if (files != null && files.Length > 0 &&
                (ext == "fx" || ext == ".fx"))
            {
                LoadEffect(files[0]);
            }
            else if (files != null && files.Length > 0 &&
                (ext == "fxparx" || ext == ".fxparx"))
            {
                LoadParameters(files[0]);
            }
            else if (files != null && files.Length > 0 &&
                (ext == "matx" || ext == ".matx"))
            {
                LoadMaterial(files[0]);
            }
        }

        #endregion

        #endregion

        #region Set model/effect

        void SelectModel(PositionedModel selectingmodel)
        {
            #region Store and set the model

            // store the current model
            mCurrentModel = selectingmodel;

            mModelEditor = new ModelEditor(mCurrentModel);
            modelPropGrid.SelectedObject = mModelEditor;

            // Set the model
            foreach (PositionedModel model in mModelDictionary.Values)
            {
                model.Visible = (model == mCurrentModel);
            }

            modelViewControl1.CurrentModel = mCurrentModel;

            #endregion

            #region Create the material

            mMaterial = new MaterialEditor(mCurrentModel.XnaModel);
            InitializeAssets();

            #endregion

            #region Mesh part selection

            meshPartBox.Items.Clear();
            for (int i = 0; i < mMaterial.PartCount; i++)
            {
                meshPartBox.Items.Add(i);
            }
            meshPartBox.SelectedIndex = 0;

            #endregion
        }

        delegate void ModelLoadedCallback(String name, PositionedModel model);
        private void ModelLoaded(String name, PositionedModel model)
        {
            if (this.InvokeRequired)
            {
                ModelLoadedCallback d = new ModelLoadedCallback(ModelLoaded);
                this.Invoke(d, new object[] { name, model });
            }
            else
            {
                // Add the new model
                if (mModelDictionary.ContainsKey(name))
                {
                    mModelDictionary[name] = model;
                }
                else
                {
                    mModelDictionary.Add(name, model);
                }


                SelectModel(model);
            }
        }

        #endregion

        #region Model Editor Input

        private void modelViewControl1_MouseDown(object sender, MouseEventArgs e)
        {
            modelViewControl1.Focus();
        }

        private void modelViewControl1_PreviewKeyDown(object sender, PreviewKeyDownEventArgs e)
        {
            if (modelViewControl1.Focused && e.KeyCode != Keys.Tab) e.IsInputKey = true;
        }

        #endregion

        #region Part Selection

        #region Select mesh part

        private void HighlightMeshPart(int index)
        {
            // Highlight the selected part
            if (index < 0)
            {
                mMaterial.ShowAllParts();
            }
            else
            {
                mMaterial.HighlightPart(index);
            }

            // Set up effects lists
            CreateEffectsList();
        }

        private void CreateEffectsList()
        {
            int partIndex = meshPartBox.SelectedIndex;

            // Set up effects list
            effectBox.Items.Clear();
            effectBox.Items.Add(DefaultEffectString);
            List<string> effectNames = mMaterial.EffectNames;
            effectNames.Sort();
            effectBox.Items.AddRange(effectNames.ToArray());

            // Select current effect
            if (mMaterial.Parts[partIndex].EffectName == string.Empty)
            {
                // Default
                effectBox.SelectedIndex = 0;
            }
            else
            {
                // Select the current part's effect
                effectBox.SelectedIndex = effectBox.Items.IndexOf(mMaterial.Parts[partIndex].EffectName);
            }
        }

        #endregion

        private void meshPartBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            HighlightMeshPart(meshPartBox.SelectedIndex);
        }

        private void effectBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            // Check what effect was selected
            int partIndex = meshPartBox.SelectedIndex;
            string effectName = (string)effectBox.Items[effectBox.SelectedIndex];

            // Check if this is the default effect, or a special effect
            if (effectName == DefaultEffectString)
            {
                // Set default
                mMaterial.SetPartDefaults(partIndex);
            }
            else
            {
                // Set the effect (sets default parameters too)
                mMaterial.SetPartEffect(partIndex, effectName);
            }

            // Set the property grid for the effect
            effectPropGrid.SelectedObject = new EffectPropertyEditor(mMaterial.Parts[partIndex].Effect);

            CreateParametersList(effectName);
        }

        private void CreateParametersList(string effectName)
        {
            int partIndex = meshPartBox.SelectedIndex;

            // Get parameters list
            parametersBox.Items.Clear();
            parametersBox.Items.Add(DefaultParametersString);
            List<string> parameterNames = mMaterial.ParameterNames(effectName);
            parameterNames.Sort();
            parametersBox.Items.AddRange(parameterNames.ToArray());
            parametersBox.Items.Add(NewParametersString);

            // Select parameters
            if (mMaterial.Parts[partIndex].ParametersName == string.Empty)
            {
                // Default parameters
                parametersBox.SelectedIndex = 0;
            }
            else
            {
                parametersBox.SelectedIndex = parametersBox.Items.IndexOf(mMaterial.Parts[partIndex].ParametersName);
            }
        }

        private void parametersBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            // Get the parameters name
            int partIndex = meshPartBox.SelectedIndex;
            string parametersName = (string)parametersBox.Items[parametersBox.SelectedIndex];

            if (parametersName == DefaultParametersString)
            {
                // Set default parameters
                mMaterial.SetPartDefaultParameters(partIndex);
                effectPropGrid.Refresh();
            }
            else if (parametersName == NewParametersString)
            {
                // Create new parameters
                if (saveParxFileDialog.ShowDialog() == DialogResult.OK)
                {
                    string fileName = saveParxFileDialog.FileName;
                    EffectParameterListSave list = EffectParameterListSave.FromEffect(mMaterial.Parts[partIndex].Effect);

                    // Save the file
                    list.Save(fileName);

                    // Add the file
                    mMaterial.AddParameters(fileName, mMaterial.Parts[partIndex].EffectName, partIndex);

                    // Recreate the parameters list
                    CreateParametersList(mMaterial.Parts[partIndex].EffectName);
                }
            }
            else
            {
                // Set parameters on part
                mMaterial.SetPartParameters(partIndex, parametersName);
                effectPropGrid.Refresh();
            }
        }

        #endregion

        private void effectPropGrid_PropertyValueChanged(object s, PropertyValueChangedEventArgs e)
        {
            // Update the parameters collection
            mMaterial.UpdatePartParameters(meshPartBox.SelectedIndex);
        }

        private void LoadMaterial(string fileName)
        {
            mMaterial.LoadMaterial(fileName);
            meshPartBox_SelectedIndexChanged(this, new EventArgs());
        }

        private void saveMaterialToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (saveMaterialDialog.ShowDialog() == DialogResult.OK)
            {
                mMaterial.SaveMaterial(saveMaterialDialog.FileName);
            }
        }

        private void loadMaterialToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (openMaterialDialog.ShowDialog() == DialogResult.OK)
            {
                LoadMaterial(openMaterialDialog.FileName);
            }
        }
    }
}